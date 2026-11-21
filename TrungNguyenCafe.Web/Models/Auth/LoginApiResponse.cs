
namespace TrungNguyenCafe.Web.Models.Auth
{
    public class LoginApiResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public DateTime ExpiresAtUtc { get; set; }

        public LoginApiUser User { get; set; } = new();
    }

    public class LoginApiUser
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }
    }
}
