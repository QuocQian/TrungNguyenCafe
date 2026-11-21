
using System;
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Products
{
    public class CreateProductRequest
    {
        // Tên sản phẩm
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(
            200,
            ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        // Giá bán
        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm.")]
        [Range(
            typeof(decimal),
            "0.01",
            "9999999999999999.99",
            ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
        public decimal Price { get; set; }

        // Chi nhánh áp dụng
        // Admin hệ thống phải chọn chi nhánh.
        // Chủ cửa hàng lấy TenantId từ JWT.
        public Guid? TenantId { get; set; }
    }
}
