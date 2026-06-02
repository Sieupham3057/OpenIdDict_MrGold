using AuthDemo.Api.Data;
using AuthDemo.Api.Models;
using AuthDemo.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthDemo.Api.Services;

// IHostedService chạy một lần khi app khởi động (StartAsync).
// Nhiệm vụ:
//   1. Đăng ký OpenIddict Application (client "angular-spa") vào DB
//   2. Seed dữ liệu ban đầu: Roles, Functions, Actions, Permissions, Admin user
public class WorkerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(IServiceProvider serviceProvider, ILogger<WorkerService> logger)
    {
        // Inject IServiceProvider (root) vì WorkerService là Singleton —
        // không thể inject Scoped service (DbContext, UserManager) trực tiếp
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var manager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();

        // Mô tả OpenIddict Application — tương đương "client" trong OAuth 2.0
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "angular-spa",
            DisplayName = "Angular SPA Client",
            // Public client: không có client_secret (SPA không thể giữ bí mật)
            ClientType = ClientTypes.Public,
            // Chỉ cấp những permission đúng với flow đã khai báo trong OpenIddictExtensions
            Permissions =
            {
                Permissions.Endpoints.Token,          // Được dùng /connect/token
                Permissions.GrantTypes.Password,      // Được dùng password grant
                Permissions.GrantTypes.RefreshToken,  // Được dùng refresh_token grant
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
                Permissions.Scopes.Roles,
            }
        };

        // Upsert: tạo mới nếu chưa có, cập nhật nếu đã tồn tại
        // → an toàn để chạy nhiều lần (idempotent)
        var existingClient = await manager.FindByClientIdAsync("angular-spa", cancellationToken);
        if (existingClient == null)
        {
            await manager.CreateAsync(descriptor, cancellationToken);
            _logger.LogInformation("Đã tạo OpenIddict Application: angular-spa");
        }
        else
        {
            await manager.UpdateAsync(existingClient, descriptor, cancellationToken);
            _logger.LogInformation("Đã cập nhật OpenIddict Application: angular-spa");
        }

        await SeedDataAsync(scope.ServiceProvider);
    }

    private async Task SeedDataAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedFunctionsAndActionsAsync(dbContext);
        await SeedAdminUserAsync(userManager, roleManager, dbContext);
    }

    // ─── 1. Roles ─────────────────────────────────────────────────────────────────

    private async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = { "Admin", "Manager", "User", "Viewer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
                _logger.LogInformation("Đã tạo role: {Role}", role);
            }
        }
    }

    // ─── 2. Functions + Actions + ActionInFunctions ───────────────────────────────

    private async Task SeedFunctionsAndActionsAsync(ApplicationDbContext dbContext)
    {
        // Actions: các thao tác CRUD chuẩn dùng chung cho mọi Function
        var actions = new[]
        {
            new AppAction { Id = "VIEW",   Name = "Xem",   SortOrder = 1, IsActive = true },
            new AppAction { Id = "CREATE", Name = "Thêm",  SortOrder = 2, IsActive = true },
            new AppAction { Id = "EDIT",   Name = "Sửa",   SortOrder = 3, IsActive = true },
            new AppAction { Id = "DELETE", Name = "Xóa",   SortOrder = 4, IsActive = true },
            new AppAction { Id = "EXPORT", Name = "Xuất",  SortOrder = 5, IsActive = true },
            new AppAction { Id = "IMPORT", Name = "Nhập",  SortOrder = 6, IsActive = true },
        };

        foreach (var action in actions)
        {
            if (!await dbContext.Actions.AnyAsync(a => a.Id == action.Id))
            {
                dbContext.Actions.Add(action);
                _logger.LogInformation("Đã tạo action: {ActionId}", action.Id);
            }
        }

        // Functions: các module/màn hình của hệ thống
        var functions = new[]
        {
            new AppFunction { Id = "DASHBOARD", Name = "Dashboard",         Url = "/dashboard",       SortOrder = 1,  IsActive = true },
            new AppFunction { Id = "USER",      Name = "Quản lý User",      Url = "/admin/users",     SortOrder = 10, IsActive = true },
            new AppFunction { Id = "ROLE",      Name = "Quản lý Role",      Url = "/admin/roles",     SortOrder = 11, IsActive = true },
            new AppFunction { Id = "FUNCTION",  Name = "Quản lý Chức năng", Url = "/admin/functions", SortOrder = 12, IsActive = true },
            new AppFunction { Id = "PRODUCT",   Name = "Quản lý Sản phẩm", Url = "/products",        SortOrder = 20, IsActive = true },
            new AppFunction { Id = "ORDER",     Name = "Quản lý Đơn hàng", Url = "/orders",          SortOrder = 21, IsActive = true },
            new AppFunction { Id = "REPORT",    Name = "Báo cáo",           Url = "/reports",         SortOrder = 30, IsActive = true },
        };

        foreach (var function in functions)
        {
            if (!await dbContext.Functions.AnyAsync(f => f.Id == function.Id))
            {
                dbContext.Functions.Add(function);
                _logger.LogInformation("Đã tạo function: {FunctionId}", function.Id);
            }
        }

        await dbContext.SaveChangesAsync();

        // ActionInFunctions: quy định Action nào được phép có trong Function nào
        // (không phải mọi Function đều cần mọi Action — ví dụ Order không có DELETE)
        var actionInFunctions = new[]
        {
            // Dashboard: chỉ xem
            ("VIEW",   "DASHBOARD"),

            // User management: full CRUD + export
            ("VIEW",   "USER"), ("CREATE", "USER"), ("EDIT", "USER"), ("DELETE", "USER"), ("EXPORT", "USER"),

            // Role management: full CRUD
            ("VIEW",   "ROLE"), ("CREATE", "ROLE"), ("EDIT", "ROLE"), ("DELETE", "ROLE"),

            // Function management: full CRUD
            ("VIEW",   "FUNCTION"), ("CREATE", "FUNCTION"), ("EDIT", "FUNCTION"), ("DELETE", "FUNCTION"),

            // Product: full CRUD + export/import
            ("VIEW",   "PRODUCT"), ("CREATE", "PRODUCT"), ("EDIT", "PRODUCT"), ("DELETE", "PRODUCT"),
            ("EXPORT", "PRODUCT"), ("IMPORT", "PRODUCT"),

            // Order: xem, sửa (không xóa đơn hàng), export
            ("VIEW",   "ORDER"), ("EDIT", "ORDER"), ("EXPORT", "ORDER"),

            // Report: chỉ xem + export
            ("VIEW",   "REPORT"), ("EXPORT", "REPORT"),
        };

        foreach (var (actionId, functionId) in actionInFunctions)
        {
            if (!await dbContext.ActionInFunctions.AnyAsync(
                    aif => aif.ActionId == actionId && aif.FunctionId == functionId))
            {
                dbContext.ActionInFunctions.Add(new ActionInFunction
                {
                    ActionId = actionId,
                    FunctionId = functionId
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    // ─── 3. Admin user + full permissions ─────────────────────────────────────────

    private async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ApplicationDbContext dbContext)
    {
        const string adminEmail = "admin@demo.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                // Admin role bypass toàn bộ permission check (xem PermissionAuthorizationHandler)
                // → không cần seed permission chi tiết cho Admin
                _logger.LogWarning(
                    "Đã tạo user admin mặc định: {Email} — HÃY ĐỔI MẬT KHẨU TRƯỚC KHI PRODUCTION!",
                    adminEmail);
            }
            else
            {
                _logger.LogError("Không thể tạo admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        // Seed Manager user — dùng để demo và test phân quyền hạn chế
        const string managerEmail = "manager@demo.local";
        if (await userManager.FindByEmailAsync(managerEmail) == null)
        {
            var managerUser = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                FullName = "Demo Manager",
                FirstName = "Demo",
                LastName = "Manager",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(managerUser, "Manager@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");

                // Seed quyền cho Manager: xem/sửa/xuất Product + Order + xem Dashboard/Report
                // Manager KHÔNG có quyền quản lý User/Role/Function
                var managerRole = await roleManager.FindByNameAsync("Manager");
                if (managerRole != null)
                {
                    var managerPermissions = new[]
                    {
                        new Permission { RoleId = managerRole.Id, FunctionId = "DASHBOARD", ActionId = "VIEW"   },
                        new Permission { RoleId = managerRole.Id, FunctionId = "PRODUCT",   ActionId = "VIEW"   },
                        new Permission { RoleId = managerRole.Id, FunctionId = "PRODUCT",   ActionId = "EDIT"   },
                        new Permission { RoleId = managerRole.Id, FunctionId = "PRODUCT",   ActionId = "EXPORT" },
                        new Permission { RoleId = managerRole.Id, FunctionId = "ORDER",     ActionId = "VIEW"   },
                        new Permission { RoleId = managerRole.Id, FunctionId = "ORDER",     ActionId = "EDIT"   },
                        new Permission { RoleId = managerRole.Id, FunctionId = "ORDER",     ActionId = "EXPORT" },
                        new Permission { RoleId = managerRole.Id, FunctionId = "REPORT",    ActionId = "VIEW"   },
                        new Permission { RoleId = managerRole.Id, FunctionId = "REPORT",    ActionId = "EXPORT" },
                    };

                    foreach (var perm in managerPermissions)
                    {
                        if (!await dbContext.Permissions.AnyAsync(p =>
                                p.RoleId == perm.RoleId &&
                                p.FunctionId == perm.FunctionId &&
                                p.ActionId == perm.ActionId))
                        {
                            dbContext.Permissions.Add(perm);
                        }
                    }

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Đã seed permissions cho role Manager");
                }

                _logger.LogWarning(
                    "Đã tạo user manager demo: {Email} — Chỉ dùng để test phân quyền!",
                    managerEmail);
            }
        }
    }

    // StopAsync gọi khi app shutdown — không cần dọn dẹp gì
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
