
using Microsoft.EntityFrameworkCore;
using TrungNguyenCafe.Entities.Products;
using TrungNguyenCafe.Entities.Users;
using TrungNguyenCafe.Entities.Roles;
using TrungNguyenCafe.Entities.Tenants;
using TrungNguyenCafe.Entities.Orders;
namespace TrungNguyenCafe.Infrastructure.Data
{
    public class CafeDbContext : DbContext
    {
        // Khởi tạo DbContext
        public CafeDbContext(
            DbContextOptions<CafeDbContext> options)
            : base(options)
        {
        }

        // Các bảng dữ liệu
        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        // Cấu hình cấu trúc database
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================
            // 1. BẢNG USERS
            // =====================================

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.UserId);

                entity.Property(x => x.UserName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(x => x.AccountStatus)
                    .IsRequired()
                    .HasMaxLength(20);

                // Tài khoản thuộc chi nhánh:
                // Username không được trùng trong cùng chi nhánh
                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.UserName
                })
                .IsUnique()
                .HasFilter("[TenantId] IS NOT NULL");

                // Tài khoản toàn hệ thống:
                // Username không được trùng giữa các tài khoản
                // có TenantId = NULL
                entity.HasIndex(x => x.UserName)
                    .IsUnique()
                    .HasFilter("[TenantId] IS NULL");

                // Quan hệ với Role
                entity.HasOne<Role>()
                    .WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Quan hệ với Tenant
                entity.HasOne<Tenant>()
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================
            // 2. BẢNG ROLES
            // =====================================

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(x => x.RoleId);

                entity.Property(x => x.RoleName)
                    .IsRequired()
                    .HasMaxLength(100);

                // Tên vai trò không được trùng
                entity.HasIndex(x => x.RoleName)
                    .IsUnique();
            });

            // =====================================
            // 3. BẢNG TENANTS
            // =====================================

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(x => x.TenantId);

                entity.Property(x => x.TenantCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.TenantName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Address)
                    .IsRequired()
                    .HasMaxLength(500);

                // Mã chi nhánh không được trùng
                entity.HasIndex(x => x.TenantCode)
                    .IsUnique();
            });

            // =====================================
            // 4. BẢNG PRODUCTS
            // =====================================

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(x => x.ProductId);

                entity.Property(x => x.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Price)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                // Quan hệ với Tenant
                entity.HasOne<Tenant>()
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Tối ưu tìm kiếm sản phẩm theo chi nhánh
                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductName
                });
            });
            // =====================================
            // 5. BẢNG ORDERS
            // =====================================

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(x => x.OrderId);

                entity.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(x => x.PaymentMethod)
    .HasMaxLength(20);

                entity.Property(x => x.Total)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                // Quan hệ với chi nhánh
                entity.HasOne<Tenant>()
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Quan hệ với nhân viên tạo đơn
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Chỉ mục theo chi nhánh và thời gian
                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.CreatedAt
                });
            });
            // =====================================
            // 6. BẢNG ORDER ITEMS
            // =====================================

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(x => x.OrderItemId);

                entity.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Quantity)
                    .IsRequired();

                
                // Quan hệ với đơn hàng
                entity.HasOne(x => x.Order)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với sản phẩm
                entity.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Chỉ mục tra cứu
                entity.HasIndex(x => x.OrderId);

                entity.HasIndex(x => x.ProductId);
            });
        }
    }
}
