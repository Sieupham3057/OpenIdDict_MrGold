using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models.Requests;

// Request body cho POST /api/users/{id}/change-password.
// Admin đặt lại mật khẩu mà không cần biết mật khẩu cũ —
// dùng GeneratePasswordResetToken + ResetPasswordAsync thay vì ChangePasswordAsync.
public class ChangePasswordRequest
{
    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
