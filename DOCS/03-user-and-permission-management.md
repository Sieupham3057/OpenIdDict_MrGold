# Quản lý User và Phân quyền (FRAC Pattern)

## Tổng quan bài toán

Sau khi có hệ thống xác thực (Login/Token), bước tiếp theo là kiểm soát **ai được làm gì** trong hệ thống. Đây là bài toán Authorization.

### Hai cấp độ Authorization

```
Authentication (Xác thực)  →  "Bạn là ai?" → Token hợp lệ
Authorization  (Phân quyền) →  "Bạn được làm gì?" → Permission check
```

---

## 1. Mô hình dữ liệu (FRAC)

### Sơ đồ quan hệ

```
AspNetRoles ──────────────────────┐
    (RoleId)                      │
                                  ▼
AppFunction ◄──── ActionInFunction ──── AppAction
    (Id)              (FK)              (Id)
     │                                   │
     └──────── Permission ───────────────┘
               (RoleId, FunctionId, ActionId)
                    ↑
              Composite PK
```

### Ý nghĩa từng bảng

| Bảng | Ý nghĩa | Ví dụ |
|------|---------|-------|
| `AppFunction` | Module/chức năng của hệ thống | "Quản lý User", "Sản phẩm", "Báo cáo" |
| `AppAction` | Thao tác chuẩn | VIEW, CREATE, EDIT, DELETE, EXPORT, IMPORT |
| `ActionInFunction` | Action nào có trong Function nào | Function "ORDER" không có DELETE |
| `Permission` | Role X được làm Action Y trên Function Z | Manager có VIEW+EDIT trên PRODUCT |

### Tại sao cần `ActionInFunction`?

Không phải mọi Function đều cần đủ 6 Action. Ví dụ:
- `DASHBOARD` → chỉ VIEW
- `ORDER` → VIEW, EDIT, EXPORT (không có DELETE — đơn hàng không được xóa)
- `REPORT` → VIEW, EXPORT

`ActionInFunction` là bảng "danh sách action được phép cấu hình" cho từng Function, giúp UI phân quyền chỉ hiển thị các checkbox hợp lệ.

---

## 2. Entity Classes

### AppFunction

```csharp
// AuthDemo.Api/Models/Entities/AppFunction.cs
public class AppFunction
{
    public string Id { get; set; }        // "USER", "PRODUCT", "ORDER"
    public string Name { get; set; }      // "Quản lý User"
    public string? Url { get; set; }      // "/admin/users"
    public string? ParentId { get; set; } // Hỗ trợ menu lồng nhau
    public int SortOrder { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; }
    public virtual ICollection<ActionInFunction> ActionInFunctions { get; set; }
}
```

### AppAction

```csharp
// AuthDemo.Api/Models/Entities/AppAction.cs
// Đặt tên AppAction (không phải Action) để tránh conflict với System.Action
public class AppAction
{
    public string Id { get; set; }   // "VIEW", "CREATE", "EDIT", "DELETE"
    public string Name { get; set; } // "Xem", "Thêm", "Sửa", "Xóa"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
```

### Permission — Composite Primary Key

```csharp
// AuthDemo.Api/Models/Entities/Permission.cs
public class Permission
{
    public Guid RoleId { get; set; }     // FK → AspNetRoles
    public string FunctionId { get; set; } // FK → AppFunction
    public string ActionId { get; set; }   // FK → AppAction
    // PK = (RoleId, FunctionId, ActionId) — cấu hình trong DbContext
}
```

### DbContext Configuration

```csharp
// Composite PK cho Permission
builder.Entity<Permission>(e =>
{
    e.ToTable("Permissions");
    e.HasKey(x => new { x.RoleId, x.FunctionId, x.ActionId }); // ← Composite PK

    e.HasOne(x => x.Function).WithMany(f => f.Permissions)
     .HasForeignKey(x => x.FunctionId);

    e.HasOne(x => x.Action).WithMany(a => a.Permissions)
     .HasForeignKey(x => x.ActionId);
});
```

