using AuthDemo.Api.Data;
using AuthDemo.Api.Extensions;
using AuthDemo.Api.Middleware;
using AuthDemo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    // Đọc cấu hình từ appsettings.json
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();


try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // builder.Configuration đã tự động load appsettings.json
    // Chỉ cần gọi UseSerilog() để nó dùng cấu hình từ builder.Configuration
    builder.Host.UseSerilog();

    // ═══════════════════════════════════════════════════════════════════════════════
    // ĐĂNG KÝ SERVICES (Dependency Injection Container)
    // ═══════════════════════════════════════════════════════════════════════════════

    builder.Services.AddDatabase(builder.Configuration, builder.Environment);
    builder.Services.AddIdentityConfig();
    builder.Services.AddOpenIddictConfig(builder.Environment, builder.Configuration);
    builder.Services.AddCorsConfig(builder.Configuration);
    builder.Services.AddAuthorizationConfig();
    builder.Services.AddSwaggerConfig();
    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();
    builder.Services.AddHostedService<WorkerService>();

    var app = builder.Build();

    // ═══════════════════════════════════════════════════════════════════════════════
    // CẤU HÌNH MIDDLEWARE PIPELINE
    // ═══════════════════════════════════════════════════════════════════════════════

    // if (app.Environment.IsDevelopment())
        app.UseSwaggerConfig();

    if (!app.Environment.IsProduction())
        app.UseHttpsRedirection();
    app.UseCors(CorsExtensions.PolicyName);
    app.UseMiddleware<RefreshTokenCookieMiddleware>();
    app.UseSecurityHeaders();
    app.UseRouting();

    // Ghi log HTTP request/response (method, path, status code, thời gian xử lý)
    app.UseSerilogRequestLogging();

    // Track HTTP metrics: request count, duration, in-flight requests theo route
    app.UseHttpMetrics();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // GET /metrics — Prometheus scrape endpoint (không yêu cầu auth)
    app.MapMetrics();

    // GET /health — k6 setup() kiểm tra API online trước khi test
    app.MapHealthChecks("/health");

    // ═══════════════════════════════════════════════════════════════════════════════
    // MIGRATE DATABASE KHI KHỞI ĐỘNG
    // ═══════════════════════════════════════════════════════════════════════════════
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AuthDemo.Api terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
