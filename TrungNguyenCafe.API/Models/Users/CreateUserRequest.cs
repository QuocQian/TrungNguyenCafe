
using System;
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Users
{
    public class CreateUserRequest
    {
        // Tên đăng nhập
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(
            100,
            ErrorMessage = "Tên đăng nhập tối đa 100 ký tự.")]
        public string UserName { get; set; } = string.Empty;

        // Mật khẩu
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự.")]
        public string Password { get; set; } = string.Empty;

        // Vai trò tài khoản
        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public string RoleName { get; set; } = string.Empty;

        // Chi nhánh được phân công
        // Null đối với tài khoản cấp hệ thống
        public Guid? TenantId { get; set; }
    }
}
