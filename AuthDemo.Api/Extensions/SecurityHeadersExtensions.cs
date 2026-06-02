namespace AuthDemo.Api.Extensions;

public static class SecurityHeadersExtensions
{
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Ngăn trình duyệt tự đoán kiểu file (MIME sniffing)
            // Tránh tấn công: upload file .jpg chứa script, trình duyệt chạy như JS
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // Ngăn trang web bị nhúng vào iframe trên domain khác
            // Chống tấn công Clickjacking (lừa user click vào iframe ẩn)
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            if (app.Environment.IsDevelopment())
            {
                // CSP lỏng hơn ở dev để Swagger UI và hot-reload Angular hoạt động:
                // - unsafe-inline, unsafe-eval: cho phép inline script (Swagger cần)
                // - blob: worker-src: cho phép Web Worker từ blob URL (Angular dev)
                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' blob:; " +
                    "style-src 'self' 'unsafe-inline'; img-src 'self' data:; worker-src blob:;");
            }
            else
            {
                // CSP chặt hơn ở production — không cho phép inline script
                // Giúp ngăn tấn công XSS (Cross-Site Scripting)
                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:");

                // Bắt buộc HTTPS trong 1 năm (31536000 giây), áp dụng cho cả subdomain
                // Trình duyệt sẽ tự redirect HTTP → HTTPS mà không cần hỏi server
                context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            await next();
        });
        return app;
    }
}
