# Tài Liệu Nghiên Cứu: AuthDemo — ASP.NET Core 8 + OpenIddict + Angular 17

> **Mục tiêu:** Hiểu sâu luồng xác thực OAuth2/OIDC theo kiến trúc ROPC, chiến lược lưu token an toàn, và các quyết định thiết kế thực tế.

---

## 1. Kiến Trúc Tổng Quan

```
┌─────────────────────────────────────────────────────────────────┐
│                        TRÌNH DUYỆT                              │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Angular SPA (localhost:4200)                             │   │
│  │                                                          │   │
│  │  AuthService ──► TokenService (access_token in memory)  │   │
│  │      │                                                   │   │
│  │  authInterceptor (tự đính Bearer token)                  │   │
│  │      │                                                   │   │
│  │  APP_INITIALIZER (auto-login khi F5)                     │   │
│  └──────────────┬───────────────────────────────────────────┘   │
│                 │ HTTP + withCredentials: true                   │
│  ┌──────────────▼─────────────────────────────────────────┐     │
│  │  Cookie Store (refresh_token — HttpOnly, SameSite)      │     │
│  │  JS KHÔNG ĐỌC ĐƯỢC — browser tự gửi kèm request        │     │
│  └─────────────────────────────────────────────────────────┘     │
└──────────────────────────┬──────────────────────────────────────┘
                           │ CORS AllowCredentials
┌──────────────────────────▼──────────────────────────────────────┐
│                 ASP.NET Core 8 (localhost:5000)                  │
│                                                                  │
│  RefreshTokenCookieMiddleware (intercept trước OpenIddict)       │
│       │  Request: inject cookie → form body                      │
│       │  Response: extract refresh_token → set HttpOnly cookie   │
│                                                                  │
│  OpenIddict Server (/connect/token)                              │
│       ├── Password Grant → issue access_token + refresh_token    │
│       └── Refresh Token Grant → issue access_token mới          │
│                                                                  │
│  AuthorizationController                                         │
│       ├── POST /connect/token  (passthrough từ OpenIddict)       │
│       ├── POST /connect/logout (revoke token trong DB)           │
│       └── GET  /connect/userinfo                                 │
│                                                                  │
│  ApiController (protected resources)                             │
│       ├── GET /api/me                                            │
│       ├── GET /api/admin-only  [Roles="Admin"]                   │
│       ├── GET /api/public      [AllowAnonymous]                  │
│       └── GET /api/products                                      │
│                                                                  │
│  ASP.NET Identity + Entity Framework Core + SQL Server           │
│  OpenIddict Tables: Applications, Authorizations, Tokens, Scopes │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Stack Công Nghệ

| Thành phần | Công nghệ | Phiên bản |
|---|---|---|
| Backend framework | ASP.NET Core | 8.0 |
| Authorization server | OpenIddict | 7.5.0 |
| ORM | Entity Framework Core | 8.x |
| Database | SQL Server | — |
| Identity | ASP.NET Core Identity | 8.x |
| Frontend framework | Angular | 17.3 |
| Reactive programming | RxJS | 7.8 |
| Language | TypeScript | 5.4 |
| CSS preprocessor | SCSS | — |

---

## 3. Chiến Lược Lưu Token (Security Design)

Đây là **quyết định thiết kế quan trọng nhất** của toàn bộ hệ thống:

```
                    Tấn công XSS              Tấn công CSRF
                         │                         │
ACCESS TOKEN:            │                         │
  → Lưu trong memory     ✓ An toàn (JS heap)       ✓ An toàn
  → Mất khi F5           (không persistable)       (không lưu)

REFRESH TOKEN:           │                         │
  → HttpOnly Cookie      ✓ An toàn (JS không đọc) ✓ SameSite=Strict
  → Path=/connect/token  (chỉ gửi đến đúng path)   ngăn request
                                                    cross-site