---

## 3. Authorization Infrastructure

### Vấn đề: `[Authorize(Roles = "Admin")]` không đủ

Role-based authorization đơn giản có giới hạn:
- Không thể phân quyền chi tiết: User A là Manager nhưng không được xóa sản phẩm
- Phải hardcode role name trong code → thay đổi cơ cấu tổ chức phải sửa code

### Giải pháp: Policy-based với custom Permission

#### Bước 1 — PermissionRequirement

```csharp
// Đây là "yêu cầu" — chứa thông tin cần kiểm tra
public class PermissionRequirement : IAuthorizationRequirement
{
    public string FunctionId { get; }
    public string ActionId { get; }

    public PermissionRequirement(string functionId, string actionId)
    {
        FunctionId = functionId;
        ActionId = actionId;
    }
}
```

#### Bước 2 — PermissionPolicyProvider

```csharp
// Tự động tạo AuthorizationPolicy khi gặp tên policy có format "Permission:FUNC:ACTION"
// Không cần đăng ký trước toàn bộ combinations
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix))
            return _fallback.GetPolicyAsync(policyName);

        // Parse "Permission:USER:VIEW" → FunctionId="USER", ActionId="VIEW"
        var parts = policyName[PolicyPrefix.Length..].Split(':');
        var policy = new AuthorizationPolicyBuilder(/* Bearer scheme */);
        policy.AddRequirements(new PermissionRequirement(parts[0], parts[1]));
        return Task.FromResult(policy.Build());
    }
}
```

#### Bước 3 — PermissionAuthorizationHandler

```csharp
// Chứa logic thực tế: truy vấn DB kiểm tra permission
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.GetClaim(OpenIddictConstants.Claims.Subject);
        // ...

        // Admin bypass toàn bộ permission check
        if (roles.Contains("Admin")) { context.Succeed(requirement); return; }

        // Kiểm tra DB
        var hasPermission = await dbContext.Permissions
            .AnyAsync(p =>
                roleIds.Contains(p.RoleId) &&
                p.FunctionId == requirement.FunctionId &&
                p.ActionId == requirement.ActionId);

        if (hasPermission) context.Succeed(requirement);
    }
}
```

> **Lưu ý về Lifetime**: Handler đăng ký là `Singleton`, nhưng `DbContext` là `Scoped`.
> Dùng `IServiceScopeFactory` để tạo scope mới mỗi lần kiểm tra → tránh lỗi lifetime.

#### Bước 4 — HasPermissionAttribute

```csharp
// Attribute tiện lợi, tự động tạo đúng tên policy
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string functionId, string actionId)
        : base($"Permission:{functionId}:{actionId}")  // → "Permission:USER:VIEW"
    {
    }
}
```

#### Đăng ký trong Program.cs

```csharp
// Đăng ký Provider và Handler
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

---

## 4. Sử dụng trong Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Chỉ user có quyền VIEW trên module USER mới truy cập được
    [HttpGet]
    [HasPermission("USER", "VIEW")]
    public async Task<IActionResult> GetAll() { ... }

    // Chỉ user có quyền CREATE trên module USER mới tạo được
    [HttpPost]
    [HasPermission("USER", "CREATE")]
    public async Task<IActionResult> Create(...) { ... }

    // Chỉ user có quyền DELETE trên module USER mới xóa được
    [HttpDelete("{id:guid}")]
    [HasPermission("USER", "DELETE")]
    public async Task<IActionResult> Delete(Guid id) { ... }
}
```

**Flow xử lý khi request đến:**
```
Request → Middleware → [HasPermission("USER","VIEW")]
    → ASP.NET Core tìm policy "Permission:USER:VIEW"
    → PermissionPolicyProvider tạo policy với PermissionRequirement
    → PermissionAuthorizationHandler.HandleRequirementAsync()
    → Query DB: SELECT 1 FROM Permissions WHERE RoleId IN (...) AND FunctionId='USER' AND ActionId='VIEW'
    → Succeed hoặc 403 Forbidden
```

