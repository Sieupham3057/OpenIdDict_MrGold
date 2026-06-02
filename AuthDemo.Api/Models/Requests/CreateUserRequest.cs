using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

// Request body cho POST /api/users (tạo user mới bởi admin).
// Password được hash bởi UserManager, không bao giờ lưu plain text.
// Roles: gán ngay khi tạo (có thể rỗng, sửa sau bằng PUT /api/users/{id}/roles).
public class CreateUserRequest
{
    [Required, MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public List<string> Roles { get; set; } = [];
}
