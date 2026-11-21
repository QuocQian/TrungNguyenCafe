
using System;

namespace TrungNguyenCafe.Entities.Orders
{
    public class OrderItem
    {
        // Mã chi tiết đơn hàng
        public Guid OrderItemId { get; set; }

        // Mã đơn hàng
        public Guid OrderId { get; set; }

        // Mã sản phẩm
        public Guid ProductId { get; set; }

        // Số lượng sản phẩm
        public int Quantity { get; set; }

        // Đơn giá tại thời điểm tạo đơn
        public decimal UnitPrice { get; set; }

        // Quan hệ với đơn hàng
        public Order? Order { get; set; }
    }
}
