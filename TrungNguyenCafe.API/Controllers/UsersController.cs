
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.API.Models.Users;
using TrungNguyenCafe.Entities.Security;
using TrungNguyenCafe.Entities.Users;
using TrungNguyenCafe.Infrastructure.Data;

namespace TrungNguyenCafe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleNames.SystemAdmin + "," + RoleNames.FranchiseOwner)]
    public class UsersController : ControllerBase
    {
        private readonly CafeDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UsersController(
            CafeDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // =====================================
        // 1. LẤY DANH SÁCH NHÂN VIÊN
        // =====================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponse>>>
            GetAllUsers(CancellationToken cancellationToken)
        {
            var users = await (
                from user in _context.Users.AsNoTracking()

                join roleItem in _context.Roles.AsNoTracking()
                    on user.RoleId equals (Guid?)roleItem.RoleId
                    into roleGroup

                from role in roleGroup.DefaultIfEmpty()

                join tenantItem in _context.Tenants.AsNoTracking()
                    on user.TenantId equals (Guid?)tenantItem.TenantId
                    into tenantGroup

                from tenant in tenantGroup.DefaultIfEmpty()

                orderby user.UserName

                select new UserResponse
                {
                    UserId = user.UserId,

                    UserName = user.UserName,

                    AccountStatus = user.AccountStatus,

                    RoleId = user.RoleId,

                    RoleName = role != null
                        ? role.RoleName
                        : string.Empty,

                    TenantId = user.TenantId,

                    TenantCode = tenant != null
                        ? tenant.TenantCode
                        : null,

                    TenantName = tenant != null
                        ? tenant.TenantName
                        : null
                }
            ).ToListAsync(cancellationToken);

            return Ok(users);
        }


        // =====================================
        // 2. LẤY THÔNG TIN CHI TIẾT TÀI KHOẢN
        // =====================================

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetUserById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == id,
                    cancellationToken);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản."
                });
            }

            var response = await BuildUserResponseAsync(
                user,
                cancellationToken);

            return Ok(response);
        }


        // =====================================
        // 3. TẠO TÀI KHOẢN NHÂN VIÊN
        // =====================================

        [HttpPost]
        public async Task<ActionResult<UserResponse>> CreateUser(
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Chuẩn hóa dữ liệu
            var userName = request.UserName.Trim();
            var roleName = request.RoleName.Trim();

            // 2. Kiểm tra vai trò
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.RoleName == roleName,
                    cancellationToken);

            if (role == null)
            {
                return BadRequest(new
                {
                    message = "Vai trò không hợp lệ."
                });
            }

            // 3. Không cho tạo Admin
            if (role.RoleName == RoleNames.SystemAdmin)
            {
                return BadRequest(new
                {
                    message = "Không thể tạo tài khoản Admin tại đây."
                });
            }

            // 4. Xác định phạm vi vai trò
            bool isSystemRole =
                role.RoleName == RoleNames.HQManager;

            bool isTenantRole =
                role.RoleName == RoleNames.FranchiseOwner ||
                role.RoleName == RoleNames.StoreManager ||
                role.RoleName == RoleNames.SalesStaff;

            if (!isSystemRole && !isTenantRole)
            {
                return BadRequest(new
                {
                    message = "Vai trò không được phép sử dụng."
                });
            }

            // 5. Kiểm tra TenantId
            if (isSystemRole && request.TenantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "HQManager không thuộc chi nhánh."
                });
            }

            if (isTenantRole && !request.TenantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn chi nhánh."
                });
            }

            // 6. Kiểm tra chi nhánh
            var tenant = request.TenantId.HasValue
                ? await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.TenantId == request.TenantId.Value
                        && x.IsActive,
                        cancellationToken)
                : null;

            if (isTenantRole && tenant == null)
            {
                return BadRequest(new
                {
                    message = "Chi nhánh không tồn tại hoặc đã ngừng hoạt động."
                });
            }

            // 7. Kiểm tra tên đăng nhập
            var usernameExists = await _context.Users
                .AnyAsync(
                    x => x.UserName == userName
                    && x.TenantId == request.TenantId,
                    cancellationToken);

            if (usernameExists)
            {
                return Conflict(new
                {
                    message = "Tên đăng nhập đã tồn tại."
                });
            }

            // 8. Tạo tài khoản
            var user = new User
            {
                UserName = userName,

                AccountStatus = "Active",

                TenantId = request.TenantId,

                RoleId = role.RoleId
            };

            // 9. Băm mật khẩu
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    request.Password);

            // 10. Lưu Database
            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể tạo tài khoản. Vui lòng kiểm tra tên đăng nhập."
                });
            }

            // 11. Trả kết quả
            var response = new UserResponse
            {
                UserId = user.UserId,

                UserName = user.UserName,

                AccountStatus = user.AccountStatus,

                RoleId = role.RoleId,

                RoleName = role.RoleName,

                TenantId = user.TenantId,

                TenantCode = tenant?.TenantCode,

                TenantName = tenant?.TenantName
            };

            return CreatedAtAction(
                nameof(GetUserById),
                new { id = user.UserId },
                response);
        }


        // =====================================
        // 4. CẬP NHẬT TÀI KHOẢN
        // =====================================

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserResponse>> UpdateUser(
            Guid id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Tìm tài khoản
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == id,
                    cancellationToken);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản."
                });
            }

            // 2. Không cho sửa tài khoản Admin
            var currentRole = user.RoleId.HasValue
                ? await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.RoleId == user.RoleId.Value,
                        cancellationToken)
                : null;

            if (currentRole?.RoleName == RoleNames.SystemAdmin)
            {
                return BadRequest(new
                {
                    message = "Không thể chỉnh sửa tài khoản Admin hệ thống."
                });
            }

            // 3. Kiểm tra vai trò mới
            var roleName = request.RoleName.Trim();

            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.RoleName == roleName,
                    cancellationToken);

            if (role == null ||
                role.RoleName == RoleNames.SystemAdmin)
            {
                return BadRequest(new
                {
                    message = "Vai trò không hợp lệ."
                });
            }

            bool isSystemRole =
                role.RoleName == RoleNames.HQManager;

            bool isTenantRole =
                role.RoleName == RoleNames.FranchiseOwner ||
                role.RoleName == RoleNames.StoreManager ||
                role.RoleName == RoleNames.SalesStaff;

            if (!isSystemRole && !isTenantRole)
            {
                return BadRequest(new
                {
                    message = "Vai trò không được phép sử dụng."
                });
            }

            // 4. Kiểm tra chi nhánh
            if (isSystemRole && request.TenantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "HQManager không thuộc chi nhánh."
                });
            }

            if (isTenantRole && !request.TenantId.HasValue)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn chi nhánh."
                });
            }

            var tenant = request.TenantId.HasValue
                ? await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.TenantId == request.TenantId.Value
                        && x.IsActive,
                        cancellationToken)
                : null;

            if (isTenantRole && tenant == null)
            {
                return BadRequest(new
                {
                    message = "Chi nhánh không tồn tại hoặc đã ngừng hoạt động."
                });
            }

            // 5. Kiểm tra trùng tài khoản trong phạm vi mới
            var usernameExists = await _context.Users
                .AnyAsync(
                    x => x.UserId != id
                    && x.UserName == user.UserName
                    && x.TenantId == request.TenantId,
                    cancellationToken);

            if (usernameExists)
            {
                return Conflict(new
                {
                    message = "Tên đăng nhập đã tồn tại trong phạm vi này."
                });
            }

            // 6. Cập nhật vai trò và chi nhánh
            user.RoleId = role.RoleId;

            user.TenantId = request.TenantId;

            // 7. Lưu thay đổi
            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể cập nhật tài khoản."
                });
            }

            // 8. Trả kết quả
            var response = new UserResponse
            {
                UserId = user.UserId,

                UserName = user.UserName,

                AccountStatus = user.AccountStatus,

                RoleId = role.RoleId,

                RoleName = role.RoleName,

                TenantId = user.TenantId,

                TenantCode = tenant?.TenantCode,

                TenantName = tenant?.TenantName
            };

            return Ok(response);
        }


        // =====================================
        // 5. KHÓA TÀI KHOẢN
        // =====================================

        [HttpPatch("{id:guid}/lock")]
        public async Task<IActionResult> LockUser(
            Guid id,
            CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == id,
                    cancellationToken);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản."
                });
            }

            // Không cho khóa Admin
            var roleName = user.RoleId.HasValue
                ? await _context.Roles
                    .Where(x => x.RoleId == user.RoleId.Value)
                    .Select(x => x.RoleName)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            if (roleName == RoleNames.SystemAdmin)
            {
                return BadRequest(new
                {
                    message = "Không thể khóa tài khoản Admin hệ thống."
                });
            }

            if (user.AccountStatus == "Locked")
            {
                return Conflict(new
                {
                    message = "Tài khoản đã bị khóa."
                });
            }

            user.AccountStatus = "Locked";

            await _context.SaveChangesAsync(
                cancellationToken);

            return Ok(new
            {
                message = "Khóa tài khoản thành công."
            });
        }


        // =====================================
        // 6. MỞ KHÓA TÀI KHOẢN
        // =====================================

        [HttpPatch("{id:guid}/unlock")]
        public async Task<IActionResult> UnlockUser(
            Guid id,
            CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == id,
                    cancellationToken);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản."
                });
            }

            if (user.AccountStatus == "Active")
            {
                return Conflict(new
                {
                    message = "Tài khoản đang hoạt động."
                });
            }

            user.AccountStatus = "Active";

            await _context.SaveChangesAsync(
                cancellationToken);

            return Ok(new
            {
                message = "Mở khóa tài khoản thành công."
            });
        }


        // =====================================
        // HÀM TẠO USER RESPONSE
        // =====================================

        private async Task<UserResponse> BuildUserResponseAsync(
            User user,
            CancellationToken cancellationToken)
        {
            var role = user.RoleId.HasValue
                ? await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.RoleId == user.RoleId.Value,
                        cancellationToken)
                : null;

            var tenant = user.TenantId.HasValue
                ? await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.TenantId == user.TenantId.Value,
                        cancellationToken)
                : null;

            return new UserResponse
            {
                UserId = user.UserId,

                UserName = user.UserName,

                AccountStatus = user.AccountStatus,

                RoleId = user.RoleId,

                RoleName = role?.RoleName ?? string.Empty,

                TenantId = user.TenantId,

                TenantCode = tenant?.TenantCode,

                TenantName = tenant?.TenantName
            };
        }
    }
}
