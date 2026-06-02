namespace AuthDemo.Api.Models.Dtos;

// DTO trả về thông tin Function kèm danh sách Action hợp lệ của Function đó.
// Dùng khi cần hiển thị menu hoặc danh sách chức năng có thể phân quyền.
public class FunctionDto
{
    public string Id { get; set; } = string.Empty;       // Ví dụ: "USER", "PRODUCT"
    public string Name { get; set; } = string.Empty;     // Tên hiển thị
    public string? Url { get; set; }                     // Đường dẫn frontend
    public string? ParentId { get; set; }                // Null = menu gốc
    public int SortOrder { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public List<ActionDto> Actions { get; set; } = [];   // Chỉ chứa actions hợp lệ của function này
}

// Action tóm tắt — chỉ cần Id và Name để hiển thị trong UI
public class ActionDto
{
    public string Id { get; set; } = string.Empty;   // Ví dụ: "VIEW", "CREATE"
    public string Name { get; set; } = string.Empty; // Ví dụ: "Xem", "Thêm"
}
