
namespace TrungNguyenCafe.API.Models.Users
{
    public class UserResponse
    {
        // Mã định danh tài khoản
        public Guid UserId { get; set; }

        // Tên đăng nhập
        public string UserName { get; set; } = string.Empty;

        // Trạng thái tài khoản
        public string AccountStatus { get; set; } = string.Empty;

        // Mã vai trò
        public Guid? RoleId { get; set; }

        // Tên vai trò
        public string RoleName { get; set; } = string.Empty;

        // Mã chi nhánh
        public Guid? TenantId { get; set; }

        // Mã chi nhánh
        public string? TenantCode { get; set; }

        // Tên chi nhánh
        public string? TenantName { get; set; }
    }
}
