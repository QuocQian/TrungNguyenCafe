
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrungNguyenCafe.API.Services;
using TrungNguyenCafe.Entities.Users;
using TrungNguyenCafe.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Kiểm tra chuỗi kết nối Database
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Không tìm thấy chuỗi kết nối DefaultConnection.");
}

// 2. Đọc cấu hình JWT từ User Secrets
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    
    throw new InvalidOperationException(
        "Cấu hình JWT không hợp lệ. Kiểm tra lại User Secrets.");
}

// 3. Đăng ký Database
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseSqlServer(connectionString));

// 4. Đăng ký công cụ mã hóa mật khẩu
builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>
>();

// 5. Đăng ký JWT Authentication
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,

            ClockSkew = TimeSpan.FromSeconds(30)
        };
});

// 6. Đăng ký Authorization
builder.Services.AddAuthorization();

// 7. Đăng ký JWT Token Service
builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService
>();

// 7. Đăng ký Controllers và Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Nhập JWT Access Token"
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

// 8. Khởi tạo tài khoản Admin toàn hệ thống
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<CafeDbContext>();

    var passwordHasher =
        services.GetRequiredService<IPasswordHasher<User>>();

    var adminUserName =
        builder.Configuration["InitialAdmin:UserName"];

    var adminPassword =
        builder.Configuration["InitialAdmin:Password"];

    if (string.IsNullOrWhiteSpace(adminUserName) ||
        string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Chưa cấu hình tài khoản Admin trong User Secrets.");
    }

    var adminUser = new User
    {
        UserName = adminUserName
    };

    var passwordHash =
        passwordHasher.HashPassword(adminUser, adminPassword);

    await DatabaseSeeder.SeedAsync(
        context,
        adminUserName,
        passwordHash
    );
}

// 9. Cấu hình Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 10. Cấu hình Middleware
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// 11. Ánh xạ Controllers
app.MapControllers();

// 12. Khởi chạy ứng dụng
app.Run();