---

## 5. API Endpoints

### User Management (`/api/users`)

| Method | URL | Permission | Mô tả |
|--------|-----|-----------|-------|
| GET | `/api/users` | USER:VIEW | Danh sách user (phân trang, tìm kiếm) |
| GET | `/api/users/{id}` | USER:VIEW | Chi tiết user |
| POST | `/api/users` | USER:CREATE | Tạo user mới |
| PUT | `/api/users/{id}` | USER:EDIT | Cập nhật thông tin |
| DELETE | `/api/users/{id}` | USER:DELETE | Xóa user |
| POST | `/api/users/{id}/change-password` | USER:EDIT | Admin đặt lại mật khẩu |
| GET | `/api/users/{id}/roles` | USER:VIEW | Xem roles của user |
| PUT | `/api/users/{id}/roles` | USER:EDIT | Gán roles cho user (replace-all) |
| POST | `/api/users/{id}/toggle-lock` | USER:EDIT | Khóa/mở khóa tài khoản |

### Role Management (`/api/roles`)

| Method | URL | Permission | Mô tả |
|--------|-----|-----------|-------|
| GET | `/api/roles` | ROLE:VIEW | Danh sách roles |
| GET | `/api/roles/{id}` | ROLE:VIEW | Chi tiết role |
| POST | `/api/roles` | ROLE:CREATE | Tạo role mới |
| PUT | `/api/roles/{id}` | ROLE:EDIT | Đổi tên role |
| DELETE | `/api/roles/{id}` | ROLE:DELETE | Xóa role |
| GET | `/api/roles/{id}/permissions` | ROLE:VIEW | Ma trận quyền của role |
| PUT | `/api/roles/{id}/permissions` | ROLE:EDIT | Lưu phân quyền (replace all) |

