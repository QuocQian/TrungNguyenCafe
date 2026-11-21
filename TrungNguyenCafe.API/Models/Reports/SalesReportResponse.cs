
using System;
using System.Collections.Generic;

namespace TrungNguyenCafe.API.Models.Reports
{
    public class SalesReportResponse
    {
        public decimal TotalRevenue { get; set; }

        public int PaidOrderCount { get; set; }

        public decimal AverageOrderValue { get; set; }

        public int TotalItemsSold { get; set; }

        public List<DailyRevenueReportItem> DailyRevenue
        { get; set; } = new();

        public List<TopProductReportItem> TopProducts
        { get; set; } = new();
    }

    public class DailyRevenueReportItem
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public int OrderCount { get; set; }
    }

    public class TopProductReportItem
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }
}
