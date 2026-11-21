
using System;
using System.Collections.Generic;
using TrungNguyenCafe.Web.Models.Orders;

namespace TrungNguyenCafe.Web.Models.Reports
{
    public class SalesReportViewModel
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public Guid? TenantId { get; set; }

        public bool CanFilterTenant { get; set; }

        public List<TenantOptionViewModel> Tenants { get; set; }
            = new();

        public decimal TotalRevenue { get; set; }

        public int PaidOrderCount { get; set; }

        public decimal AverageOrderValue { get; set; }

        public int TotalItemsSold { get; set; }

        public List<DailyRevenueViewModel> DailyRevenue { get; set; }
            = new();

        public List<TopProductViewModel> TopProducts { get; set; }
            = new();
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public int OrderCount { get; set; }
    }

    public class TopProductViewModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }
}
