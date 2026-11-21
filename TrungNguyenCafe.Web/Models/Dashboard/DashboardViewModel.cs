
using System;
using System.Collections.Generic;

namespace TrungNguyenCafe.Web.Models.Dashboard
{
    public class DashboardViewModel
    {
        public DateTime Today { get; set; }

        public bool IsLoaded { get; set; }

        public string? ErrorMessage { get; set; }

        public int TotalBranches { get; set; }

        public int ActiveBranches { get; set; }

        public int ActiveEmployees { get; set; }

        public int OrdersToday { get; set; }

        public decimal RevenueToday { get; set; }

        public List<DashboardDailyActivityViewModel>
            DailyActivity
        { get; set; } = new();
    }

    public class DashboardDailyActivityViewModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public int OrderCount { get; set; }

        public int PaidOrderCount { get; set; }
    }
}
