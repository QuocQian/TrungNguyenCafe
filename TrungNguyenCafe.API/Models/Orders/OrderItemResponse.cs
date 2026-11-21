using System;

namespace TrungNguyenCafe.API.Models.Orders
{
    public class OrderItemResponse
    {
        // Mã sản phẩm
        public Guid ProductId { get; set; }

        // Tên sản phẩm
        public string ProductName { get; set; } = string.Empty;

        // Số lượng
        public int Quantity { get; set; }

        // Đơn giá
        public decimal UnitPrice { get; set; }

        // Thành tiền
        public decimal Subtotal { get; set; }
    }
}