### GET `/api/roles/{id}/permissions` — Response mẫu

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Manager",
  "permissions": [
    {
      "functionId": "PRODUCT",
      "functionName": "Quản lý Sản phẩm",
      "actions": [
        { "actionId": "VIEW",   "actionName": "Xem",  "hasPermission": true  },
        { "actionId": "CREATE", "actionName": "Thêm", "hasPermission": false },
        { "actionId": "EDIT",   "actionName": "Sửa",  "hasPermission": true  },
        { "actionId": "DELETE", "actionName": "Xóa",  "hasPermission": false },
        { "actionId": "EXPORT", "actionName": "Xuất", "hasPermission": true  }
      ]
    }
  ]
}
```

Frontend dùng response này để render **bảng checkbox phân quyền**.

---

## 6. Seed Data

Khi khởi động lần đầu, hệ thống tự động tạo:

### Roles
- `Admin` — có toàn quyền (bypass permission check)
- `Manager` — quyền hạn chế (được seed cụ thể)
- `User` — chưa có quyền gì
- `Viewer` — chưa có quyền gì

### Users
| Email | Password | Role | Mô tả |
|-------|----------|------|-------|
| admin@demo.local | Admin@123456 | Admin | Tài khoản quản trị |
| manager@demo.local | Manager@123456 | Manager | Demo phân quyền |

### Functions & Actions

**Actions chuẩn:** VIEW, CREATE, EDIT, DELETE, EXPORT, IMPORT

**Functions:**
| Id | Tên | Actions có sẵn |
|----|-----|----------------|
| DASHBOARD | Dashboard | VIEW |
| USER | Quản lý User | VIEW, CREATE, EDIT, DELETE, EXPORT |
| ROLE | Quản lý Role | VIEW, CREATE, EDIT, DELETE |
| PRODUCT | Quản lý Sản phẩm | VIEW, CREATE, EDIT, DELETE, EXPORT, IMPORT |
| ORDER | Quản lý Đơn hàng | VIEW, EDIT, EXPORT *(không có DELETE)* |
| REPORT | Báo cáo | VIEW, EXPORT |

---

## 7. Lưu ý quan trọng

### Admin Role bypass

Admin không cần có record trong bảng Permissions. Handler luôn trả về `Succeed` khi user có role Admin:

```csharp
if (roles.Contains("Admin"))
{
    context.Succeed(requirement); // Bypass toàn bộ
    return;
}
```

### Đổi mật khẩu: Admin không cần old password

```csharp
// Admin reset password cho user khác — không cần biết mật khẩu cũ
var token = await _userManager.GeneratePasswordResetTokenAsync(user);
await _userManager.ResetPasswordAsync(user, token, newPassword);
```

### Replace-all khi lưu permissions

`PUT /api/roles/{id}/permissions` xóa toàn bộ cũ, thêm lại từ đầu:
```csharp
// Xóa cũ
_dbContext.Permissions.RemoveRange(oldPermissions);
// Thêm mới
await _dbContext.Permissions.AddRangeAsync(newPermissions);
await _dbContext.SaveChangesAsync();
```
Đơn giản hơn so với diff và merge, an toàn hơn vì không bỏ sót.

### IsActive trên User

Field `IsActive` trên ApplicationUser khác với `LockoutEnd`:
- `LockoutEnd` — tạm thời khóa (có thể tự mở hạn hoặc qua admin)
- `IsActive = false` — vô hiệu hóa vĩnh viễn tài khoản

Handler kiểm tra cả hai:
```csharp
if (user == null || !user.IsActive) return; // Không cấp quyền
```

---

## 8. Angular Frontend — Admin Module

### Kiến trúc tổng thể

```
auth-demo/src/app/
├── core/
│   ├── models/
│   │   ├── user.model.ts       ← UserDto, CreateUserRequest, UpdateUserRequest
│   │   └── role.model.ts       ← RoleDto, RolePermissionDto, FunctionPermissionDto
│   └── services/
│       ├── user.service.ts     ← HTTP calls /api/users
│       └── role.service.ts     ← HTTP calls /api/roles
└── features/
    ├── admin/
    │   ├── admin.routes.ts     ← Lazy-loaded child routes
    │   ├── layout/
    │   │   └── admin-layout.component.ts  ← Sidebar + <router-outlet>
    │   ├── users/
    │   │   └── user-list.component.ts     ← Full CRUD user
    │   └── roles/
    │       ├── role-list.component.ts     ← Danh sách roles
    │       └── role-permissions.component.ts  ← Ma trận checkbox
    └── shared/
        └── forbidden.component.ts         ← Trang 403
```

### Routing Admin (Lazy Loading)

```typescript
// app.routes.ts — route /admin lazy load toàn bộ admin module
{
  path: 'admin',
  loadChildren: () =>
    import('./features/admin/admin.routes').then(m => m.adminRoutes),
  canActivate: [authGuard],           // Kiểm tra đăng nhập
  canMatch: [roleMatchGuard('Admin')], // Kiểm tra role TRƯỚC khi load chunk
}

