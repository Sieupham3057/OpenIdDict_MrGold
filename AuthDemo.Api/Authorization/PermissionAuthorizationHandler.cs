using AuthDemo.Api.Data;
using AuthDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthDemo.Api.Authorization;

// Kiểm tra xem user (thông qua các role) có quyền thực hiện action trên function không
//
// Flow:
//   1. Lấy userId từ claim Subject
//   2. Lấy danh sách role của user
//   3. Admin → bypass toàn bộ permission check
//   4. Kiểm tra bảng Permissions: có record (RoleId, FunctionId, ActionId) không?
//
// Cải tiến production: thêm IMemoryCache để cache permissions per user, invalidate khi thay đổi
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory)
    {
        // Dùng IServiceScopeFactory thay vì inject DbContext trực tiếp
        // vì Handler được đăng ký là Singleton, DbContext là Scoped
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId)) return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return;

        var roles = await userManager.GetRolesAsync(user);

        // Admin không cần kiểm tra permission chi tiết
        if (roles.Contains("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        // Tìm RoleId của các role user đang có
        var roleIds = await dbContext.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        var hasPermission = await dbContext.Permissions
            .AnyAsync(p =>
                roleIds.Contains(p.RoleId) &&
                p.FunctionId == requirement.FunctionId &&
                p.ActionId == requirement.ActionId);

        if (hasPermission)
            context.Succeed(requirement);
    }
}
