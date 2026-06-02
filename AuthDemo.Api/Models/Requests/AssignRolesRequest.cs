using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

// Request body cho PUT /api/users/{id}/roles.
// Thao tác là REPLACE (xóa hết role cũ, gán role mới) — không phải merge/append.
// Gửi list rỗng [] để thu hồi toàn bộ role của user.
public class AssignRolesRequest
{
    [Required]
    public List<string> Roles { get; set; } = [];
}