```

**Tại sao KHÔNG dùng localStorage?**
- localStorage bị XSS đọc được hoàn toàn
- Token đánh cắp được dùng mãi đến khi hết hạn
- Không có cơ chế tự bảo vệ

**Tại sao KHÔNG dùng sessionStorage?**
- Bị XSS đọc được trong cùng tab
- Mất khi đóng tab (UX tệ hơn cookie)

**Tại sao access token vào memory, refresh token vào cookie?**
- Access token ngắn hạn (15 phút) → mất khi F5 không sao, có refresh token để lấy lại
- Refresh token dài hạn (7 ngày) → PHẢI bảo vệ bằng HttpOnly cookie

---

## 4. Luồng 1: Đăng Nhập (ROPC Password Flow)

```
Angular                          Backend                       Database
   │                                │                              │
   │ 1. User nhập username/password │                              │
   │                                │                              │
   │ 2. POST /connect/token         │                              │
   │    Content-Type:               │                              │
   │    application/x-www-form-     │                              │
   │    urlencoded                  │                              │
   │    grant_type=password         │                              │
   │    username=admin@demo.local   │                              │
   │    password=Admin@123456       │                              │
   │    client_id=angular-spa       │                              │
   │    scope=profile email roles   │                              │
   │ ──────────────────────────────►│                              │
   │                                │                              │
   │                    3. RefreshTokenCookieMiddleware:           │
   │                       - Detect: grant_type=password          │
   │                       - Không inject cookie (password grant) │
   │                       - Buffer response body                  │
   │                                │                              │
   │                    4. OpenIddict validate request             │
   │                                │                              │
   │                    5. AuthorizationController.Exchange()      │
   │                       HandlePasswordGrantAsync()              │
   │                                │                              │
   │                                │── FindByNameAsync ──────────►│
   │                                │◄── user found ──────────────│
   │                                │                              │
   │                                │── IsLockedOutAsync ─────────►│
   │                                │◄── false ───────────────────│
   │                                │                              │
   │                                │── CheckPasswordSignInAsync ─►│
   │                                │◄── Succeeded: true ─────────│
   │                                │                              │
   │                                │── ResetAccessFailedCountAsync│
   │                                │                              │
   │                    6. BuildClaimsIdentityAsync():             │
   │                       - Claims.Subject = userId              │
   │                       - Claims.Name = username               │
   │                       - Claims.Email = email                  │
   │                       - "full_name" = FullName                │
   │                       - Claims.Role[] = ["Admin"]             │
   │                       - SetScopes(offline_access + scopes)   │
   │                                │                              │
   │                    7. SignIn(principal) →                     │
   │                       OpenIddict issues:                      │
   │                       - access_token (JWT, 15 phút)          │
   │                       - refresh_token (opaque, 7 ngày)        │
   │                       - Lưu refresh_token vào OpenIddict DB   │
   │                                │──── Store token ────────────►│
   │                                │                              │
   │                    8. RefreshTokenCookieMiddleware:           │
   │                       (post-process response)                 │
   │                       - Parse JSON response                   │
   │                       - Extract refresh_token                 │
   │                       - Set-Cookie: refresh_token=...; HttpOnly│
   │                       - Remove refresh_token khỏi JSON       │
   │                                │                              │
   │◄───────────────────────────────│                              │
   │    HTTP 200 + Set-Cookie header│                              │
   │    {                           │                              │
   │      "access_token": "eyJ...", │                              │
   │      "token_type": "Bearer",   │                              │
   │      "expires_in": 900,        │                              │
   │      "scope": "profile email roles"                           │
   │    }                           │                              │
   │    (refresh_token đã bị xóa)  │                              │
   │                                │                              │
   │ 9. tokenService.setAccessToken(token, 900)                    │
   │    → Lưu trong JS heap                                        │
   │    → tokenExpiry = now + (900-60)s                            │
   │                                │                              │
   │ 10. GET /connect/userinfo ─────────────────────────────────►  │
   │ ◄── { sub, name, email, roles } ──────────────────────────── │
   │                                │                              │
   │ 11. authService._isAuthenticated.set(true)                    │
   │     authService._currentUser.set(userInfo)                    │
   │     Router.navigate(['/dashboard'])                           │
