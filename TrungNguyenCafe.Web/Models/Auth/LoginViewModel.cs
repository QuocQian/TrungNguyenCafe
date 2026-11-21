
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.Web.Models.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [StringLength(
            50,
            ErrorMessage = "Mã chi nhánh tối đa 50 ký tự.")]
        [Display(Name = "Mã chi nhánh")]
        public string? TenantCode { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
