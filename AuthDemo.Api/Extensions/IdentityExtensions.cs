using AuthDemo.Api.Data;
using AuthDemo.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthDemo.Api.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityConfig(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            // ─── Chính sách mật khẩu ─────────────────────────────────────────────
            options.Password.RequiredLength = 8;           // Tối thiểu 8 ký tự
            options.Password.RequireDigit = true;          // Phải có ít nhất 1 chữ số
            options.Password.RequireLowercase = true;      // Phải có chữ thường
            options.Password.RequireUppercase = true;      // Phải có chữ hoa
            options.Password.RequireNonAlphanumeric = false; // Không bắt buộc ký tự đặc biệt

            // ─── Lockout (khóa tài khoản khi đăng nhập sai nhiều lần) ────────────
            options.Lockout.AllowedForNewUsers = true;         // Áp dụng cho cả user mới tạo
            options.Lockout.MaxFailedAccessAttempts = 5;       // Sai 5 lần → khóa
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Khóa 15 phút

            // ─── Yêu cầu về User ─────────────────────────────────────────────────
            options.User.RequireUniqueEmail = true;         // Mỗi email chỉ được dùng 1 lần
            options.SignIn.RequireConfirmedEmail = false;   // Không bắt xác nhận email trước khi đăng nhập
                                                           // (đặt true ở production nếu có hệ thống gửi mail)
        })
        // Lưu Identity data (user, role, claims...) vào ApplicationDbContext (PostgreSQL)
        .AddEntityFrameworkStores<ApplicationDbContext>()
        // Thêm providers tạo token cho: reset password, xác nhận email, 2FA...
        // (Không liên quan đến OpenIddict token — đây là token nội bộ của Identity)
        .AddDefaultTokenProviders();

        return services;
    }
}
