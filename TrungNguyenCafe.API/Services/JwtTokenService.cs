
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using TrungNguyenCafe.Entities.Users;

namespace TrungNguyenCafe.API.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(User user, string roleName);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(User user, string roleName)
        {
            // 1. Đọc cấu hình JWT
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var expiresMinutesValue =
                _configuration["Jwt:ExpiresMinutes"];

            if (string.IsNullOrWhiteSpace(key) ||
                Encoding.UTF8.GetByteCount(key) < 32 ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "Cấu hình JWT không hợp lệ.");
            }

            if (!int.TryParse(
                expiresMinutesValue,
                out var expiresMinutes) ||
                expiresMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Thời hạn JWT không hợp lệ.");
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(
                    "Vai trò người dùng không hợp lệ.");
            }

            // 2. Tạo danh sách Claims
            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.UserId.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.UserName),

                new Claim(
                    ClaimTypes.Role,
                    roleName)
            };

            // 3. Thêm TenantId nếu là tài khoản thuộc chi nhánh
            if (user.TenantId.HasValue)
            {
                claims.Add(new Claim(
                    "TenantId",
                    user.TenantId.Value.ToString()));
            }

            // 4. Thêm RoleId
            if (user.RoleId.HasValue)
            {
                claims.Add(new Claim(
                    "RoleId",
                    user.RoleId.Value.ToString()));
            }

            // 5. Tạo khóa ký
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            // 6. Tạo JWT Token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    expiresMinutes),
                signingCredentials: credentials
            );

            // 7. Chuyển Token thành chuỗi
            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
