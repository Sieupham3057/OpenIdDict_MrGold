using System.Text;
using System.Text.Json;

namespace AuthDemo.Api.Middleware;

/// <summary>
/// Hiện thực hoá chiến lược lưu Refresh Token trong HttpOnly Cookie:
///
/// - Sau khi login/refresh thành công: extract refresh_token từ JSON response,
///   set vào HttpOnly cookie, trả về JSON không có refresh_token.
///
/// - Khi nhận refresh_token grant: đọc cookie, inject refresh_token vào form body
///   trước khi OpenIddict validate (vì OpenIddict validate trước khi vào controller).
/// </summary>
public class RefreshTokenCookieMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsTokenEndpoint(context))
        {
            await next(context);
            return;
        }

        // Bước 1: Với refresh_token grant, inject cookie vào form body
        var modifiedBody = await InjectRefreshTokenFromCookie(context);
        var bodyBytes = Encoding.UTF8.GetBytes(modifiedBody);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;

        // Bước 2: Buffer response để post-process
        var originalResponseBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await next(context);

        // Bước 3: Với response thành công, move refresh_token vào HttpOnly cookie
        responseBuffer.Seek(0, SeekOrigin.Begin);
        var responseJson = await new StreamReader(responseBuffer).ReadToEndAsync();

        if (context.Response.StatusCode == 200)
            responseJson = MoveRefreshTokenToCookie(context, responseJson);

        context.Response.Body = originalResponseBody;
        var responseBytes = Encoding.UTF8.GetBytes(responseJson);
        context.Response.ContentLength = responseBytes.Length;
        await context.Response.Body.WriteAsync(responseBytes);
    }

    private static bool IsTokenEndpoint(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/connect/token") &&
        context.Request.Method == HttpMethods.Post;

    private static async Task<string> InjectRefreshTokenFromCookie(HttpContext context)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        var formValues = ParseFormBody(body);

        if (!formValues.TryGetValue("grant_type", out var grantType) ||
            grantType != "refresh_token")
            return body;

        // Nếu client đã gửi refresh_token trong body thì không cần override
        if (formValues.TryGetValue("refresh_token", out var existing) &&
            !string.IsNullOrEmpty(existing))
            return body;

        var cookieToken = context.Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(cookieToken))
            return body;

        var separator = body.Length > 0 && !body.EndsWith('&') ? "&" : "";
        return body + separator + "refresh_token=" + Uri.EscapeDataString(cookieToken);
    }

    private string MoveRefreshTokenToCookie(HttpContext context, string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (!doc.RootElement.TryGetProperty("refresh_token", out var rtElement))
                return responseJson;

            var refreshToken = rtElement.GetString();
            if (string.IsNullOrEmpty(refreshToken))
                return responseJson;

            context.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Path = "/connect/token",
                MaxAge = TimeSpan.FromDays(7),
            });

            // Xóa refresh_token khỏi JSON trả về client
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseJson)!;
            dict.Remove("refresh_token");
            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return responseJson;
        }
    }

    private static Dictionary<string, string> ParseFormBody(string body)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx > 0)
            {
                result[Uri.UnescapeDataString(pair[..idx])] =
                    Uri.UnescapeDataString(pair[(idx + 1)..]);
            }
        }
        return result;
    }
}
