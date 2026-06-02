namespace AuthDemo.Api.Models.Dtos;

// DTO trả về cho client — không bao gồm PasswordHash và các trường nhạy cảm của IdentityUser.
// IsLockedOut được tính từ LockoutEnd (xem UsersController.MapToDto),
// không lấy trực tiếp từ entity vì entity không có property này.
public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLockedOut { get; set; }   // true nếu LockoutEnd > UtcNow
    public IList<string> Roles { get; set; } = [];
}
