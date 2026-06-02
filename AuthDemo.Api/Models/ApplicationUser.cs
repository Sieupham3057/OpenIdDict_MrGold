using Microsoft.AspNetCore.Identity;

namespace AuthDemo.Api.Models;

// Kế thừa IdentityUser<Guid> để dùng Guid làm khóa chính thay vì string mặc định.
// IdentityUser đã có sẵn: Id, UserName, Email, PasswordHash, PhoneNumber,
// LockoutEnd, LockoutEnabled, AccessFailedCount, EmailConfirmed...
// Chỉ thêm các trường nghiệp vụ riêng của ứng dụng bên dưới.
public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }      // Họ tên đầy đủ (hiển thị)
    public string? FirstName { get; set; }     // Tên (dùng khi cần tách riêng)
    public string? LastName { get; set; }      // Họ
    public DateOnly? DateOfBirth { get; set; } // Ngày sinh — DateOnly (không có giờ)
    public bool IsActive { get; set; } = true; // Vô hiệu hóa user mà không xóa khỏi DB
    public string? AvatarUrl { get; set; }     // URL ảnh đại diện
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Luôn lưu UTC
    public DateTime? UpdatedAt { get; set; }
}
