
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.API.Models.Orders;
using TrungNguyenCafe.Entities.Orders;
using TrungNguyenCafe.Entities.Security;
using TrungNguyenCafe.Infrastructure.Data;

namespace TrungNguyenCafe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles =
        RoleNames.SystemAdmin + "," +
        RoleNames.HQManager + "," +
        RoleNames.FranchiseOwner + "," +
        RoleNames.StoreManager + "," +
        RoleNames.SalesStaff)]
    public class OrdersController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public OrdersController(CafeDbContext context)
        {
            _context = context;
        }

        // =====================================
        // 1. TẠO ĐƠN HÀNG
        // =====================================

        [HttpPost]
        [Authorize(Roles =
            RoleNames.SystemAdmin + "," +
            RoleNames.FranchiseOwner + "," +
            RoleNames.StoreManager + "," +
            RoleNames.SalesStaff)]
        public async Task<ActionResult<OrderResponse>> CreateOrder(
            [FromBody] CreateOrderRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra danh sách sản phẩm
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Đơn hàng phải có ít nhất một sản phẩm."
                });
            }

            // 2. Kiểm tra dữ liệu đầu vào
            if (request.Items.Any(x =>
                x == null ||
                x.ProductId == Guid.Empty ||
                x.Quantity <= 0))
            {
                return BadRequest(new
                {
                    message = "Thông tin sản phẩm hoặc số lượng không hợp lệ."
                });
            }

            // 3. Xác định tài khoản Admin
            bool isSystemAdmin =
                User.IsInRole(RoleNames.SystemAdmin);

            // 4. Xác định chi nhánh tạo đơn
            Guid tenantId;

            if (isSystemAdmin)
            {
                // Admin phải chọn chi nhánh
                if (!request.TenantId.HasValue ||
                    request.TenantId.Value == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        message = "Admin vui lòng chọn chi nhánh tạo đơn."
                    });
                }

                tenantId = request.TenantId.Value;
            }
            else
            {
                // Các tài khoản cửa hàng
                // lấy TenantId từ JWT
                if (!TryGetCurrentTenantId(out tenantId))
                {
                    return Forbid();
                }

                // Không cho phép chọn chi nhánh khác
                if (request.TenantId.HasValue &&
                    request.TenantId.Value != tenantId)
                {
                    return Forbid();
                }
            }

            // 5. Lấy UserId từ JWT
            if (!TryGetCurrentUserId(out var userId))
            {
                return Forbid();
            }

            // 6. Kiểm tra chi nhánh hoạt động
            var tenantActive = await _context.Tenants
                .AnyAsync(
                    x => x.TenantId == tenantId
                    && x.IsActive,
                    cancellationToken);

            if (!tenantActive)
            {
                return BadRequest(new
                {
                    message = "Chi nhánh không tồn tại hoặc đã ngừng hoạt động."
                });
            }

            // 7. Kiểm tra tài khoản người tạo đơn
            var userQuery = _context.Users
                .Where(x =>
                    x.UserId == userId &&
                    x.AccountStatus == "Active");

            // Admin hệ thống không bắt buộc thuộc chi nhánh
            if (!isSystemAdmin)
            {
                userQuery = userQuery.Where(
                    x => x.TenantId == tenantId);
            }

            var userActive = await userQuery
                .AnyAsync(cancellationToken);

            if (!userActive)
            {
                return Forbid();
            }

            // 8. Gom các sản phẩm trùng mã
            var requestedItems = request.Items
                .GroupBy(x => x.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(x => x.Quantity)
                })
                .ToList();

            var productIds = requestedItems
                .Select(x => x.ProductId)
                .ToList();

            // 9. Lấy sản phẩm từ Database
            // Chỉ lấy sản phẩm thuộc chi nhánh được chọn
            var products = await _context.Products
                .Where(x =>
                    productIds.Contains(x.ProductId)
                    && x.TenantId == tenantId
                    && x.IsActive)
                .ToListAsync(cancellationToken);

            // 10. Kiểm tra sản phẩm
            if (products.Count != requestedItems.Count)
            {
                return BadRequest(new
                {
                    message = "Có sản phẩm không tồn tại, không thuộc chi nhánh hoặc đã ngừng kinh doanh."
                });
            }

            if (products.Any(x => x.Price < 0))
            {
                return BadRequest(new
                {
                    message = "Giá sản phẩm không hợp lệ."
                });
            }

            var productDictionary = products
                .ToDictionary(x => x.ProductId);

            // 11. Tạo đơn hàng
            var order = new Order
            {
                OrderId = Guid.NewGuid(),

                TenantId = tenantId,

                UserId = userId,

                Status = "Pending",

                PaymentMethod = null,

                PaidAt = null,

                Total = 0,

                CreatedAt = DateTime.UtcNow,

                Items = new List<OrderItem>()
            };

            // 12. Tạo chi tiết đơn hàng
            foreach (var item in requestedItems)
            {
                var product = productDictionary[item.ProductId];

                var orderItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),

                    OrderId = order.OrderId,

                    ProductId = product.ProductId,

                    Quantity = item.Quantity,

                    UnitPrice = product.Price
                };

                order.Items.Add(orderItem);

                // Tính tổng tiền tại Backend
                order.Total +=
                    product.Price * item.Quantity;
            }

            // 13. Lưu Database
            _context.Orders.Add(order);

            await _context.SaveChangesAsync(cancellationToken);

            // 14. Trả kết quả
            var response = await BuildOrderResponseAsync(
                order,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = order.OrderId },
                response);
        }


        // =====================================
        // 2. LẤY DANH SÁCH ĐƠN HÀNG
        // =====================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponse>>>
            GetAllOrders(
                [FromQuery] Guid? tenantId = null,
                [FromQuery] DateTime? fromUtc = null,
                [FromQuery] DateTime? toUtc = null,
                CancellationToken cancellationToken = default)
        {
            if (fromUtc.HasValue &&
                toUtc.HasValue &&
                fromUtc.Value > toUtc.Value)
            {
                return BadRequest(new
                {
                    message = "Khoảng thời gian không hợp lệ."
                });
            }

            var query = _context.Orders
                .AsNoTracking();

            // Nhân viên, chủ cửa hàng và quản lý cửa hàng
            // chỉ xem đơn hàng của chi nhánh mình
            if (IsTenantRole())
            {
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
            else if (tenantId.HasValue)
            {
                // Admin và HQManager có thể lọc chi nhánh
                query = query.Where(
                    x => x.TenantId == tenantId.Value);
            }

            // Lọc thời gian UTC
            if (fromUtc.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt <= toUtc.Value);
            }

            var orders = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            // Lấy danh sách mã đơn hàng
            var orderIds = orders
                .Select(x => x.OrderId)
                .ToList();

            // Lấy chi tiết sản phẩm
            var itemRows = await (
                from item in _context.OrderItems.AsNoTracking()

                join product in _context.Products.AsNoTracking()
                    on item.ProductId equals product.ProductId

                where orderIds.Contains(item.OrderId)

                select new
                {
                    item.OrderId,

                    Item = new OrderItemResponse
                    {
                        ProductId = item.ProductId,

                        ProductName = product.ProductName,

                        Quantity = item.Quantity,

                        UnitPrice = item.UnitPrice,

                        Subtotal = item.Quantity * item.UnitPrice
                    }
                }
            ).ToListAsync(cancellationToken);

            var itemLookup = itemRows
                .GroupBy(x => x.OrderId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.Item).ToList());

            // Chuyển Entity sang Response
            var responses = orders.Select(order =>
                new OrderResponse
                {
                    OrderId = order.OrderId,

                    TenantId = order.TenantId,

                    UserId = order.UserId,

                    Status = order.Status,

                    PaymentMethod = order.PaymentMethod,

                    PaidAt = order.PaidAt,

                    Total = order.Total,

                    CreatedAt = order.CreatedAt,

                    Items = itemLookup.TryGetValue(
                        order.OrderId,
                        out var items)
                            ? items
                            : new List<OrderItemResponse>()
                }
            ).ToList();

            return Ok(responses);
        }


        // =====================================
        // 3. LẤY CHI TIẾT ĐƠN HÀNG
        // =====================================

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderResponse>> GetOrderById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(x => x.OrderId == id);

            // Kiểm soát Tenant
            if (IsTenantRole())
            {
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

            var order = await query
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy đơn hàng."
                });
            }

            var response = await BuildOrderResponseAsync(
                order,
                cancellationToken);

            return Ok(response);
        }


        // =====================================
        // 4. THANH TOÁN TIỀN MẶT
        // =====================================


        [HttpPatch("{id:guid}/cash-payment")]
        [Authorize(Roles =
            RoleNames.SystemAdmin + "," +
            RoleNames.FranchiseOwner + "," +
            RoleNames.StoreManager + "," +
            RoleNames.SalesStaff)]
        public async Task<ActionResult<OrderResponse>>
            RecordCashPayment(
                Guid id,
                CancellationToken cancellationToken)
        {
            bool isAdmin = User.IsInRole(RoleNames.SystemAdmin);

            if (!TryGetCurrentUserId(out var userId))
                return Forbid();

            Guid? tenantId = null;

            // Kiểm tra quyền Admin
            if (isAdmin)
            {
                var adminActive = await _context.Users.AnyAsync(
                    x => x.UserId == userId
                    && x.AccountStatus == "Active",
                    cancellationToken);

                if (!adminActive)
                    return Forbid();
            }
            else
            {
                // Nhân viên và quản lý chỉ thanh toán tại chi nhánh mình
                if (!TryGetCurrentTenantId(out var currentTenantId))
                    return Forbid();

                tenantId = currentTenantId;

                var tenantActive = await _context.Tenants.AnyAsync(
                    x => x.TenantId == currentTenantId
                    && x.IsActive,
                    cancellationToken);

                if (!tenantActive)
                    return Forbid();

                var userActive = await _context.Users.AnyAsync(
                    x => x.UserId == userId
                    && x.TenantId == currentTenantId
                    && x.AccountStatus == "Active",
                    cancellationToken);

                if (!userActive)
                    return Forbid();
            }

            // Tìm đơn hàng
            var query = _context.Orders.Where(
                x => x.OrderId == id);

            if (tenantId.HasValue)
            {
                query = query.Where(
                    x => x.TenantId == tenantId.Value);
            }

            var order = await query.FirstOrDefaultAsync(
                cancellationToken);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy đơn hàng."
                });
            }

            // Chi nhánh của đơn hàng phải đang hoạt động
            var orderTenantActive = await _context.Tenants.AnyAsync(
                x => x.TenantId == order.TenantId
                && x.IsActive,
                cancellationToken);

            if (!orderTenantActive)
                return Forbid();

            // Không cho thanh toán lần thứ hai
            if (order.Status == "Paid")
            {
                return Conflict(new
                {
                    message = "Đơn hàng đã được thanh toán."
                });
            }

            if (order.Status != "Pending")
            {
                return Conflict(new
                {
                    message = "Trạng thái đơn hàng không hợp lệ."
                });
            }

            // Ghi nhận thanh toán
            order.Status = "Paid";
            order.PaymentMethod = "Cash";
            order.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var response = await BuildOrderResponseAsync(
                order,
                cancellationToken);

            return Ok(response);
        }



        // =====================================
        // 5. HÀM CHUYỂN ORDER SANG RESPONSE
        // =====================================

        private async Task<OrderResponse> BuildOrderResponseAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            var items = await (
                from item in _context.OrderItems.AsNoTracking()

                join product in _context.Products.AsNoTracking()
                    on item.ProductId equals product.ProductId

                where item.OrderId == order.OrderId

                select new OrderItemResponse
                {
                    ProductId = item.ProductId,

                    ProductName = product.ProductName,

                    Quantity = item.Quantity,

                    UnitPrice = item.UnitPrice,

                    Subtotal = item.Quantity * item.UnitPrice
                }
            ).ToListAsync(cancellationToken);

            return new OrderResponse
            {
                OrderId = order.OrderId,

                TenantId = order.TenantId,

                UserId = order.UserId,

                Status = order.Status,

                PaymentMethod = order.PaymentMethod,

                PaidAt = order.PaidAt,

                Total = order.Total,

                CreatedAt = order.CreatedAt,

                Items = items
            };
        }


        // =====================================
        // 6. KIỂM TRA VAI TRÒ CHI NHÁNH
        // =====================================

        private bool IsTenantRole()
        {
            return User.IsInRole(RoleNames.FranchiseOwner)
                || User.IsInRole(RoleNames.StoreManager)
                || User.IsInRole(RoleNames.SalesStaff);
        }


        // =====================================
        // 7. LẤY USER ID TỪ JWT
        // =====================================

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var userIdValue =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(
                userIdValue,
                out userId);
        }


        // =====================================
        // 8. LẤY TENANT ID TỪ JWT
        // =====================================

        private bool TryGetCurrentTenantId(out Guid tenantId)
        {
            var tenantIdValue =
                User.FindFirst("TenantId")?.Value;

            return Guid.TryParse(
                tenantIdValue,
                out tenantId);
        }
    }
}
