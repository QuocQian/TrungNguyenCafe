
using System;

namespace TrungNguyenCafe.Entities.Users
{
    public class User
    {
        // Khóa chính của người dùng
        public Guid UserId { get; set; } = Guid.NewGuid();

        // Tên đăng nhập
        public string UserName { get; set; } = string.Empty;

        // Mật khẩu đã được băm
        public string PasswordHash { get; set; } = string.Empty;

        // Trạng thái tài khoản: Active / Locked
        public string AccountStatus { get; set; } = string.Empty;

        // Mã chi nhánh (Tenant)
        public Guid? TenantId { get; set; }

        // Mã vai trò (Role)
        public Guid? RoleId { get; set; }
    }
}
