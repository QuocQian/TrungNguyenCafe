
using Microsoft.EntityFrameworkCore;

using TrungNguyenCafe.Entities.Users;
using TrungNguyenCafe.Entities.Roles;
using TrungNguyenCafe.Entities.Security;
namespace TrungNguyenCafe.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
            CafeDbContext context,
            string adminUserName,
            string adminPasswordHash,
            CancellationToken cancellationToken = default)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(adminUserName))
            {
                throw new ArgumentException(
                    "Admin username không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(adminPasswordHash))
            {
                throw new ArgumentException(
                    "Admin password hash không được để trống.");
            }

            // 2. Danh sách vai trò mặc định
            string[] roleNames = RoleNames.All;

            // 3. Tạo các vai trò nếu chưa tồn tại
            foreach (var roleName in roleNames)
            {
                bool roleExists = await context.Roles
                    .AnyAsync(
                        x => x.RoleName == roleName,
                        cancellationToken);

                if (!roleExists)
                {
                    context.Roles.Add(new Role
                    {
                        RoleName = roleName
                    });
                }
            }

            // Lưu các vai trò vào database
            await context.SaveChangesAsync(cancellationToken);

            // 4. Lấy vai trò Admin
            var adminRole = await context.Roles
    .SingleAsync(
        x => x.RoleName == RoleNames.SystemAdmin,
        cancellationToken);

            // 5. Kiểm tra tài khoản Admin đã tồn tại chưa
            var existingAdmin = await context.Users
                .SingleOrDefaultAsync(
                    x => x.TenantId == null
                    && x.UserName == adminUserName,
                    cancellationToken);

            // 6. Tạo tài khoản Admin nếu chưa tồn tại
            if (existingAdmin == null)
            {
                var admin = new User
                {
                    UserName = adminUserName,

                    PasswordHash = adminPasswordHash,

                    AccountStatus = "Active",

                    TenantId = null,

                    RoleId = adminRole.RoleId
                };

                context.Users.Add(admin);

                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
