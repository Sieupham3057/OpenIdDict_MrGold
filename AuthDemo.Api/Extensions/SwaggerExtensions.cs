using Microsoft.OpenApi.Models;

namespace AuthDemo.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthDemo API", Version = "v1" });

            // Thêm ô nhập Bearer token vào Swagger UI
            // Cách dùng: lấy access_token từ /connect/token → paste vào ô Authorize
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Chỉ cần paste access_token (không cần gõ 'Bearer')",
                Name = "Authorization",       // Header name
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",            // Swagger tự thêm "Bearer " prefix
                BearerFormat = "JWT",
            });

            // Bắt buộc Bearer token cho mọi endpoint trong Swagger UI
            // (chỉ ảnh hưởng giao diện Swagger, không ảnh hưởng logic thực tế)
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()  // Không yêu cầu scope cụ thể
                }
            });
        });
        return services;
    }

    public static WebApplication UseSwaggerConfig(this WebApplication app)
    {
        app.UseSwagger();      // Tạo file swagger.json
        app.UseSwaggerUI();    // Hiển thị giao diện web tại /swagger
        return app;
    }
}
