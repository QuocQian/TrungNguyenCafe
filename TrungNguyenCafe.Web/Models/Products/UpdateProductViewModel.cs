
using System;
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.Web.Models.Products
{
    public class UpdateProductViewModel
    {
        // Mã sản phẩm
        public Guid ProductId { get; set; }

        // Tên sản phẩm
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(
            200,
            ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        // Giá bán
        [Range(
            typeof(decimal),
            "0.01",
            "9999999999999999.99",
            ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
        public decimal Price { get; set; }
    }
}
