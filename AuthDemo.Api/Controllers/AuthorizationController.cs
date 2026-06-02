using System.Collections.Immutable;
using System.Security.Claims;
using AuthDemo.Api.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthDemo.Api.Controllers;

// Controller xử lý toàn bộ luồng OAuth 2.0:
//   POST /connect/token  — đăng nhập (password grant) hoặc refresh token
//   POST /connect/logout — đăng xuất, revoke refresh token
//   GET  /connect/userinfo — trả thông tin user hiện tại
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictTokenManager _tokenManager;  // Quản lý token trong DB
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictTokenManager tokenManager,
        ILogger<AuthorizationController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    // POST /connect/token
    // Điểm vào chung cho mọi grant type.
    // EnableTokenEndpointPassthrough() (cấu hình trong OpenIddictExtensions) cho phép
    // controller này xử lý thay vì OpenIddict tự xử lý hoàn toàn.
    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        _logger.LogInformation("Nhận yêu cầu token. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
        // Lấy request OpenIddict đã parse từ form body (grant_type, username, password...)
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request không tìm thấy.");

        if (request.IsPasswordGrantType())
            return await HandlePasswordGrantAsync(request);

        if (request.IsRefreshTokenGrantType())
            return await HandleRefreshTokenGrantAsync(request);

        // Trả lỗi chuẩn OAuth 2.0 nếu grant_type không được hỗ trợ
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "Grant type không được hỗ trợ."
        });
    }

    // Xử lý đăng nhập bằng username + password (Resource Owner Password Credentials)
    private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request)
    {
        // Cho phép đăng nhập bằng cả username lẫn email
        var user = await _userManager.FindByNameAsync(request.Username!)
                ?? await _userManager.FindByEmailAsync(request.Username!);

        if (user == null)
        {
            _logger.LogWarning("Đăng nhập thất bại: username không tồn tại. IP: {IP}",
                HttpContext.Connection.RemoteIpAddress);

            // Forbid với OpenIddict scheme để trả về lỗi OAuth 2.0 đúng format
            // (không trả về 403 HTTP thông thường)
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Username hoặc mật khẩu không đúng."
                }));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            _logger.LogWarning("Tài khoản bị khóa: {UserId}. Hết hạn: {LockoutEnd}",
                user.Id, lockoutEnd);

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        $"Tài khoản bị khóa tạm thời đến {lockoutEnd:HH:mm dd/MM/yyyy}."
                }));
        }

        // lockoutOnFailure: true → Identity tự tăng AccessFailedCount và khóa khi đủ số lần
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password!, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Mật khẩu sai cho user: {UserId}. IP: {IP}",
                user.Id, HttpContext.Connection.RemoteIpAddress);

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Username hoặc mật khẩu không đúng."
                }));
        }

        // Đăng nhập thành công → reset counter đếm số lần sai
        await _userManager.ResetAccessFailedCountAsync(user);

        var identity = await BuildClaimsIdentityAsync(user);
        var principal = new ClaimsPrincipal(identity);

        // Thêm offline_access scope để OpenIddict luôn phát refresh_token trong ROPC flow.
        // RefreshTokenCookieMiddleware sẽ bắt refresh_token từ JSON response và chuyển vào cookie.
        principal.SetScopes(request.GetScopes().Append(Scopes.OfflineAccess));

        _logger.LogInformation("Đăng nhập thành công: {UserId} ({Email}). IP: {IP}",
            user.Id, user.Email, HttpContext.Connection.RemoteIpAddress);

        // SignIn với OpenIddict scheme → OpenIddict tạo access_token + refresh_token và trả về JSON
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // Xử lý cấp lại access_token bằng refresh_token (không cần nhập lại mật khẩu)
    private async Task<IActionResult> HandleRefreshTokenGrantAsync(OpenIddictRequest request)
    {
        // OpenIddict tự validate refresh_token (chữ ký, hạn sử dụng, đã bị revoke chưa)
        // Kết quả trả về principal chứa claims từ lần đăng nhập gốc
        var result = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Refresh token không hợp lệ hoặc đã hết hạn."
                }));
        }

        // Lấy userId từ claim Subject trong refresh token đã validate
        var userId = result.Principal?.GetClaim(Claims.Subject);
        var user = await _userManager.FindByIdAsync(userId!);

        // Kiểm tra lại trạng thái user — có thể bị xóa hoặc khóa sau khi token được cấp
        if (user == null || await _userManager.IsLockedOutAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "User không còn tồn tại hoặc tài khoản bị khóa."
                }));
        }

        // Build lại claims mới (role có thể đã thay đổi kể từ lần đăng nhập gốc)
        var identity = await BuildClaimsIdentityAsync(user);
        var principal = new ClaimsPrincipal(identity);

        // Giữ nguyên scope từ refresh token gốc (không mở rộng quyền khi refresh)
        principal.SetScopes(result.Principal!.GetScopes());

        _logger.LogInformation("Refresh token thành công cho user: {UserId}", user.Id);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // Tạo ClaimsIdentity chứa thông tin user sẽ được nhúng vào JWT
    private async Task<ClaimsIdentity> BuildClaimsIdentityAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,   // Claim nào đại diện cho "tên" (dùng User.Identity.Name)
            roleType: Claims.Role);  // Claim nào đại diện cho "role"

        // Subject (sub): định danh duy nhất của user trong token — thường là UserId
        identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));
        identity.SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user));
        identity.SetClaim(Claims.Email, await _userManager.GetEmailAsync(user));

        if (user.FullName != null)
            identity.SetClaim("full_name", user.FullName);

        // Thêm tất cả role của user vào token dưới dạng nhiều claim "role"
        var roles = await _userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, roles.ToImmutableArray());

        // Quyết định claim nào được nhúng vào access_token và/hoặc identity_token
        foreach (var claim in identity.Claims)
            claim.SetDestinations(GetDestinations(claim, identity));

        return identity;
    }

    // Xác định claim được ghi vào token nào:
    //   AccessToken: claim nằm trong JWT → API đọc được
    //   IdentityToken: claim nằm trong OIDC id_token → frontend đọc được
    //
    // Quy tắc: claim chỉ vào IdentityToken khi client yêu cầu scope tương ứng
    // (tránh lộ email/role khi client không xin scope đó)
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;
                if (identity.HasScope(Scopes.Profile)) // Client yêu cầu scope "profile"?
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (identity.HasScope(Scopes.Email))   // Client yêu cầu scope "email"?
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (identity.HasScope(Scopes.Roles))   // Client yêu cầu scope "roles"?
                    yield return Destinations.IdentityToken;
                yield break;

            default:
                // Mọi claim khác (full_name...) chỉ vào access_token
                yield return Destinations.AccessToken;
                yield break;
        }
    }

    // POST /connect/logout
    // Phải chỉ định scheme rõ ràng — AddIdentity đặt default scheme là cookie,
    // nếu không chỉ định Bearer scheme thì [Authorize] sẽ redirect về login page thay vì 401
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("Nhận yêu cầu logout. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
        var userId = User.GetClaim(Claims.Subject);

        // Revoke tất cả refresh token đang active của user trong DB.
        // Access token (JWT stateless) không lưu trong DB nên không revoke được —
        // chúng sẽ tự hết hạn sau 15 phút (SetAccessTokenLifetime).
        if (!string.IsNullOrEmpty(userId))
        {
            await foreach (var token in _tokenManager.FindBySubjectAsync(userId))
                await _tokenManager.TryRevokeAsync(token);
        }

        _logger.LogInformation("User đăng xuất: {UserId}. IP: {IP}",
            userId, HttpContext.Connection.RemoteIpAddress);

        return Ok(new { message = "Đăng xuất thành công." });
    }

    // GET/POST /connect/userinfo
    // Trả thông tin user từ token đang gửi — dùng thay cho decode JWT ở frontend
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        _logger.LogInformation("Nhận yêu cầu userinfo. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
        var userId = User.GetClaim(Claims.Subject);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            sub = user.Id.ToString(),
            name = user.UserName,
            email = user.Email,
            full_name = user.FullName,
            roles,
            email_verified = user.EmailConfirmed
        });
    }
}
