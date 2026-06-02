using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;

namespace AuthDemo.Api.Authorization;

// Tự động tạo AuthorizationPolicy từ tên policy có format: "Permission:FUNCTION_ID:ACTION_ID"
// Ví dụ: "Permission:USER:VIEW" → require PermissionRequirement("USER", "VIEW")
//
// Lý do dùng IAuthorizationPolicyProvider thay vì đăng ký trước:
//   - Không cần biết trước toàn bộ Function+Action khi khởi động
//   - [HasPermission("USER", "VIEW")] tự động được giải quyết
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            return _fallback.GetPolicyAsync(policyName);

        var parts = policyName[PolicyPrefix.Length..].Split(':');
        if (parts.Length != 2)
            return _fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.AddRequirements(new PermissionRequirement(parts[0], parts[1]));

        return Task.FromResult<AuthorizationPolicy?>(policy.Build());
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}
