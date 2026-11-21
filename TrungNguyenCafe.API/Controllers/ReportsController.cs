
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.API.Models.Reports;
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
        RoleNames.StoreManager)]
    public class ReportsController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public ReportsController(CafeDbContext context)
        {
            _context = context;
        }

        // =====================================
        // 1. BÁO CÁO DOANH THU
        // =====================================

        [HttpGet("sales")]
        public async Task<ActionResult<SalesReportResponse>>
            GetSalesReport(
                [FromQuery] DateTime fromUtc,
                [FromQuery] DateTime toUtcExclusive,
                [FromQuery] Guid? tenantId = null,
                CancellationToken cancellationToken = default)
        {
            // 1. Kiểm tra khoảng thời gian

            if (fromUtc == default ||
                toUtcExclusive == default ||
                fromUtc >= toUtcExclusive)
            {
                return BadRequest(new
                {
                    message = "Khoảng thời gian không hợp lệ."
                });
            }

            // 2. Lấy các đơn đã thanh toán

            var query = _context.Orders
                .AsNoTracking()
                .Where(x =>
                    x.Status == "Paid" &&
                    x.PaidAt.HasValue &&
                    x.PaidAt.Value >= fromUtc &&
                    x.PaidAt.Value < toUtcExclusive);

            // 3. Phân quyền theo chi nhánh

            bool isTenantUser =
                User.IsInRole(RoleNames.FranchiseOwner) ||
                User.IsInRole(RoleNames.StoreManager);

            if (isTenantUser)
            {
                var tenantValue =
                    User.FindFirst("TenantId")?.Value;

                if (!Guid.TryParse(tenantValue, out var currentTenantId))
                    return Forbid();

                var tenantActive = await _context.Tenants
                    .AnyAsync(x =>
                        x.TenantId == currentTenantId &&
                        x.IsActive,
                        cancellationToken);

                if (!tenantActive)
                    return Forbid();

                // Chỉ xem doanh thu chi nhánh của mình

                query = query.Where(
                    x => x.TenantId == currentTenantId);
            }
            else if (tenantId.HasValue)
            {
                // Admin và HQManager được lọc chi nhánh

                var tenantExists = await _context.Tenants
                    .AnyAsync(x =>
                        x.TenantId == tenantId.Value &&
                        x.IsActive,
                        cancellationToken);

                if (!tenantExists)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy chi nhánh."
                    });
                }

                query = query.Where(
                    x => x.TenantId == tenantId.Value);
            }

            // 4. Tổng doanh thu

            var totalRevenue = await query
                .SumAsync(
                    x => (decimal?)x.Total,
                    cancellationToken) ?? 0m;

            // 5. Tổng số đơn đã thanh toán

            var paidOrderCount = await query
                .CountAsync(cancellationToken);

            // 6. Giá trị trung bình mỗi đơn

            var averageOrderValue =
                paidOrderCount > 0
                    ? totalRevenue / paidOrderCount
                    : 0m;

            // 7. Tổng số sản phẩm đã bán

            var paidOrderIds = query.Select(
                x => x.OrderId);

            var totalItemsSold = await _context.OrderItems
                .AsNoTracking()
                .Where(x => paidOrderIds.Contains(x.OrderId))
                .SumAsync(
                    x => (int?)x.Quantity,
                    cancellationToken) ?? 0;

            // 8. Doanh thu theo ngày
            // Dữ liệu lưu UTC, chuyển sang ngày Việt Nam UTC+7

            var dailyRevenue = await query
                .GroupBy(x => x.PaidAt!.Value.AddHours(7).Date)
                .Select(group => new DailyRevenueReportItem
                {
                    Date = group.Key,

                    Revenue = group.Sum(x => x.Total),

                    OrderCount = group.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            // 9. Top 5 sản phẩm bán chạy

            var topProducts = await (
                from item in _context.OrderItems.AsNoTracking()

                join product in _context.Products.AsNoTracking()
                    on item.ProductId equals product.ProductId

                join order in query
                    on item.OrderId equals order.OrderId

                group new { item, product }
                    by new
                    {
                        item.ProductId,
                        product.ProductName
                    }
                    into grouped

                select new TopProductReportItem
                {
                    ProductId = grouped.Key.ProductId,

                    ProductName = grouped.Key.ProductName,

                    QuantitySold = grouped.Sum(
                        x => x.item.Quantity),

                    Revenue = grouped.Sum(
                        x => x.item.Quantity * x.item.UnitPrice)
                }

            )
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync(cancellationToken);

            // 10. Trả kết quả

            var response = new SalesReportResponse
            {
                TotalRevenue = totalRevenue,

                PaidOrderCount = paidOrderCount,

                AverageOrderValue = averageOrderValue,

                TotalItemsSold = totalItemsSold,

                DailyRevenue = dailyRevenue,

                TopProducts = topProducts
            };

            return Ok(response);
        }
    }
}
