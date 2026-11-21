
using System;
using System.Collections.Generic;

namespace TrungNguyenCafe.API.Models.Dashboard
{
    public class DashboardSummaryResponse
    {
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int ActiveEmployees { get; set; }
        public int OrdersToday { get; set; }
        public decimal RevenueToday { get; set; }

        public List<DashboardDailyActivityItem> DailyActivity
        { get; set; } = new();
    }

    public class DashboardDailyActivityItem
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public int PaidOrderCount { get; set; }
    }
}
