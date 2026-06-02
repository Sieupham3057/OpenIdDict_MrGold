namespace AuthDemo.Api.Models.Dtos;

// DTO đơn giản cho danh sách role
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// DTO cho GET /api/roles/{id}/permissions — trả về permission matrix của role:
//   Role
//   └── Function (USER, PRODUCT...)
//       └── Action (VIEW, CREATE...) + HasPermission (true/false)
// Frontend dùng để render bảng checkbox phân quyền
public class RolePermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FunctionPermissionDto> Permissions { get; set; } = [];
}

public class FunctionPermissionDto
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public List<ActionPermissionDto> Actions { get; set; } = [];
}

// Mỗi ô trong permission matrix: action + trạng thái checked
public class ActionPermissionDto
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public bool HasPermission { get; set; } // true = role có quyền này, false = chưa được cấp
}
