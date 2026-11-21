
using System;

namespace TrungNguyenCafe.Entities.Products
{
    public class Product
    {
        // Mã định danh sản phẩm
        public Guid ProductId { get; set; }

        // Sản phẩm thuộc chi nhánh nào
        public Guid TenantId { get; set; }

        // Tên sản phẩm
        public string ProductName { get; set; } = string.Empty;

        // Giá bán
        public decimal Price { get; set; }

        // Trạng thái kinh doanh
        public bool IsActive { get; set; } = true;
    }
}
