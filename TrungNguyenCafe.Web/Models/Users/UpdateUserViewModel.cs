
using System;
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.Web.Models.Users
{
    public class UpdateUserViewModel
    {
        // Vai trò mới
        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public string RoleName { get; set; } = string.Empty;

        // Chi nhánh được phân công
        // Null đối với tài khoản cấp hệ thống
        public Guid? TenantId { get; set; }
    }
}
