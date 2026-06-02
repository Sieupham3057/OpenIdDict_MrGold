using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace AuthDemo.Api.Controllers;

[ApiController]
[Route("api")]  // Route gọn: /api/me, /api/products — không phải /api/api/me
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;

    public ApiController(ILogger<ApiController> logger)
    {
        _logger = logger;
    }

    // GET /api/me
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId   = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        var userName = User.FindFirstValue(ClaimTypes.Name)
                    ?? User.FindFirstValue(OpenIddictConstants.Claims.Name);
        var email    = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue(OpenIddictConstants.Claims.Email);
        var fullName = User.FindFirstValue("full_name");
        // OpenIddict dùng claim type "role" (OIDC), không phải ClaimTypes.Role (Microsoft schema)
        var roles    = User.FindAll(OpenIddictConstants.Claims.Role)
                          .Concat(User.FindAll(ClaimTypes.Role))
                          .Select(c => c.Value).Distinct().ToList();

        return Ok(new { userId, userName, email, fullName, roles });
    }

    // GET /api/admin-only — chỉ Admin
    [HttpGet("admin-only")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
               Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "Bạn đang ở khu vực Admin." });
    }

    // GET /api/public — không cần đăng nhập
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult PublicEndpoint()
    {
        return Ok(new { message = "Endpoint công khai, không cần đăng nhập." });
    }

}