// admin.routes.ts — child routes bên trong admin module
export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/admin-layout.component'),
    children: [
      { path: 'users',                      loadComponent: () => import('./users/user-list.component') },
      { path: 'roles',                      loadComponent: () => import('./roles/role-list.component') },
      { path: 'roles/:id/permissions',      loadComponent: () => import('./roles/role-permissions.component') },
      { path: '', redirectTo: 'users', pathMatch: 'full' },
    ],
  },
];
```

### Permission Matrix Component

UI hiển thị ma trận quyền: Rows = Functions, Columns = Actions.

```
╔════════════════╦══════╦════════╦══════╦════════╦════════╦════════╦══════════╗
║ Chức năng      ║ VIEW ║ CREATE ║ EDIT ║ DELETE ║ EXPORT ║ IMPORT ║ Chọn tất ║
╠════════════════╬══════╬════════╬══════╬════════╬════════╬════════╬══════════╣
║ Dashboard      ║  ☑   ║   —    ║  —   ║   —    ║   —    ║   —    ║    ☑     ║
║ Quản lý User   ║  ☑   ║   ☑    ║  ☑   ║   ☐    ║   ☑    ║   —    ║    ◑     ║
║ Quản lý Role   ║  ☑   ║   ☐    ║  ☐   ║   ☐    ║   —    ║   —    ║    ◑     ║
╚════════════════╩══════╩════════╩══════╩════════╩════════╩════════╩══════════╝
```

- `☑` = có quyền  
- `☐` = không có quyền (checkbox unchecked)  
- `—` = không khả dụng (action này không thuộc function, không có trong ActionInFunctions)
- `◑` = indeterminate (một số checked, một số không)

**Computed signal để lấy tất cả action IDs:**
```typescript
allActions = computed(() => {
  const data = this.roleData();
  if (!data) return [];
  const map = new Map<string, { id: string; name: string }>();
  for (const func of data.permissions) {
    for (const act of func.actions) {
      if (!map.has(act.actionId)) map.set(act.actionId, { id: act.actionId, name: act.actionName });
    }
  }
  const ORDER = ['VIEW', 'CREATE', 'EDIT', 'DELETE', 'EXPORT', 'IMPORT'];
  return [...map.values()].sort((a, b) => ORDER.indexOf(a.id) - ORDER.indexOf(b.id));
});
```

**Toggle quyền (immutable update):**
```typescript
toggle(functionId: string, actionId: string, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked;
  this.roleData.update(data => ({
    ...data!,
    permissions: data!.permissions.map(f =>
      f.functionId !== functionId ? f : {
        ...f,
        actions: f.actions.map(a =>
          a.actionId !== actionId ? a : { ...a, hasPermission: checked }
        ),
      }
    ),
  }));
}
```

### Signals trong Angular 17

```typescript
// State quản lý bằng Angular Signals
users = signal<UserDto[]>([]);           // Reactive state
totalCount = signal(0);
page = signal(1);

// Computed signal — tự cập nhật khi dependency thay đổi
totalPages = computed(() => Math.ceil(this.totalCount() / this.PAGE_SIZE));

// Update signal (immutable)
this.users.update(list =>
  list.map(u => u.id === userId ? { ...u, isLockedOut: true } : u)
);
```

---

## 9. Angular Route Guards — canActivate vs canMatch

### Vấn đề Flash khi dùng chỉ canActivate

Khi lazy chunk đã được browser cache, sequence xảy ra:

```
User navigate → /admin
    ↓
Route matched → chunk đã cache → tải tức thì
    ↓
AdminLayoutComponent bắt đầu render (1 frame)
    ↓
canActivate guard chạy → fail → redirect /forbidden
    ↓
Flash! User thấy admin screen trong 1 frame
```

### Giải pháp: canMatch

`canMatch` chạy **TRƯỚC KHI route được match** và **TRƯỚC KHI lazy chunk được tải**:

```
User navigate → /admin
    ↓
canMatch guard chạy → user không có role Admin
    ↓
Route KHÔNG được match → chunk KHÔNG được tải
    ↓
Redirect ngay về /forbidden → KHÔNG có flash
```

### So sánh canActivate vs canMatch

| Tiêu chí | `canActivate` | `canMatch` |
|----------|---------------|------------|
| Thời điểm chạy | Sau khi route matched + chunk loaded | TRƯỚC khi route matched |
| Ngăn lazy load | Không (chunk vẫn download) | Có (chunk không bao giờ download) |
| Redirect khi fail | ✅ UrlTree | ✅ UrlTree |
| Dùng cho | Authentication check | Role/feature flag check |
| Flash với cached chunk | Có thể bị | Không bao giờ |

### Implement đúng chuẩn

```typescript
// auth.guard.ts
export const roleMatchGuard = (requiredRole: string): CanMatchFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    // Chưa đăng nhập → trả true để canActivate (authGuard) xử lý redirect về login
    if (!auth.isAuthenticated()) return true;

    // Đã đăng nhập → kiểm tra role
    if (auth.hasRole(requiredRole)) return true;

    // Không có role → redirect ngay (không load chunk)
    return router.createUrlTree(['/forbidden']);
  };

