using AuthDemo.Api.Data;

namespace AuthDemo.Api.Extensions;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddOpenIddictConfig(this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        services.AddOpenIddict()
            // === TẦNG 1: CORE ===
            // Kết nối OpenIddict với Entity Framework Core để lưu tokens,
            // applications, authorizations vào database (bảng OpenIddictTokens, v.v.)
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })

            // === TẦNG 2: SERVER — cấu hình Authorization Server ===
            // Đây là nơi server phát hành token (Authorization Server)
            .AddServer(options =>
            {
                // Khai báo endpoint nhận yêu cầu đăng nhập / lấy token
                // Client sẽ POST tới /connect/token với username + password
                options.SetTokenEndpointUris("/connect/token");

                // === GRANT TYPES — bắt buộc phải khai báo tường minh ===
                // OpenIddict mặc định KHÔNG cho phép bất kỳ flow nào.
                // Nếu thiếu dòng này → client gọi /connect/token sẽ bị lỗi
                // "unsupported_grant_type" dù endpoint đúng.

                // Password Flow (Resource Owner Password Credentials):
                // Client gửi username + password trực tiếp lên server lấy access token.
                // Dùng cho ứng dụng trusted (SPA, mobile app của chính mình).
                options.AllowPasswordFlow();

                // Refresh Token Flow:
                // Khi access token hết hạn (15 phút), client dùng refresh token
                // để lấy access token mới mà không cần user nhập lại mật khẩu.
                // Nếu thiếu dòng này → refresh token không được cấp, user phải
                // đăng nhập lại mỗi 15 phút.
                options.AllowRefreshTokenFlow();

                // Khai báo các scope hợp lệ mà client được phép yêu cầu.
                // "profile" → thông tin cơ bản (tên, avatar)
                // "email"   → địa chỉ email
                // "roles"   → danh sách vai trò (Admin, User, v.v.)
                // Nếu client yêu cầu scope không nằm trong danh sách này → bị từ chối
                options.RegisterScopes("profile", "email", "roles");

                // Access token tồn tại 15 phút — ngắn để giảm rủi ro nếu bị lộ
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));

                // Refresh token tồn tại 7 ngày — dùng để đổi lấy access token mới
                // (xem AllowRefreshTokenFlow ở trên)
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

                // === SIGNING & ENCRYPTION KEYS ===
                // Server cần 2 loại key:
                // - Encryption key: mã hóa nội dung token (ai đọc được token?)
                // - Signing key:    ký token để xác minh token không bị giả mạo
                if (environment.IsDevelopment())
                {
                    // Ephemeral = key tạm thời, sinh mỗi lần restart → KHÔNG dùng production
                    // vì token cũ sẽ không validate được sau khi server restart
                    options.AddEphemeralEncryptionKey()
                           .AddEphemeralSigningKey();
                }
                else
                {
                    // Production: dùng persistent certificate (lưu trong KeyVault, file .pfx...)
                    // để token vẫn hợp lệ sau khi restart hoặc deploy nhiều instance
                    // options.AddEncryptionCertificate(...)
                    //        .AddSigningCertificate(...);
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }

                // Tắt mã hóa nội dung access token — token sẽ ở dạng JWT thuần (base64)
                // Mặc định OpenIddict mã hóa token → client không đọc được payload
                // Tắt đi để Angular/Postman có thể decode JWT xem claims (tiện debug)
                options.DisableAccessTokenEncryption();

                // === TÍCH HỢP VỚI ASP.NET CORE ===
                options.UseAspNetCore()
                       // Cho phép controller của mình xử lý /connect/token thay vì
                       // OpenIddict tự xử lý hoàn toàn → ta có thể validate thêm logic
                       .EnableTokenEndpointPassthrough()
                       // Cho phép chạy qua HTTP (không bắt buộc HTTPS) — CHỈ DEV
                       // Ở production PHẢI xóa dòng này để bắt buộc HTTPS
                       .DisableTransportSecurityRequirement();
            })

            // === TẦNG 3: VALIDATION — cấu hình Resource Server ===
            // Khi API nhận request kèm Bearer token, tầng này kiểm tra token có hợp lệ không
            // UseLocalServer: lấy signing key từ chính server này (không cần gọi ra ngoài)
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
