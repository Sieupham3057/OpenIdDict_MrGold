namespace AuthDemo.Api.Models.Entities;

// Phân quyền: Role X được thực hiện Action Y trên Function Z
// Composite PK: (RoleId, FunctionId, ActionId) — cấu hình trong DbContext
public class Permission
{
    public Guid RoleId { get; set; }
    public string FunctionId { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;

    public virtual AppFunction Function { get; set; } = null!;
    public virtual AppAction Action { get; set; } = null!;
}
