
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Tenants
{
    public class CreateTenantRequest
    {
        // Mã chi nhánh
        [Required(ErrorMessage = "Vui lòng nhập mã chi nhánh.")]
        [StringLength(50, ErrorMessage = "Mã chi nhánh không được vượt quá 50 ký tự.")]
        public string TenantCode { get; set; } = string.Empty;

        // Tên chi nhánh
        [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh.")]
        [StringLength(200, ErrorMessage = "Tên chi nhánh không được vượt quá 200 ký tự.")]
        public string TenantName { get; set; } = string.Empty;

        // Địa chỉ chi nhánh
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi nhánh.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        public string Address { get; set; } = string.Empty;
    }
}
