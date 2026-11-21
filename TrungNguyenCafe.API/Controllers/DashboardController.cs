
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.API.Models.Dashboard;
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
    public class DashboardController : ControllerBase
    {
        private readonly CafeDbContext _context;

        public DashboardController(CafeDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryResponse>>
            GetSummary(CancellationToken cancellationToken)
        {
            // 1. Xác định phạm vi dữ liệu

            Guid? tenantId = null;

            bool isTenantUser =
                User.IsInRole(RoleNames.FranchiseOwner) ||
                User.IsInRole(RoleNames.StoreManager) ||
                User.IsInRole(RoleNames.SalesStaff);

            if (isTenantUser)
            {
                var tenantValue =
                    User.FindFirst("TenantId")?.Value;

                if (!Guid.TryParse(
                    tenantValue, out var currentTenantId))
                {
                    return Forbid();
                }

                var tenantActive = await _context.Tenants
                    .AnyAsync(x =>
                        x.TenantId == currentTenantId &&
                        x.IsActive,
                        cancellationToken);

                if (!tenantActive)
                    return Forbid();

                tenantId = currentTenantId;
            }

            // 2. Thống kê chi nhánh

            var tenantQuery = _context.Tenants.AsNoTracking();

            if (tenantId.HasValue)
            {
                tenantQuery = tenantQuery.Where(
                    x => x.TenantId == tenantId.Value);
            }

            int totalBranches = await tenantQuery
                .CountAsync(cancellationToken);

            int activeBranches = await tenantQuery
                .CountAsync(
                    x => x.IsActive,
                    cancellationToken);

            // 3. Thống kê tài khoản nhân sự đang hoạt động

            var employeeQuery =
                from user in _context.Users.AsNoTracking()

                join role in _context.Roles.AsNoTracking()
                    on user.RoleId equals (Guid?)role.RoleId

                where user.AccountStatus == "Active" &&
                    (
                        role.RoleName == RoleNames.FranchiseOwner ||
                        role.RoleName == RoleNames.StoreManager ||
                        role.RoleName == RoleNames.SalesStaff
                    )

                select user;

            if (tenantId.HasValue)
            {
                employeeQuery = employeeQuery.Where(
                    x => x.TenantId == tenantId.Value);
            }

            int activeEmployees = await employeeQuery
                .CountAsync(cancellationToken);

            // 4. Thời gian Việt Nam UTC+7

            var todayLocal = DateTime.UtcNow.AddHours(7).Date;

            var todayStartUtc = DateTime.SpecifyKind(
                todayLocal.AddHours(-7),
                DateTimeKind.Utc);

            var tomorrowStartUtc = todayStartUtc.AddDays(1);

            var sevenDaysStartUtc = todayStartUtc.AddDays(-6);

            // 5. Truy vấn đơn hàng theo phạm vi

            var orderQuery = _context.Orders.AsNoTracking();

            if (tenantId.HasValue)
            {
                orderQuery = orderQuery.Where(
                    x => x.TenantId == tenantId.Value);
            }

            // Chỉ lấy các đơn có liên quan trong 7 ngày gần nhất

            var recentOrders = await orderQuery
                .Where(x =>
                    (
                        x.CreatedAt >= sevenDaysStartUtc &&
                        x.CreatedAt < tomorrowStartUtc
                    )
                    ||
                    (
                        x.Status == "Paid" &&
                        x.PaidAt.HasValue &&
                        x.PaidAt.Value >= sevenDaysStartUtc &&
                        x.PaidAt.Value < tomorrowStartUtc
                    ))
                .Select(x => new
                {
                    x.CreatedAt,
                    x.Status,
                    x.PaidAt,
                    x.Total
                })
                .ToListAsync(cancellationToken);

            // 6. Tổng đơn hàng hôm nay

            int ordersToday = recentOrders.Count(x =>
                x.CreatedAt >= todayStartUtc &&
                x.CreatedAt < tomorrowStartUtc);

            // 7. Doanh thu hôm nay

            var todayPaidOrders = recentOrders
                .Where(x =>
                    x.Status == "Paid" &&
                    x.PaidAt.HasValue &&
                    x.PaidAt.Value >= todayStartUtc &&
                    x.PaidAt.Value < tomorrowStartUtc)
                .ToList();

            decimal revenueToday = todayPaidOrders
                .Sum(x => x.Total);

            // 8. Thống kê hoạt động 7 ngày

            var dailyActivity = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = todayLocal.AddDays(offset - 6);

                    var dayStartUtc = DateTime.SpecifyKind(
                        day.AddHours(-7),
                        DateTimeKind.Utc);

                    var dayEndUtc = dayStartUtc.AddDays(1);

                    var createdOrders = recentOrders
                        .Where(x =>
                            x.CreatedAt >= dayStartUtc &&
                            x.CreatedAt < dayEndUtc)
                        .ToList();

                    var paidOrders = recentOrders
                        .Where(x =>
                            x.Status == "Paid" &&
                            x.PaidAt.HasValue &&
                            x.PaidAt.Value >= dayStartUtc &&
                            x.PaidAt.Value < dayEndUtc)
                        .ToList();

                    return new DashboardDailyActivityItem
                    {
                        Date = day,

                        OrderCount = createdOrders.Count,

                        PaidOrderCount = paidOrders.Count,

                        Revenue = paidOrders.Sum(x => x.Total)
                    };
                })
                .ToList();

            // 9. Trả dữ liệu

            return Ok(new DashboardSummaryResponse
            {
                TotalBranches = totalBranches,

                ActiveBranches = activeBranches,

                ActiveEmployees = activeEmployees,

                OrdersToday = ordersToday,

                RevenueToday = revenueToday,

                DailyActivity = dailyActivity
            });
        }
    }
}
