using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Orders
{
    public class CreateOrderRequest
    {
        // Chi nhánh tạo đơn
        // Chỉ Admin bắt buộc phải truyền TenantId
        // Các tài khoản cửa hàng lấy TenantId từ JWT
        public Guid? TenantId { get; set; }

        // Danh sách sản phẩm trong đơn hàng
        [Required]
        [MinLength(1)]
        public List<CreateOrderItemRequest> Items { get; set; }
            = new List<CreateOrderItemRequest>();
    }
}