using System;
using System.Collections.Generic;

namespace TrungNguyenCafe.API.Models.Orders
{
    public class OrderResponse
    {
        public Guid OrderId { get; set; }

        public Guid TenantId { get; set; }

        public Guid UserId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; }

        public DateTime? PaidAt { get; set; }

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderItemResponse> Items { get; set; }
            = new List<OrderItemResponse>();
    }
}