```

**Lưu ý thực tế:**
- `scope=offline_access` được **force append** trong `HandlePasswordGrantAsync()` (dòng 118) — không cần client gửi, đảm bảo luôn có refresh token
- `withCredentials: true` là **bắt buộc** để browser nhận và lưu Set-Cookie header từ domain khác
- Content-Type phải là `application/x-www-form-urlencoded` — OAuth2 spec, không dùng JSON

---

## 5. Luồng 2: Tải Lại Trang (F5 / Page Refresh)

```
Browser                Angular                    Backend
   │                      │                           │
   │ F5 pressed           │                           │
   │                      │                           │
   │ 1. Angular khởi động │                           │
   │    Chạy APP_INITIALIZER trước khi render         │
   │    ↓                 │                           │
   │    initializeAuth() → authService.initAuth()     │
   │                      │                           │
   │               2. POST /connect/token             │
   │                  grant_type=refresh_token        │
   │                  client_id=angular-spa           │
   │                  (KHÔNG có refresh_token trong body)
   │                  Cookie: refresh_token=...       │
   │                  (browser tự gửi — JS không biết)│
   │                      │──────────────────────────►│
   │                      │                           │
   │                      │       3. RefreshTokenCookieMiddleware:
   │                      │          - Detect: grant_type=refresh_token
   │                      │          - refresh_token không có trong body
   │                      │          - Đọc cookie "refresh_token"
   │                      │          - Inject vào form body:
   │                      │            body += "&refresh_token=<cookie_value>"
   │                      │                           │
   │                      │       4. OpenIddict validate refresh_token:
   │                      │          - Kiểm tra DB: token còn tồn tại?
   │                      │          - Token chưa bị revoke?
   │                      │          - Chưa hết 7 ngày?
   │                      │                           │
   │                      │       5. HandleRefreshTokenGrantAsync():
   │                      │          - AuthenticateAsync() lấy principal cũ
   │                      │          - FindByIdAsync(userId) — user vẫn tồn tại?
   │                      │          - IsLockedOutAsync() — không bị khóa?
   │                      │          - BuildClaimsIdentityAsync() — claims mới
   │                      │          - SignIn() → issue access_token mới
   │                      │                           │
   │                      │◄──────────────────────────│
   │                      │  { access_token: "eyJ...", expires_in: 900 }
   │                      │  Set-Cookie: refresh_token=<new_token>
   │                      │  (refresh_token được rotate — token cũ bị xóa)
   │                      │                           │
   │               6. tokenService.setAccessToken(...)│
   │               7. GET /connect/userinfo           │
   │               8. _isAuthenticated.set(true)      │
   │                      │                           │
   │ 9. Angular render    │                           │
   │    → Router kiểm tra guard                       │
   │    → Hiện dashboard (đã auth)                    │
```

**Điểm mấu chốt:** APP_INITIALIZER **block rendering** cho đến khi `initAuth()` hoàn thành. User không thấy flash màn hình login nếu cookie còn hợp lệ.

---

## 6. Luồng 3: Gọi API Bảo Vệ (authInterceptor)

```
Angular Component         authInterceptor         Backend API
      │                        │                       │
      │ http.get('/api/me')    │                       │
      │───────────────────────►│                       │
      │                        │                       │
      │              1. token = tokenService.getAccessToken()
      │                 - Kiểm tra còn trong memory?  │
      │                 - Kiểm tra chưa hết 14 phút?  │
      │                        │                       │
      │              2. Attach header:                 │
      │                 Authorization: Bearer eyJ...   │
      │                        │───────────────────────►
      │                        │                       │
      │                        │              3. OpenIddict Validation:
      │                        │                 - Validate JWT signature
      │                        │                 - Kiểm tra expiry
      │                        │                 - Populate User claims
      │                        │                       │
      │                        │◄──────────────────────│
      │◄───────────────────────│  200 OK               │
      │                        │                       │
      │  [TRƯỜNG HỢP TOKEN HẾT HẠN]                   │
      │                        │                       │
      │              4. HTTP 401 nhận được             │
      │                        │                       │
      │              5. Không phải /connect/token?     │
      │                 → authService.refreshAccessToken()
      │                        │                       │
      │              6. POST /connect/token            │
      │                 grant_type=refresh_token       │
      │                 (cookie tự gửi kèm)            │
      │                        │───────────────────────►
      │                        │◄──────────────────────│
      │                        │  { access_token: mới }│
      │                        │                       │
      │              7. Retry request gốc với token mới│
      │                        │───────────────────────►
      │                        │◄──────────────────────│
      │◄───────────────────────│  200 OK               │
