
using System;

namespace TrungNguyenCafe.Web.Models.Products
{
    public class ProductViewModel
    {
        public Guid ProductId { get; set; }

        public Guid TenantId { get; set; }

        public string TenantCode { get; set; } = string.Empty;

        public string TenantName { get; set; } = string.Empty;

        public bool TenantIsActive { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public bool IsActive { get; set; }
    }
}
