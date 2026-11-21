
namespace TrungNguyenCafe.API.Models.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public DateTime ExpiresAtUtc { get; set; }

        public LoginUserInfo User { get; set; } = new();
    }

    public class LoginUserInfo
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }
    }
}
