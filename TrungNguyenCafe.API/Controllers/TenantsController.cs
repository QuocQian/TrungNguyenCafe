
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.API.Models.Tenants;
using TrungNguyenCafe.Entities.Tenants;
using TrungNguyenCafe.Infrastructure.Data;

namespace TrungNguyenCafe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class TenantsController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public TenantsController(CafeDbContext context)
        {
            _context = context;
        }


        // =====================================
        // 1. LẤY DANH SÁCH CHI NHÁNH
        // =====================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantResponse>>> GetAllTenants(
            [FromQuery] bool includeInactive = false)
        {
            // 1. Truy vấn danh sách chi nhánh
            var query = _context.Tenants
                .AsNoTracking();

            // 2. Mặc định chỉ lấy chi nhánh đang hoạt động
            if (!includeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            // 3. Sắp xếp và chuyển dữ liệu sang Response
            var tenants = await query
                .OrderBy(x => x.TenantCode)
                .Select(x => new TenantResponse
                {
                    TenantId = x.TenantId,
                    TenantCode = x.TenantCode,
                    TenantName = x.TenantName,
                    Address = x.Address,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            // 4. Trả về kết quả
            return Ok(tenants);
        }


        // =====================================
        // 2. LẤY THÔNG TIN CHI TIẾT CHI NHÁNH
        // =====================================

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TenantResponse>> GetTenantById(Guid id)
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .Where(x => x.TenantId == id)
                .Select(x => new TenantResponse
                {
                    TenantId = x.TenantId,
                    TenantCode = x.TenantCode,
                    TenantName = x.TenantName,
                    Address = x.Address,
                    IsActive = x.IsActive
                })
                .SingleOrDefaultAsync();

            if (tenant == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chi nhánh."
                });
            }

            return Ok(tenant);
        }

        // =====================================
        // 3. TẠO CHI NHÁNH MỚI
        // =====================================

        [HttpPost]
        public async Task<ActionResult<TenantResponse>> CreateTenant(
            [FromBody] CreateTenantRequest request)
        {
            // 1. Chuẩn hóa dữ liệu đầu vào
            var tenantCode = request.TenantCode.Trim();

            // 2. Kiểm tra mã chi nhánh đã tồn tại
            var codeExists = await _context.Tenants
                .AnyAsync(x => x.TenantCode == tenantCode);

            if (codeExists)
            {
                return Conflict(new
                {
                    message = "Mã chi nhánh đã tồn tại."
                });
            }

            // 3. Tạo đối tượng chi nhánh
            var tenant = new Tenant
            {
                TenantCode = tenantCode,
                TenantName = request.TenantName.Trim(),
                Address = request.Address.Trim(),
                IsActive = true
            };

            // 4. Lưu vào Database
            _context.Tenants.Add(tenant);

            await _context.SaveChangesAsync();

            // 5. Tạo dữ liệu trả về
            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                TenantCode = tenant.TenantCode,
                TenantName = tenant.TenantName,
                Address = tenant.Address,
                IsActive = tenant.IsActive
            };

            // 6. Trả về HTTP 201 Created
            return CreatedAtAction(
                nameof(GetTenantById),
                new { id = tenant.TenantId },
                response
            );
        }

        // =====================================
        // 4. CẬP NHẬT THÔNG TIN CHI NHÁNH
        // =====================================

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TenantResponse>> UpdateTenant(
            Guid id,
            [FromBody] UpdateTenantRequest request)
        {
            // 1. Tìm chi nhánh theo ID
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(x => x.TenantId == id);

            // 2. Kiểm tra chi nhánh có tồn tại không
            if (tenant == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chi nhánh."
                });
            }

            // 3. Cập nhật thông tin chi nhánh
            tenant.TenantName = request.TenantName.Trim();
            tenant.Address = request.Address.Trim();

            // 4. Lưu thay đổi vào Database
            await _context.SaveChangesAsync();

            // 5. Trả về thông tin sau khi cập nhật
            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                TenantCode = tenant.TenantCode,
                TenantName = tenant.TenantName,
                Address = tenant.Address,
                IsActive = tenant.IsActive
            };

            return Ok(response);
        }

        // =====================================
        // 5. XÓA CHI NHÁNH (SOFT DELETE)
        // =====================================

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTenant(Guid id)
        {
            // 1. Tìm chi nhánh theo ID
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(x => x.TenantId == id);

            // 2. Kiểm tra chi nhánh có tồn tại không
            if (tenant == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chi nhánh."
                });
            }

            // 3. Chuyển trạng thái chi nhánh thành ngừng hoạt động
            tenant.IsActive = false;

            // 4. Lưu thay đổi vào Database
            await _context.SaveChangesAsync();

            // 5. Trả về kết quả
            return NoContent();
        }

        // =====================================
        // 6. KHÔI PHỤC CHI NHÁNH
        // =====================================

        [HttpPatch("{id:guid}/restore")]
        public async Task<ActionResult<TenantResponse>> RestoreTenant(Guid id)
        {
            // 1. Tìm chi nhánh theo ID
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(x => x.TenantId == id);

            // 2. Kiểm tra chi nhánh có tồn tại không
            if (tenant == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy chi nhánh."
                });
            }

            // 3. Kiểm tra trạng thái hiện tại
            if (tenant.IsActive)
            {
                return Conflict(new
                {
                    message = "Chi nhánh đang hoạt động."
                });
            }

            // 4. Khôi phục chi nhánh
            tenant.IsActive = true;

            // 5. Lưu thay đổi vào Database
            await _context.SaveChangesAsync();

            // 6. Trả về thông tin chi nhánh
            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                TenantCode = tenant.TenantCode,
                TenantName = tenant.TenantName,
                Address = tenant.Address,
                IsActive = tenant.IsActive
            };

            return Ok(response);
        }






    }
}