```

**Vòng lặp vô hạn (infinite loop) prevention:**
```typescript
// auth.interceptor.ts — dòng 43
if (req.url.includes('/connect/token')) {
  router.navigate(['/auth/login']);
  return throwError(() => error);
}
```
Nếu refresh token cũng bị 401 → không retry nữa, redirect login.

---

## 7. Luồng 4: Đăng Xuất (Logout)

```
Angular                    Backend                    Database
   │                           │                          │
   │ logout() được gọi         │                          │
   │                           │                          │
   │ POST /connect/logout      │                          │
   │ Authorization: Bearer ... │                          │
   │ withCredentials: true     │                          │
   │──────────────────────────►│                          │
   │                           │                          │
   │               1. [Authorize] → Validate JWT          │
   │                  Lấy userId từ Claims.Subject        │
   │                           │                          │
   │               2. FindBySubjectAsync(userId)          │
   │                  → Lấy tất cả tokens của user        │
   │                           │─────────────────────────►│
   │                           │◄────────────────────────│
   │                           │                          │
   │               3. TryRevokeAsync(token)               │
   │                  → Đánh dấu refresh_token = Revoked  │
   │                  → (Access token không lưu trong DB) │
   │                           │─────────────────────────►│
   │                           │◄────────────────────────│
   │                           │                          │
   │◄──────────────────────────│                          │
   │  200 { message: "OK" }    │                          │
   │                           │                          │
   │ 4. clearState():          │                          │
   │    tokenService.clearAll()│                          │
   │    → accessToken = null   │                          │
   │    → tokenExpiry = null   │                          │
   │    _isAuthenticated.set(false)                       │
   │    _currentUser.set(null) │                          │
   │    router.navigate(['/auth/login'])                  │
```

**Hạn chế thiết kế:** Access token (JWT) **không thể bị thu hồi ngay**. Sau logout, access token cũ vẫn hợp lệ đến khi hết 15 phút. Đây là trade-off của JWT stateless. Giải pháp nếu cần revoke ngay: dùng **token introspection** hoặc **short-lived token + blocklist**.

---

## 8. Middleware RefreshTokenCookieMiddleware — Giải Phẫu Chi Tiết

```
POST /connect/token
grant_type=refresh_token
client_id=angular-spa
(Cookie: refresh_token=abc123 — browser gửi tự động)

┌─────────────────────────────────────────────────────────┐
│ RefreshTokenCookieMiddleware.InvokeAsync()               │
│                                                         │
│ BƯỚC 1: InjectRefreshTokenFromCookie()                  │
│   - Đọc form body: grant_type=refresh_token             │
│   - Không thấy refresh_token trong body                 │
│   - Đọc cookie: refresh_token=abc123                    │
│   - Append: body += "&refresh_token=abc123"             │
│   - Reset context.Request.Body = new MemoryStream(...)  │
│                                                         │
│ BƯỚC 2: Buffer response                                 │
│   - Thay context.Response.Body = MemoryStream()         │
│   - await next(context)  → OpenIddict xử lý             │
│                                                         │
│ BƯỚC 3: MoveRefreshTokenToCookie()                      │
│   - Đọc response JSON:                                  │
│     {                                                   │
│       "access_token": "eyJ...",                         │
│       "refresh_token": "def456",  ← token mới (rotate) │
│       "expires_in": 900                                 │
│     }                                                   │
│   - Set cookie: refresh_token=def456;                   │
│     HttpOnly; SameSite=Strict; Path=/connect/token      │
│   - Xóa refresh_token khỏi JSON dict                    │
│   - Trả về:                                             │
│     {                                                   │
│       "access_token": "eyJ...",                         │
│       "expires_in": 900                                 │
│     }                                                   │
└─────────────────────────────────────────────────────────┘
```

**Tại sao middleware phải đứng TRƯỚC UseAuthentication?**

```csharp
// Program.cs — thứ tự QUAN TRỌNG
app.UseMiddleware<RefreshTokenCookieMiddleware>(); // ← trước
app.UseAuthentication();                           // ← sau
app.UseAuthorization();
```

OpenIddict validate request body **bên trong** UseAuthentication pipeline. Nếu middleware đứng sau, refresh_token đã bị OpenIddict đọc trước khi middleware kịp inject từ cookie → 401.

---

## 9. Claims & Destinations — Cách OpenIddict Quyết Định Claim Nào Vào Token Nào

```csharp
// AuthorizationController.cs — GetDestinations()

