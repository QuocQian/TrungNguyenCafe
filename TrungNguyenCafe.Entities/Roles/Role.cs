
using System;

namespace TrungNguyenCafe.Entities.Roles
{
    public class Role
    {
        // Khóa chính của vai trò
        public Guid RoleId { get; set; } = Guid.NewGuid();

        // Tên vai trò trong hệ thống
        public string RoleName { get; set; } = string.Empty;
    }
}
