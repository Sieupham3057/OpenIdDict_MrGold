using Microsoft.AspNetCore.Authorization;

namespace AuthDemo.Api.Authorization;

// Attribute dùng trên controller action để yêu cầu quyền cụ thể
//
// Ví dụ sử dụng:
//   [HasPermission("USER", "VIEW")]    → xem danh sách user
//   [HasPermission("USER", "CREATE")]  → tạo user mới
//   [HasPermission("USER", "EDIT")]    → sửa thông tin user
//   [HasPermission("USER", "DELETE")]  → xóa user
//
// Attribute này tự động tạo policy name: "Permission:USER:VIEW"
// PermissionPolicyProvider sẽ parse tên này và tạo AuthorizationPolicy tương ứng
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string functionId, string actionId)
        : base($"{PermissionPolicyProvider.PolicyPrefix}{functionId}:{actionId}")
    {
    }
}