private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
{
    switch (claim.Type)
    {
        case Claims.Name:
            yield return Destinations.AccessToken;       // Luôn có trong access token
            if (identity.HasScope(Scopes.Profile))
                yield return Destinations.IdentityToken; // Chỉ có trong id_token nếu client xin scope=profile

        case Claims.Email:
            yield return Destinations.AccessToken;
            if (identity.HasScope(Scopes.Email))
                yield return Destinations.IdentityToken;

        case Claims.Role:
            yield return Destinations.AccessToken;
            if (identity.HasScope(Scopes.Roles))
                yield return Destinations.IdentityToken;

        default:
            yield return Destinations.AccessToken;      // full_name, sub → access token
    }
}
```

**Tại sao phải làm vậy?** Nếu không set destinations, OpenIddict sẽ **bỏ claim** khỏi token để tránh lộ thông tin. Đây là security-by-default của OpenIddict 7.x.

---

## 10. WorkerService — Seed Dữ Liệu Khởi Động

```
Startup
  │
  ▼
WorkerService.StartAsync()
  │
  ├─► Seed OpenIddict Application "angular-spa"
  │     ClientType = Public (không có client_secret)
  │     Permissions:
  │       - Endpoints.Token
  │       - GrantTypes.Password
  │       - GrantTypes.RefreshToken
  │       - Scopes.Profile / Email / Roles
  │
  └─► SeedDataAsync()
        ├─► Create roles: Admin, User, Viewer
        └─► Create admin: admin@demo.local / Admin@123456
              EmailConfirmed = true (bỏ qua email confirmation)
              Role = Admin
```

**Lưu ý:** WorkerService dùng `CreateScope()` riêng vì IHostedService là singleton, nhưng DbContext là scoped. Không tạo scope mới → exception `Cannot consume a scoped service from singleton`.

**Public Client vs Confidential Client:**
- **Public** (SPA, mobile): không có client_secret — không thể giữ bí mật
- **Confidential** (server-to-server): có client_secret
- ROPC flow với Public client là pattern phổ biến cho SPA (dù Authorization Code + PKCE được khuyến nghị hơn)

---

## 11. ApiController — Cách Đọc Claims Đúng Trong OpenIddict

```csharp
// ĐÚNG — OpenIddict dùng claim type theo OIDC spec
var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject); // "sub"
var roles  = User.FindAll(OpenIddictConstants.Claims.Role);           // "role"

// CÓ THỂ SAI — Microsoft dùng namespace dài hơn
var userId2 = User.FindFirstValue(ClaimTypes.NameIdentifier); // Khác với "sub"
var roles2  = User.FindAll(ClaimTypes.Role);                  // Khác với "role"
```

**Tại sao controller phải chỉ định scheme?**
```csharp
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
```

Khi dùng `AddIdentity()`, default scheme là **Cookie** (cho web app truyền thống). Nếu không chỉ định, `[Authorize]` sẽ redirect về login page thay vì trả 401 JSON.

---

## 12. Cấu Hình OpenIddict — Các Tùy Chọn Quan Trọng

```csharp
// Program.cs — phân tích từng option

