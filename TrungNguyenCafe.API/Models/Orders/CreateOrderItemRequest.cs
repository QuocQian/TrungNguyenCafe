using System;
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Orders
{
    public class CreateOrderItemRequest
    {
        // Mã sản phẩm
        public Guid ProductId { get; set; }

        // Số lượng
        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}