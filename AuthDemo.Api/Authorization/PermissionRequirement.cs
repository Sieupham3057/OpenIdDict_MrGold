using Microsoft.AspNetCore.Authorization;

namespace AuthDemo.Api.Authorization;

// Yêu cầu quyền cụ thể: Function + Action
// Được dùng bởi PermissionPolicyProvider để build policy động
public class PermissionRequirement : IAuthorizationRequirement
{
    public string FunctionId { get; }
    public string ActionId { get; }

    public PermissionRequirement(string functionId, string actionId)
    {
        FunctionId = functionId;
        ActionId = actionId;
    }
}
