namespace AuthDemo.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AngularPolicy";

    public static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedOrigins is not configured.");

        var headers = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedHeaders is not configured.");

        var methods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedMethods is not configured.");

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .WithHeaders(headers)
                    .WithMethods(methods)
                    .AllowCredentials(); // Bắt buộc cho HttpOnly cookie (refresh_token)
            });
        });

        return services;
    }
}