options.SetTokenEndpointUris("/connect/token");
// → Chỉ đăng ký token endpoint với OpenIddict
// → Logout và UserInfo là API controller thông thường

options.AllowPasswordFlow();
options.AllowRefreshTokenFlow();
// → Chỉ bật 2 grant types này — không cần Authorization Code
// → ROPC flow phù hợp khi server + client cùng tin cậy

options.RegisterScopes("profile", "email", "roles");
// → OpenIddict 7.x: PHẢI khai báo scope hợp lệ
// → Client chỉ được xin scope đã đăng ký

options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
// → Access token ngắn: nếu bị lộ, thiệt hại giới hạn 15 phút
// → Refresh token dài: UX tốt, không cần login lại thường xuyên

options.DisableAccessTokenEncryption();
// → Mặc định OpenIddict mã hóa access token
// → Disable để API (cùng server) validate dễ dàng bằng JWT library
// → Production: nếu có resource server riêng, cần cấu hình thêm

options.UseAspNetCore()
       .EnableTokenEndpointPassthrough();
// → Passthrough: cho phép controller handle /connect/token
// → Không passthrough: OpenIddict tự xử lý hoàn toàn (không qua controller)

options.AddEphemeralEncryptionKey()
       .AddEphemeralSigningKey();
// → DEV ONLY: key được tạo ngẫu nhiên mỗi lần restart
// → Access token từ app instance trước sẽ invalid sau restart
// → Production: dùng X.509 certificate persistent
```

---

## 13. Angular Signals — State Management

```typescript
// auth.service.ts

// Signal: reactive state
private _isAuthenticated = signal(false);
private _currentUser = signal<UserInfo | null>(null);

// Computed: derived state (readonly)
readonly isAuthenticated = computed(() => this._isAuthenticated());
readonly currentUser = computed(() => this._currentUser());
```

**Tại sao dùng Signals thay vì BehaviorSubject?**
- Signals là primitive của Angular 17+, tích hợp với change detection
- Đơn giản hơn BehaviorSubject (không cần unsubscribe)
- Template có thể đọc trực tiếp: `{{ authService.currentUser()?.name }}`

**Tại sao `computed()` thay vì expose signal trực tiếp?**
- `computed()` tạo **readonly** signal — bên ngoài không `set()` được
- Bảo vệ state chỉ thay đổi qua AuthService methods

---

## 14. APP_INITIALIZER — Auto Login Sau F5

```typescript
// app.config.ts
function initializeAuth(authService: AuthService) {
  return () => authService.initAuth(); // Trả về Observable<boolean>
}

{
  provide: APP_INITIALIZER,
  useFactory: initializeAuth,
  deps: [AuthService],
  multi: true,
}
```

```typescript
// auth.service.ts — initAuth()
initAuth(): Observable<boolean> {
  return this.refreshAccessToken().pipe(
    switchMap(() => this.fetchUserInfo()),
    tap(user => {
      this.tokenService.setUserInfo(user);
      this._currentUser.set(user);
      this._isAuthenticated.set(true);
    }),
    map(() => true),
    catchError(() => {
      this._isAuthenticated.set(false);
      return of(false); // Luôn complete (không throw) để Angular tiếp tục boot
    }),
  );
}
```

**Chuỗi xử lý:**
1. `refreshAccessToken()` → POST /connect/token (cookie kèm theo tự động)
2. Nếu cookie hết hạn → 401 → `catchError` → return `of(false)` → Angular tiếp tục load, guard redirect về login
3. Nếu cookie hợp lệ → nhận access_token → fetchUserInfo() → set state → Angular render dashboard

**Quan trọng:** `catchError` phải return `of(false)` chứ không được `throwError()`. Nếu APP_INITIALIZER throw error, Angular sẽ **không boot** — trang trắng hoàn toàn.

---

## 15. Route Guards

```typescript
// auth.guard.ts

// Guard cơ bản — chặn nếu chưa đăng nhập
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  return router.createUrlTree(['/auth/login'], {
    queryParams: { returnUrl: state.url }, // Lưu URL để redirect sau login
  });
};

