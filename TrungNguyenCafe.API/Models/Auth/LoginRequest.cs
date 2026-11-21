
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string Password { get; set; } = string.Empty;

        // Chỉ cần nhập đối với tài khoản thuộc chi nhánh
        [StringLength(50)]
        public string? TenantCode { get; set; }
    }
}
