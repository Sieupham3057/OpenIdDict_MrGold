using AuthDemo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Dùng PostgreSQL (Npgsql). Để chuyển sang SQL Server thì bỏ comment dòng dưới
            // và xóa dòng UseNpgsql (connection string trong appsettings.json giữ nguyên key)
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            //options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            // Bắt buộc phải gọi UseOpenIddict() để EF Core biết có các bảng
            // OpenIddict (OpenIddictTokens, OpenIddictApplications...) cần quản lý
            options.UseOpenIddict();

            if (environment.IsDevelopment())
            {
                // In ra lỗi chi tiết hơn khi query bị lỗi (chậm hơn một chút)
                options.EnableDetailedErrors();
                // In giá trị tham số SQL vào log — KHÔNG dùng production vì lộ dữ liệu nhạy cảm
                options.EnableSensitiveDataLogging();
            }
        });
        return services;
    }
}
