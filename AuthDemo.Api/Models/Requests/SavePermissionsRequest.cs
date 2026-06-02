using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

// Request body cho PUT /api/roles/{id}/permissions.
// Gửi toàn bộ danh sách permission CÒN LẠI (không phải delta/diff).
// Server sẽ xóa hết permission cũ và ghi lại danh sách mới —
// tức là permission nào không có trong list này sẽ bị thu hồi.
public class SavePermissionsRequest
{
    [Required]
    public List<PermissionEntry> Permissions { get; set; } = [];
}

// Một ô trong permission matrix: Function + Action
// Ví dụ: { FunctionId: "USER", ActionId: "VIEW" } → role được xem danh sách user
public class PermissionEntry
{
    [Required, MaxLength(50)]
    public string FunctionId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ActionId { get; set; } = string.Empty;
}
