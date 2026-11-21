
using System;
using System.Collections.Generic;

namespace TrungNguyenCafe.Web.Models.Orders
{
    // Sản phẩm hiển thị trên màn hình POS
    public class PosProductViewModel
    {
        public Guid ProductId { get; set; }
        public Guid TenantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    // Chi nhánh
    public class TenantOptionViewModel
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string? TenantCode { get; set; }
    }

    // Màn hình bán hàng
    public class PosViewModel
    {
        public bool IsAdmin { get; set; }
        public Guid? TenantId { get; set; }

        public List<TenantOptionViewModel> Tenants { get; set; }
            = new();

        public List<PosProductViewModel> Products { get; set; }
            = new();
    }

    // Dữ liệu gửi tạo đơn hàng
    public class CreateOrderViewModel
    {
        public Guid? TenantId { get; set; }

        public List<CreateOrderItemViewModel> Items { get; set; }
            = new();
    }

    public class CreateOrderItemViewModel
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // Chi tiết sản phẩm trong đơn
    public class OrderItemViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    // Thông tin đơn hàng
    public class OrderViewModel
    {
        public Guid OrderId { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; }

        public DateTime? PaidAt { get; set; }

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderItemViewModel> Items { get; set; }
            = new();
    }

    // Danh sách đơn hàng
    public class OrderHistoryViewModel
    {
        public Guid? TenantId { get; set; }

        public bool CanFilterTenant { get; set; }

        public List<TenantOptionViewModel> Tenants { get; set; }
            = new();

        public List<OrderViewModel> Orders { get; set; }
            = new();
    }
}