// Guard role — factory function
export const roleGuard = (requiredRole: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.hasRole(requiredRole)) return true;
  return router.createUrlTree(['/forbidden']);
};
```

```typescript
// app.routes.ts — cách dùng
{
  path: 'admin',
  component: DashboardComponent,
  canActivate: [authGuard, roleGuard('Admin')], // Cả 2 guard đều phải pass
}
```

---

## 16. Database Schema

```
ASP.NET Identity tables:
  AspNetUsers         → ApplicationUser (+ FullName)
  AspNetRoles         → IdentityRole
  AspNetUserRoles     → many-to-many
  AspNetUserClaims    → additional user claims
  AspNetRoleClaims    → role-based claims
  AspNetUserLogins    → external login providers
  AspNetUserTokens    → 2FA tokens, etc.

OpenIddict tables:
  OpenIddictApplications  → Registered clients (angular-spa)
  OpenIddictAuthorizations→ Authorization grants (không dùng trong ROPC)
  OpenIddictScopes        → Registered scopes
  OpenIddictTokens        → Refresh tokens (access tokens KHÔNG lưu)
```

**Tại sao access token không lưu trong DB?**
- Access token là JWT self-contained — chứa đủ thông tin để validate
- Không cần DB lookup khi verify → hiệu năng cao
- Refresh token là opaque (chuỗi ngẫu nhiên) → PHẢI tra DB để biết hợp lệ không

---

## 17. Security Headers

```csharp
// Program.cs
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
// → Chặn MIME sniffing: browser không đoán loại content

context.Response.Headers.Append("X-Frame-Options", "DENY");
// → Chặn clickjacking: không cho load trong iframe

// Dev CSP (nới lỏng cho Swagger)
"default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' blob:; ..."

// Prod CSP (chặt)
"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:"

context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
// → HSTS: Force HTTPS trong 1 năm (prod only)
```

---

## 18. Account Lockout — Chống Brute Force

```csharp
// Program.cs
options.Lockout.AllowedForNewUsers = true;
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
```

```csharp
// AuthorizationController.cs
var result = await _signInManager.CheckPasswordSignInAsync(
    user, request.Password!, lockoutOnFailure: true); // ← bật tự động tăng fail count
