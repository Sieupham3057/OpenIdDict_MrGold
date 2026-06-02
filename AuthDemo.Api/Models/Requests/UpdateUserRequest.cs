using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

// Request body cho PUT /api/users/{id} — partial update.
// Mọi trường đều nullable: null = không thay đổi, có giá trị = cập nhật.
// Đổi email dùng ChangeEmailAsync (cần token nội bộ), không gán trực tiếp.
// Không có Password ở đây — dùng POST /api/users/{id}/change-password riêng.
public class UpdateUserRequest
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool? IsActive { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}
