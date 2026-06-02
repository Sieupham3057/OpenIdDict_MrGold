using AuthDemo.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AuthDemo.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationConfig(this IServiceCollection services)
    {
        // Thay thế IAuthorizationPolicyProvider mặc định bằng PermissionPolicyProvider
        // để hỗ trợ policy động có dạng "Permission:FUNCTION:ACTION"
        // Singleton vì không có state thay đổi theo request
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Handler kiểm tra xem user (qua role) có quyền thực hiện action trên function không
        // Singleton vì dùng IServiceScopeFactory để tự tạo scope khi cần DbContext
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
