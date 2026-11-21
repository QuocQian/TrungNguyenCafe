
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.Web.Models.Tenants
{
    public class CreateTenantViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã chi nhánh.")]
        [StringLength(50, ErrorMessage = "Mã chi nhánh tối đa 50 ký tự.")]
        public string TenantCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh.")]
        [StringLength(200, ErrorMessage = "Tên chi nhánh tối đa 200 ký tự.")]
        public string TenantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
        public string Address { get; set; } = string.Empty;
    }
}