// app.routes.ts
{
  path: 'admin',
  loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
  canActivate: [authGuard],            // Xử lý chưa đăng nhập → /login
  canMatch: [roleMatchGuard('Admin')], // Xử lý không có role → /forbidden (không load chunk)
}
```

### Quy tắc thiết kế Guard

- **`canActivate`** — dùng cho **Authentication** (đã đăng nhập chưa?)
- **`canMatch`** — dùng cho **Authorization** (có quyền truy cập route này không?)
- **`canActivateChild`** — áp dụng guard cho tất cả child routes
- **`canDeactivate`** — cảnh báo trước khi rời route (VD: form chưa save)
- **`resolve`** — tải data trước khi render component

---

## 10. Cải tiến cho Production

### 1. Cache permissions

Mỗi request đang query DB 1 lần. Với traffic cao, thêm `IMemoryCache`:

```csharp
var cacheKey = $"permissions:{userId}";
if (!_cache.TryGetValue(cacheKey, out HashSet<string>? permSet))
{
    // Query DB
    permSet = ...; // "FUNCTION:ACTION" set
    _cache.Set(cacheKey, permSet, TimeSpan.FromMinutes(5));
}
// Invalidate khi thay đổi permissions
_cache.Remove($"permissions:{userId}");
```

### 2. Audit log

Thêm bảng `AuditLogs` ghi lại ai thay đổi permissions lúc nào:
```csharp
// Sau khi SavePermissions:
await _auditService.LogAsync(currentUserId, "PERMISSION_UPDATED", $"Role {roleId}");
```

### 3. Soft delete

Thay vì xóa hẳn user, set `IsActive = false`. Dữ liệu lịch sử vẫn còn nguyên.

### 4. Permission caching phía Angular

```typescript
// role.service.ts — cache kết quả permissions trong session
private permCache = new Map<string, RolePermissionDto>();

getPermissions(id: string): Observable<RolePermissionDto> {
  if (this.permCache.has(id)) return of(this.permCache.get(id)!);
  return this.http.get<RolePermissionDto>(`${this.BASE}/${id}/permissions`)
    .pipe(tap(data => this.permCache.set(id, data)));
}
```

---

## 11. Cấu trúc file

### Backend
```
AuthDemo.Api/
├── Authorization/
│   ├── HasPermissionAttribute.cs
│   ├── PermissionRequirement.cs
│   ├── PermissionPolicyProvider.cs
│   └── PermissionAuthorizationHandler.cs
├── Controllers/
│   ├── UsersController.cs
│   └── RolesController.cs
├── Models/Entities/
│   ├── AppFunction.cs
│   ├── AppAction.cs
│   ├── ActionInFunction.cs
│   └── Permission.cs
└── Services/WorkerService.cs
```

### Frontend
```
auth-demo/src/app/
├── core/
│   ├── auth/
│   │   ├── auth.guard.ts      ← authGuard + roleMatchGuard (canMatch)
│   │   ├── auth.service.ts
│   │   ├── auth.interceptor.ts
│   │   └── token.service.ts
│   ├── models/
│   │   ├── user.model.ts
│   │   └── role.model.ts
│   └── services/
│       ├── user.service.ts
│       └── role.service.ts
├── features/
│   ├── admin/
│   │   ├── admin.routes.ts
│   │   ├── layout/admin-layout.component.ts
│   │   ├── users/user-list.component.ts
│   │   └── roles/
│   │       ├── role-list.component.ts
│   │       └── role-permissions.component.ts
│   ├── auth/login/login.component.ts
│   ├── dashboard/dashboard.component.ts
│   └── shared/forbidden.component.ts
└── app.routes.ts
```
