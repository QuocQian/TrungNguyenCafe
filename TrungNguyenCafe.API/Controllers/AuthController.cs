
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;

using TrungNguyenCafe.API.Models.Auth;
using TrungNguyenCafe.API.Services;
using TrungNguyenCafe.Entities.Security;
using TrungNguyenCafe.Entities.Users;
using TrungNguyenCafe.Infrastructure.Data;

namespace TrungNguyenCafe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CafeDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(
            CafeDbContext context,
            IPasswordHasher<User> passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        // POST: api/Auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Chuẩn hóa tên đăng nhập
            var userName = request.UserName.Trim();

            User? user;

            // 2. Xác định loại tài khoản
            if (string.IsNullOrWhiteSpace(request.TenantCode))
            {
                // Tài khoản toàn hệ thống: A1, A2
                user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.UserName == userName
                        && u.TenantId == null,
                        cancellationToken);
            }
            else
            {
                // Tài khoản thuộc chi nhánh: A3, A4, A5
                var tenantCode = request.TenantCode.Trim();

                // 3. Tìm chi nhánh đang hoạt động
                var tenant = await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.TenantCode.ToUpper()
                            == tenantCode.ToUpper()
                        && t.IsActive,
                        cancellationToken);

                if (tenant == null)
                {
                    return InvalidLogin();
                }

                // 4. Tìm tài khoản thuộc đúng chi nhánh
                user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.UserName == userName
                        && u.TenantId == tenant.TenantId,
                        cancellationToken);
            }

            // 5. Kiểm tra tài khoản
            if (user == null)
            {
                return InvalidLogin();
            }

            // 6. Kiểm tra trạng thái tài khoản
            if (!string.Equals(
                user.AccountStatus,
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return InvalidLogin();
            }

            // 7. Kiểm tra mật khẩu
            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return InvalidLogin();
            }

            // 8. Kiểm tra RoleId
            if (!user.RoleId.HasValue)
            {
                return InvalidLogin();
            }

            // 9. Lấy thông tin Role
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.RoleId == user.RoleId.Value,
                    cancellationToken);

            if (role == null ||
                !RoleNames.All.Contains(
                    role.RoleName,
                    StringComparer.OrdinalIgnoreCase))
            {
                return InvalidLogin();
            }

            // 10. Kiểm tra quyền theo phạm vi tài khoản
            bool isSystemRole =
                string.Equals(
                    role.RoleName,
                    RoleNames.SystemAdmin,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    role.RoleName,
                    RoleNames.HQManager,
                    StringComparison.OrdinalIgnoreCase);

            bool isTenantRole =
                string.Equals(
                    role.RoleName,
                    RoleNames.FranchiseOwner,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    role.RoleName,
                    RoleNames.StoreManager,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    role.RoleName,
                    RoleNames.SalesStaff,
                    StringComparison.OrdinalIgnoreCase);

            if (user.TenantId == null && !isSystemRole)
            {
                return InvalidLogin();
            }

            if (user.TenantId.HasValue && !isTenantRole)
            {
                return InvalidLogin();
            }

            // 11. Tạo JWT Token
            var accessToken = _jwtTokenService.CreateToken(
                user,
                role.RoleName);

            // 12. Lấy thời gian hết hạn Token
            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            var expiresAtUtc = jwtToken.ValidTo;

            // 13. Tạo Login Response
            var response = new LoginResponse
            {
                AccessToken = accessToken,

                TokenType = "Bearer",

                ExpiresAtUtc = expiresAtUtc,

                User = new LoginUserInfo
                {
                    UserId = user.UserId,

                    UserName = user.UserName,

                    RoleName = role.RoleName,

                    TenantId = user.TenantId
                }
            };

            // 14. Trả kết quả
            return Ok(response);
        }

        // Thông báo đăng nhập thất bại thống nhất
        private UnauthorizedObjectResult InvalidLogin()
        {
            return Unauthorized(new
            {
                message = "Tên đăng nhập hoặc mật khẩu không đúng."
            });
        }
    }
}