```

**Luồng lockout:**
1. Sai password lần 1–4: `AccessFailedCount` tăng dần
2. Sai lần 5: tài khoản bị khóa đến `now + 15 phút`
3. Đăng nhập thành công: `ResetAccessFailedCountAsync()` reset về 0
4. Trong thời gian khóa: trả error với thời điểm hết hạn cụ thể

**Vấn đề:** Thông báo "tài khoản bị khóa đến HH:mm" có thể xác nhận username tồn tại. Thực tế bảo mật cao hơn nên trả thông báo chung chung.

---

## 19. Checklist Production

### Bắt Buộc
- [ ] Đổi mật khẩu admin mặc định `Admin@123456`
- [ ] Thay `AddEphemeralEncryptionKey()` bằng X.509 certificate
- [ ] Xóa `DisableTransportSecurityRequirement()` → bắt buộc HTTPS
- [ ] Tắt Swagger UI (`app.UseSwagger()` / `app.UseSwaggerUI()`)
- [ ] Bật `RequireConfirmedEmail = true` (kích hoạt email workflow)
- [ ] Thu hẹp CORS origin (không dùng localhost trong prod)
- [ ] Không dùng `EnableSensitiveDataLogging()` (lộ password trong log)

### Nên Làm
- [ ] Implement token introspection nếu cần revoke access token ngay lập tức
- [ ] Thêm rate limiting cho `/connect/token` (chống brute force cấp network)
- [ ] Implement refresh token rotation logging (audit trail)
- [ ] Cấu hình logging infrastructure (ELK, Seq, Application Insights)
- [ ] Thêm HSTS preloading nếu domain mới

### Xem Xét
- [ ] Chuyển sang Authorization Code + PKCE (loại bỏ ROPC)
- [ ] Thêm 2FA/MFA
- [ ] Implement email confirmation workflow
- [ ] Thêm endpoint `/connect/revoke` cho client-side revocation

---

## 20. Câu Hỏi Thường Gặp Khi Học

**Q: Tại sao không dùng Authorization Code + PKCE thay vì ROPC?**

A: Authorization Code + PKCE là chuẩn hiện đại và được khuyến nghị hơn. ROPC phù hợp khi:
- Client và Authorization Server cùng do một tổ chức quản lý (first-party)
- Không có redirect được (app không có browser)
- Đây là demo/internal tool
Nhược điểm ROPC: user phải tin tưởng client với password, không hỗ trợ SSO/MFA dễ dàng.

**Q: Refresh token rotation là gì?**

A: Mỗi lần dùng refresh token, server cấp refresh token MỚI và vô hiệu hóa token cũ. OpenIddict làm điều này tự động. Lợi ích: nếu refresh token bị đánh cắp và dùng, server detect được (vì token cũ không còn hợp lệ) và có thể alert.

**Q: Tại sao SameSite=Strict mà không phải Lax?**

A: Strict: cookie chỉ gửi khi user navigate **từ chính domain đó**. Lax: cho phép gửi khi click link từ domain khác (GET requests). Vì `/connect/token` chỉ nhận POST, Strict và Lax đều bảo vệ như nhau trong thực tế. Strict an toàn hơn về mặt conceptual.

**Q: Tại sao Path=/connect/token trên cookie?**

A: Giới hạn cookie chỉ được gửi đến path `/connect/token`. Nếu không giới hạn, browser gửi refresh token cookie kèm **mọi** request đến `localhost:5000` — không cần thiết và tăng attack surface.

**Q: `_refreshInProgress` flag trong AuthService làm gì?**

A: Nếu nhiều request đồng thời nhận 401, interceptor sẽ gọi `refreshAccessToken()` nhiều lần song song. Flag này ngăn điều đó. Vấn đề: implement hiện tại chỉ block request thứ 2 nhưng không queue lại — nên dùng `shareReplay` hoặc `BehaviorSubject` để handle đúng hơn trong production.

**Q: Tại sao `DisableAccessTokenEncryption()` trong dev?**

A: Mặc định OpenIddict mã hóa access token (không phải ký — là mã hóa thực sự). Khi API server và Auth server cùng process, có thể dùng encryption key nội bộ. Khi disable: access token là JWT thuần túy, dễ debug bằng jwt.io. Production với microservice riêng biệt cần cấu hình key sharing.

---

## 21. Sơ Đồ File & Dependencies

```
AuthDemo.Api/
├── Program.cs                    ← Entry point, DI registration
├── Controllers/
│   ├── AuthorizationController   ← /connect/token, /logout, /userinfo
│   └── ApiController             ← /api/me, /admin-only, /products
├── Middleware/
│   └── RefreshTokenCookieMiddleware  ← Cookie strategy implementation
├── Services/
│   └── WorkerService             ← Seed data on startup
├── Models/
│   ├── ApplicationUser.cs        ← IdentityUser + FullName
│   └── Requests/LoginRequest.cs
└── Data/
    ├── ApplicationDbContext.cs   ← EF + OpenIddict config
    └── Migrations/               ← DB schema history

auth-demo/src/app/
├── app.config.ts                 ← APP_INITIALIZER + HttpClient config
├── app.routes.ts                 ← Route definitions với guards
├── core/
│   ├── models/auth.model.ts      ← TokenResponse, UserInfo interfaces
│   └── auth/
│       ├── token.service.ts      ← In-memory token storage
│       ├── auth.service.ts       ← Auth state + API calls (Signals)
│       ├── auth.interceptor.ts   ← Auto Bearer + 401 handling
│       └── auth.guard.ts         ← Route protection
└── features/
    ├── auth/login/               ← Login form component
    └── dashboard/                ← Protected dashboard
```

---

*Tài liệu này được tạo từ source code thực tế của project AuthDemo (OpenIdDict). Ngày tạo: 2026-05-20.*
