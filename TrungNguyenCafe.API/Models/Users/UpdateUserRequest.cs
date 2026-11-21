
using System.ComponentModel.DataAnnotations;

namespace TrungNguyenCafe.API.Models.Users
{
    public class UpdateUserRequest
    {
        // Vai trò mới
        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public string RoleName { get; set; } = string.Empty;

        // Chi nhánh được phân công
        public Guid? TenantId { get; set; }
    }
}
