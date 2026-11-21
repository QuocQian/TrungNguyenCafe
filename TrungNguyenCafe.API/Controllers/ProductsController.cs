
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.API.Models.Products;
using TrungNguyenCafe.Entities.Products;
using TrungNguyenCafe.Entities.Security;
using TrungNguyenCafe.Infrastructure.Data;

namespace TrungNguyenCafe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public ProductsController(CafeDbContext context)
        {
            _context = context;
        }

        // =====================================
        // 1. LẤY DANH SÁCH SẢN PHẨM
        // =====================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAllProducts(
    [FromQuery] Guid? tenantId = null,
    [FromQuery] bool includeInactive = false,
    CancellationToken cancellationToken = default)
        {
            var query = _context.Products.AsNoTracking();
            // Mặc định chỉ hiển thị sản phẩm đang kinh doanh
            if (!includeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            // Admin hệ thống được xem sản phẩm mọi chi nhánh
            if (User.IsInRole(RoleNames.SystemAdmin))
            {
                if (tenantId.HasValue)
                {
                    query = query.Where(
                        x => x.TenantId == tenantId.Value);
                }
            }
            else
            {
                // Chủ cửa hàng chỉ xem sản phẩm chi nhánh mình
                if (!TryGetCurrentTenantId(out var currentTenantId))
                {
                    return Forbid();
                }

                var tenantActive = await _context.Tenants
                    .AnyAsync(
                        x => x.TenantId == currentTenantId
                        && x.IsActive,
                        cancellationToken);

                if (!tenantActive)
                {
                    return Forbid();
                }

                query = query.Where(
                    x => x.TenantId == currentTenantId);
            }


            var products = await (
                from product in query

                join tenant in _context.Tenants.AsNoTracking()
                    on product.TenantId equals tenant.TenantId

                orderby tenant.TenantCode, product.ProductName

                select new ProductResponse
                {
                    ProductId = product.ProductId,

                    TenantId = product.TenantId,

                    TenantCode = tenant.TenantCode,

                    TenantName = tenant.TenantName,

                    TenantIsActive = tenant.IsActive,

                    ProductName = product.ProductName,

                    Price = product.Price,

                    IsActive = product.IsActive
                }

            ).ToListAsync(cancellationToken);


            return Ok(products);
        }

        // =====================================
        // 2. LẤY CHI TIẾT SẢN PHẨM
        // =====================================

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductResponse>> GetProductById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ProductId == id,
                    cancellationToken);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm."
                });
            }

            // Kiểm tra quyền truy cập chi nhánh
            if (!await CanManageTenantAsync(
                product.TenantId,
                cancellationToken))
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm."
                });
            }

            return Ok(ToResponse(product));
        }

        // =====================================
        // 3. TẠO SẢN PHẨM MỚI
        // =====================================

        [HttpPost]
        public async Task<ActionResult<ProductResponse>> CreateProduct(
            [FromBody] CreateProductRequest request,
            CancellationToken cancellationToken)
        {
            Guid targetTenantId;

            // Admin được chọn chi nhánh
            if (User.IsInRole(RoleNames.SystemAdmin))
            {
                if (!request.TenantId.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "Vui lòng chọn chi nhánh."
                    });
                }

                targetTenantId = request.TenantId.Value;
            }
            else
            {
                // Chủ cửa hàng lấy TenantId từ JWT
                // Không tin TenantId do Client gửi lên
                if (!TryGetCurrentTenantId(out targetTenantId))
                {
                    return Forbid();
                }
            }

            // Kiểm tra chi nhánh tồn tại và đang hoạt động
            var tenantExists = await _context.Tenants
                .AnyAsync(
                    x => x.TenantId == targetTenantId
                    && x.IsActive,
                    cancellationToken);

            if (!tenantExists)
            {
                return BadRequest(new
                {
                    message = "Chi nhánh không tồn tại hoặc đã ngừng hoạt động."
                });
            }

            // Chuẩn hóa tên sản phẩm
            var productName = request.ProductName.Trim();

            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(new
                {
                    message = "Tên sản phẩm không được để trống."
                });
            }

            // Tạo sản phẩm
            var product = new Product
            {
                ProductId = Guid.NewGuid(),

                TenantId = targetTenantId,

                ProductName = productName,

                Price = request.Price,

                IsActive = true
            };

            // Lưu Database
            _context.Products.Add(product);

            await _context.SaveChangesAsync(cancellationToken);

            // Trả kết quả
            var response = ToResponse(product);

            return CreatedAtAction(
                nameof(GetProductById),
                new { id = product.ProductId },
                response);
        }

        // =====================================
        // 4. CẬP NHẬT THÔNG TIN SẢN PHẨM
        // =====================================

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductResponse>> UpdateProduct(
            Guid id,
            [FromBody] UpdateProductRequest request,
            CancellationToken cancellationToken)
        {
            // Tìm sản phẩm
            var product = await _context.Products
                .FirstOrDefaultAsync(
                    x => x.ProductId == id,
                    cancellationToken);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm."
                });
            }

            // Kiểm tra quyền theo chi nhánh
            if (!await CanManageTenantAsync(
                product.TenantId,
                cancellationToken))
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm."
                });
            }

            // Chuẩn hóa tên sản phẩm
            var productName = request.ProductName.Trim();

            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(new
                {
                    message = "Tên sản phẩm không được để trống."
                });
            }

            // Cập nhật thông tin
            product.ProductName = productName;

            product.Price = request.Price;

            // Lưu Database
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToResponse(product));
        }

        // =====================================
        // 5. CẬP NHẬT TRẠNG THÁI SẢN PHẨM
        // =====================================

        [HttpPatch("{id:guid}/status")]
        public async Task<ActionResult<ProductResponse>> UpdateProductStatus(
            Guid id,
            [FromBody] bool isActive,
            CancellationToken cancellationToken)
        {
            // Tìm sản phẩm
            var product = await _context.Products
                .FirstOrDefaultAsync(
                    x => x.ProductId == id,
                    cancellationToken);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm."
                });
            }

            // Kiểm tra quyền theo chi nhánh
            if (!await CanManageTenantAsync(
                product.TenantId,
                cancellationToken))
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm."
                });
            }

            // Cập nhật trạng thái
            product.IsActive = isActive;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToResponse(product));
        }

        // =====================================
        // 6. KIỂM TRA QUYỀN CHI NHÁNH
        // =====================================

        private async Task<bool> CanManageTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            // Admin được quản lý tất cả chi nhánh
            if (User.IsInRole(RoleNames.SystemAdmin))
            {
                return true;
            }

            // Chủ cửa hàng chỉ được quản lý chi nhánh của mình
            if (!TryGetCurrentTenantId(out var currentTenantId))
            {
                return false;
            }

            if (currentTenantId != tenantId)
            {
                return false;
            }

            // Chi nhánh phải đang hoạt động
            return await _context.Tenants
                .AnyAsync(
                    x => x.TenantId == currentTenantId
                    && x.IsActive,
                    cancellationToken);
        }

        // =====================================
        // 7. LẤY TENANT ID TỪ JWT
        // =====================================

        private bool TryGetCurrentTenantId(out Guid tenantId)
        {
            var tenantIdValue = User.FindFirst("TenantId")?.Value;

            return Guid.TryParse(
                tenantIdValue,
                out tenantId);
        }

        // =====================================
        // 8. CHUYỂN ENTITY SANG RESPONSE
        // =====================================

        private static ProductResponse ToResponse(Product product)
        {
            return new ProductResponse
            {
                ProductId = product.ProductId,
                TenantId = product.TenantId,
                ProductName = product.ProductName,
                Price = product.Price,
                IsActive = product.IsActive
            };
        }
    }
}
