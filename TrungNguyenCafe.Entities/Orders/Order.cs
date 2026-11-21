
using System;
using System.Collections.Generic;

namespace TrungNguyenCafe.Entities.Orders
{
    public class Order
    {
        // Mã đơn hàng
        public Guid OrderId { get; set; }

        // Chi nhánh tạo đơn
        public Guid TenantId { get; set; }

        // Nhân viên tạo đơn
        public Guid UserId { get; set; }

        // Trạng thái hóa đơn
        // Pending: Chưa thanh toán
        // Paid: Đã thanh toán
        // Trạng thái hóa đơn
        // Pending: Chưa thanh toán
        // Paid: Đã thanh toán
        public string Status { get; set; } = "Pending";

        // Phương thức thanh toán
        // Cash: Tiền mặt
        public string? PaymentMethod { get; set; }

        // Thời điểm thanh toán
        public DateTime? PaidAt { get; set; }

        // Tổng tiền đơn hàng
        public decimal Total { get; set; }

        // Thời gian tạo đơn
        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        // Danh sách chi tiết sản phẩm
        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();
    }
}
