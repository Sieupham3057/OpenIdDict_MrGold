namespace AuthDemo.Api.Models.Requests;

// Class này chỉ dùng tham chiếu trong code — login thực tế không qua đây.
// OpenIddict nhận request dạng application/x-www-form-urlencoded (OAuth 2.0 chuẩn),
// không phải JSON body, nên controller đọc trực tiếp qua HttpContext.GetOpenIddictServerRequest().
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
