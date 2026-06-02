namespace AuthDemo.Api.Models.Entities;

// Action = thao tác có thể thực hiện (VIEW, CREATE, EDIT, DELETE, EXPORT, IMPORT)
// Tên class AppAction để tránh conflict với System.Action / Microsoft.AspNetCore.Routing.ActionConstraints
// Bảng DB vẫn tên là "Actions"
public class AppAction
{
    public string Id { get; set; } = string.Empty;    // Ví dụ: "VIEW", "CREATE", "DELETE"
    public string Name { get; set; } = string.Empty;  // Tên hiển thị: "Xem", "Thêm", "Xóa"
    public int SortOrder { get; set; }                 // Thứ tự hiển thị trong permission matrix
    public bool IsActive { get; set; } = true;         // Ẩn action mà không xóa khỏi DB

    // Navigation properties: EF Core dùng để truy vấn ngược chiều
    public virtual ICollection<Permission> Permissions { get; set; } = [];
    public virtual ICollection<ActionInFunction> ActionInFunctions { get; set; } = [];
}
