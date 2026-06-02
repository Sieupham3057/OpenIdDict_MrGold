namespace AuthDemo.Api.Models.Entities;

// Function = module/chức năng trong hệ thống (ví dụ: USER, ROLE, PRODUCT, ORDER...)
// Id là string (ví dụ: "USER") thay vì int để dễ đọc trong code và DB
// Cây menu có thể lồng nhau thông qua ParentId (ví dụ: REPORT nằm dưới ADMIN)
public class AppFunction
{
    public string Id { get; set; } = string.Empty;       // Ví dụ: "USER", "PRODUCT"
    public string Name { get; set; } = string.Empty;     // Tên hiển thị: "Quản lý User"
    public string? Url { get; set; }                     // Đường dẫn frontend: "/admin/users"
    public string? ParentId { get; set; }                // Null nếu là menu gốc
    public int SortOrder { get; set; }                   // Thứ tự hiển thị trong menu
    public string? Icon { get; set; }                    // Icon class (Material Icons, FontAwesome...)
    public bool IsActive { get; set; } = true;           // Ẩn khỏi hệ thống mà không xóa

    // Navigation properties — EF Core dùng để JOIN khi Include()
    public virtual ICollection<Permission> Permissions { get; set; } = [];
    public virtual ICollection<ActionInFunction> ActionInFunctions { get; set; } = [];
}
