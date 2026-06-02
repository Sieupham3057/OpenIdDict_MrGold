namespace AuthDemo.Api.Models.Entities;

// Bảng trung gian: Action nào được phép có trong Function nào
// Composite PK: (ActionId, FunctionId) — cấu hình trong DbContext
public class ActionInFunction
{
    public string ActionId { get; set; } = string.Empty;
    public string FunctionId { get; set; } = string.Empty;

    public virtual AppAction Action { get; set; } = null!;
    public virtual AppFunction Function { get; set; } = null!;
}
