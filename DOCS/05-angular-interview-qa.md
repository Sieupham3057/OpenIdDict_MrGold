# Câu hỏi & Trả lời Phỏng vấn Angular

> Tổng hợp từ dự án AuthDemo — Angular 17, Standalone Components, Signals, RxJS, Guards.

---

## Mục Lục

- [Phần 1: Angular Cơ Bản](#phan-1)
  - [Q1. Angular là gì? Angular vs AngularJS](#q1)
  - [Q2. Component là gì? Decorator metadata](#q2)
  - [Q3. Standalone Components — Angular 17](#q3)
  - [Q4. Change Detection — OnPush](#q4)
  - [Q5. @Input() và @Output() — Component Communication](#q5)
- [Phần 2: Angular 17 — Signals](#phan-2)
  - [Q6. Angular Signals — tốt hơn plain property thế nào?](#q6)
  - [Q7. signal(), computed(), effect() — phân biệt](#q7)
  - [Q8. Immutable update với Signal](#q8)
- [Phần 3: RxJS & Observable](#phan-3)
  - [Q9. Observable vs Promise — khi nào dùng cái nào?](#q9)
  - [Q10. RxJS operators thường dùng](#q10)
  - [Q11. Memory leak với Observable — phòng tránh](#q11)
- [Phần 4: Routing & Guards](#phan-4)
  - [Q12. canActivate, canMatch, canActivateChild, canDeactivate — phân biệt](#q12)
  - [Q13. canMatch vs canActivate — ngăn flash admin screen](#q13)
  - [Q14. Lazy Loading — cấu hình](#q14)
  - [Q15. Router params, query params, data — phân biệt](#q15)
- [Phần 5: Forms](#phan-5)
  - [Q16. Template-driven Forms vs Reactive Forms](#q16)
  - [Q17. Hiển thị lỗi validation trong template](#q17)
- [Phần 6: HTTP & Interceptors](#phan-6)
  - [Q18. HTTP Interceptor — auth interceptor trong project](#q18)
  - [Q19. withCredentials: true — khi nào cần?](#q19)
  - [Q20. Observable với async pipe trong template](#q20)
- [Phần 7: Performance](#phan-7)
  - [Q21. Kỹ thuật tối ưu performance Angular](#q21)
  - [Q22. Bundle size optimization](#q22)
- [Phần 8: Testing](#phan-8)
  - [Q23. Unit Test Angular Component với TestBed](#q23)
- [Phần 9: Security](#phan-9)
  - [Q24. XSS trong Angular — Angular tự bảo vệ không?](#q24)
  - [Q25. CSRF và SameSite Cookie](#q25)
  - [Q26. Content Security Policy (CSP)](#q26)
- [Phần 10: Câu Hỏi Thực Tế Từ Project](#phan-10)
  - [Q27. APP_INITIALIZER — dùng để làm gì?](#q27)
  - [Q28. roleMatchGuard trả `true` khi chưa đăng nhập — tại sao?](#q28)
  - [Q29. computed() signal gọi trong template được không?](#q29)
  - [Q30. Flow từ lúc user login đến gọi API bảo vệ](#q30)
- [Phần 11: Directives & Pipes](#phan-11)
  - [Q31. Structural Directive vs Attribute Directive — tự tạo](#q31)
  - [Q32. Pure Pipe vs Impure Pipe — use case](#q32)
  - [Q33. Content Projection — ng-content](#q33)
  - [Q34. ViewChild, ContentChild — phân biệt](#q34)
- [Phần 12: Dependency Injection Nâng Cao](#phan-12)
  - [Q35. InjectionToken — khi nào dùng thay vì class/interface?](#q35)
  - [Q36. @defer block — Angular 17 lazy rendering](#q36)
- [Phần 13: Component Patterns Nâng Cao](#phan-13)
  - [Q37. New Control Flow — @if, @for, @switch vs *ngIf, *ngFor](#q37)
  - [Q38. effect() trong Signals — pitfalls cần tránh](#q38)
  - [Q39. HostListener và HostBinding — use case](#q39)
  - [Q40. Signal-based input() và output() — Angular 17.1+](#q40)
  - [Q41. Preloading Strategy trong Angular Router](#q41)
  - [Q42. State Management với Signal/Service — không cần NgRx](#q42)
  - [Q43. NgOptimizedImage — image optimization](#q43)
  - [Q44. Standalone component testing — pattern](#q44)
  - [Q45. SSR với Angular 17 — khi nào cần? Pitfalls?](#q45)
- [Phần 14: Kiến Trúc Frontend (Technical Leader)](#phan-14)
  - [Q46. Micro-Frontend Architecture — Module Federation](#q46)
  - [Q47. Nx Monorepo — tổ chức Angular project](#q47)
  - [Q48. Design System — xây dựng component library](#q48)
  - [Q49. Performance Profiling — tìm và fix bottleneck](#q49)
  - [Q50. NgRx ở quy mô lớn — khi nào cần?](#q50)
  - [Q51. Testing Strategy — Test Pyramid áp dụng thế nào?](#q51)
  - [Q52. CI/CD Pipeline cho Angular — GitHub Actions](#q52)
  - [Q53. Accessibility (a11y) — TL cần đảm bảo gì?](#q53)
  - [Q54. Internationalization (i18n) — localization strategy](#q54)
- [Phần 15: Technical Leadership Mindset](#phan-15)
  - [Q55. Chọn thư viện / framework — TL evaluate thế nào?](#q55)
  - [Q56. Developer không theo convention — TL xử lý thế nào?](#q56)
  - [Q57. Upgrade Angular version — strategy cho large codebase](#q57)
  - [Q58. Web Performance — Core Web Vitals cho Angular SPA](#q58)
  - [Q59. Technical Roadmap cho team Frontend](#q59)
- [Phần 16: Docker & Deployment Angular (Production-grade)](#phan-16-angular)
  - [Q60. Docker deployment Angular SPA — Nginx multi-stage build](#q60)
  - [Q61. Full-stack Docker Compose — Angular + .NET API + SQL Server + Redis](#q61)
  - [Q62. Runtime configuration Angular production — không hardcode API URL](#q62)
  - [Q63. Kubernetes deploy Angular SPA — Deployment, ConfigMap, Ingress](#q63)
- [Phần 17: Angular 18/19 & Advanced Signals](#phan-17-angular)
  - [Q64. Angular 18/19 — Zoneless Change Detection thực tế](#q64)
  - [Q65. linkedSignal() và resource() — Angular 19 mới](#q65)
  - [Q66. toSignal() và fromSignal() — bridge RxJS với Signals](#q66)
- [Phần 18: Micro-Frontend & Module Federation Nâng Cao](#phan-18-angular)
  - [Q67. Module Federation runtime sharing — pitfalls thực tế](#q67)
  - [Q68. State sharing giữa Micro-Frontend apps](#q68)

---

<a id="phan-1"></a>
## PHẦN 1: ANGULAR CƠ BẢN

---

<a id="q1"></a>
**Q1. Angular là gì? Phân biệt Angular với AngularJS?**

Angular (từ v2+) là framework TypeScript do Google phát triển để xây dựng SPA. Khác hoàn toàn với AngularJS (v1.x):

| | AngularJS (v1) | Angular (v2+) |
|--|----------------|---------------|
| Ngôn ngữ | JavaScript | TypeScript |
| Kiến trúc | MVC | Component-based |
| Data binding | Two-way (ng-model) | One-way + EventEmitter |
| Performance | Digest cycle | Zone.js + Change Detection |
| Mobile | Không tối ưu | Mobile-first |

---

<a id="q2"></a>
**Q2. Component là gì? Giải thích các decorator metadata?**

Component là khối UI cơ bản nhất trong Angular. `@Component` decorator cấu hình:

```typescript
@Component({
  selector: 'app-user-list',   // HTML tag: <app-user-list>
  standalone: true,            // Angular 17: không cần NgModule
  imports: [CommonModule, ReactiveFormsModule], // Dependencies
  templateUrl: './user-list.component.html',    // External template
  // template: `...`,          // Hoặc inline template
  styleUrls: ['./user-list.component.scss'],    // External styles
  // styles: [`...`],          // Hoặc inline styles
  changeDetection: ChangeDetectionStrategy.OnPush // Performance optimization
})
export class UserListComponent {}
```

---

<a id="q3"></a>
**Q3. Standalone Components là gì? Tại sao Angular 17 dùng thay vì NgModule?**

Standalone Component không cần khai báo trong `NgModule`. Tự khai báo dependencies qua `imports` array.

**Lợi ích:**
- Ít boilerplate hơn (không cần module file)
- Lazy loading đơn giản hơn: `loadComponent` thay vì `loadChildren` + module
- Tree-shaking hiệu quả hơn (chỉ bundle những gì thực sự dùng)
- Dễ test độc lập

```typescript
// Angular < 17 (NgModule approach)
@NgModule({
  declarations: [UserListComponent],
  imports: [CommonModule, ReactiveFormsModule],
})
export class UserModule { }

// Angular 17 (Standalone)
@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule], // Import trực tiếp
})
export class UserListComponent { }
```

---

<a id="q4"></a>
**Q4. Change Detection hoạt động như thế nào? OnPush là gì?**

Angular mặc định (Default strategy) kiểm tra toàn bộ component tree sau mỗi event, HTTP call, hoặc setTimeout.

**OnPush**: Chỉ re-render component khi:
1. Input reference thay đổi (shallow comparison)
2. Event trong component xảy ra
3. Observable được subscribe với `async` pipe emit value
4. `ChangeDetectorRef.markForCheck()` được gọi thủ công
5. **Signal thay đổi** (Angular 17+)

```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent {
  // Với OnPush, phải dùng immutable update
  users = signal<UserDto[]>([]); // Signal tự trigger CD khi thay đổi

  // SAI với OnPush
  // this.users.push(newUser); // Mutate array, reference không đổi → không re-render

  // ĐÚNG với OnPush
  // this.users.update(list => [...list, newUser]); // New reference → re-render
}
```

---

<a id="q5"></a>
**Q5. Giải thích `@Input()` và `@Output()` — Component Communication?**

```typescript
// PARENT → CHILD: @Input
@Component({ selector: 'app-user-card' })
export class UserCardComponent {
  @Input() user!: UserDto;                      // Nhận data từ parent
  @Input({ required: true }) userId!: string;   // Angular 16+: required input
}
// Sử dụng: <app-user-card [user]="selectedUser" />

// CHILD → PARENT: @Output + EventEmitter
@Component({ selector: 'app-user-form' })
export class UserFormComponent {
  @Output() saved = new EventEmitter<UserDto>();
  @Output() cancelled = new EventEmitter<void>();

  onSave(user: UserDto) { this.saved.emit(user); }
}
// Sử dụng: <app-user-form (saved)="onUserSaved($event)" />
```

---

<a id="phan-2"></a>
## PHẦN 2: ANGULAR 17 — SIGNALS

---

<a id="q6"></a>
**Q6. Angular Signals là gì? Tại sao tốt hơn so với plain property?**

Signal là reactive primitive — giá trị có thể theo dõi sự thay đổi. Khi signal thay đổi, Angular tự biết cần re-render component nào mà không cần Zone.js scan toàn bộ tree.

```typescript
// Plain property — Angular không biết khi nào đổi
count = 0;
increment() { this.count++; } // Phải chạy change detection toàn bộ tree

// Signal — Angular track chính xác dependency
count = signal(0);
increment() { this.count.update(v => v + 1); } // Chỉ re-render component dùng count

// Các loại signal
const name = signal('John');           // WritableSignal
const upper = computed(() => name().toUpperCase()); // ComputedSignal (readonly)

// Đọc signal — luôn gọi như function
console.log(name());   // 'John'
console.log(upper());  // 'JOHN'

// Cập nhật
name.set('Jane');              // Set giá trị mới
name.update(v => v + '!');     // Update dựa trên giá trị cũ
```

---

<a id="q7"></a>
**Q7. Phân biệt `signal()`, `computed()`, và `effect()`?**

```typescript
// signal() — WritableSignal, có thể set/update
const page = signal(1);
const search = signal('');

// computed() — readonly, tự tính lại khi dependency thay đổi
const totalPages = computed(() => Math.ceil(this.totalCount() / 10));
// Được cache — chỉ tính lại khi totalCount thay đổi

// effect() — side effect khi signal thay đổi (tránh lạm dụng)
effect(() => {
  console.log('Page changed to:', this.page());
  // Tự động re-run khi page() thay đổi
});
```

**Khi nào dùng:**
- `signal` → state cần thay đổi
- `computed` → derived state từ signals khác
- `effect` → sync với browser API, logging (dùng ít, không dùng để update signal khác)

---

<a id="q8"></a>
**Q8. Immutable update với Signal — tại sao quan trọng?**

Angular Signal và OnPush change detection đều dùng **reference comparison**. Mutating array/object không trigger re-render.

```typescript
users = signal<UserDto[]>([]);

// SAI — mutate trực tiếp, Angular không phát hiện thay đổi
addUser(newUser: UserDto) {
  this.users().push(newUser); // Push vào array cũ → reference không đổi!
}

// ĐÚNG — tạo array mới
addUser(newUser: UserDto) {
  this.users.update(list => [...list, newUser]); // Spread tạo reference mới
}

// ĐÚNG — update 1 phần tử trong list
updateUser(updated: UserDto) {
  this.users.update(list =>
    list.map(u => u.id === updated.id ? { ...updated } : u) // Object spread
  );
}

// ĐÚNG — xóa phần tử
deleteUser(id: string) {
  this.users.update(list => list.filter(u => u.id !== id));
}
```

---

<a id="phan-3"></a>
## PHẦN 3: RXJS & OBSERVABLE

---

<a id="q9"></a>
**Q9. Observable vs Promise — khi nào dùng cái nào?**

| | Observable | Promise |
|--|------------|---------|
| Lazy | Có (chỉ chạy khi subscribe) | Không (chạy ngay) |
| Multiple values | Có (stream) | Không (1 value) |
| Cancellable | Có (unsubscribe) | Không |
| Operators | Phong phú (map, filter, retry...) | Hạn chế |

```typescript
// Observable — HTTP request trong Angular
this.http.get<UserDto[]>('/api/users')
  .pipe(
    retry(3),                        // Retry 3 lần khi lỗi
    map(users => users.filter(u => u.isActive)), // Transform
    catchError(err => {              // Error handling
      return throwError(() => new Error('Failed'));
    })
  )
  .subscribe({ next: data => ..., error: err => ... });

// Promise — chỉ cần 1 value, không cần cancel
const token = await fetch('/connect/token').then(r => r.json());
```

Angular `HttpClient` trả về Observable (lazy, cancellable), phù hợp với HTTP requests.

---

<a id="q10"></a>
**Q10. Giải thích các RxJS operators thường dùng?**

```typescript
// map — transform giá trị
of(1, 2, 3).pipe(map(x => x * 2)) // → 2, 4, 6

// filter — lọc giá trị
of(1, 2, 3, 4).pipe(filter(x => x % 2 === 0)) // → 2, 4

// switchMap — hủy observable cũ, subscribe observable mới
searchControl.valueChanges.pipe(
  debounceTime(300),
  switchMap(term => this.http.get(`/api/users?search=${term}`))
  // Nếu user gõ nhanh, chỉ call API lần cuối cùng
)

// mergeMap — subscribe song song, không hủy
// concatMap — subscribe tuần tự, chờ lần trước xong
// exhaustMap — ignore request mới khi đang xử lý (dùng cho login button)

// catchError — xử lý lỗi và return fallback
.pipe(
  catchError(err => of([])) // Trả empty array khi lỗi
)

// tap — side effect không thay đổi stream (logging, set signal)
.pipe(
  tap(data => this.loading.set(false))
)

// takeUntilDestroyed (Angular 16+) — tự unsubscribe khi component destroy
.pipe(takeUntilDestroyed())
```

---

<a id="q11"></a>
**Q11. Memory leak với Observable — cách phòng tránh?**

Nếu không unsubscribe, Observable tiếp tục chạy sau khi component bị destroy → memory leak.

```typescript
// ❌ LEAK — không unsubscribe
ngOnInit() {
  this.userService.getAll().subscribe(data => this.users = data);
  // subscribe vẫn tồn tại sau khi component destroy
}

// ✅ Cách 1 — unsubscribe thủ công
private destroy$ = new Subject<void>();
ngOnInit() {
  this.userService.getAll()
    .pipe(takeUntil(this.destroy$))
    .subscribe(data => this.users = data);
}
ngOnDestroy() {
  this.destroy$.next();
  this.destroy$.complete();
}

// ✅ Cách 2 — Angular 16+ DestroyRef
private destroyRef = inject(DestroyRef);
ngOnInit() {
  this.userService.getAll()
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(data => this.users = data);
}

// ✅ Cách 3 — async pipe (tự unsubscribe)
// template: {{ users$ | async | json }}
users$ = this.userService.getAll();
```

---

<a id="phan-4"></a>
## PHẦN 4: ROUTING & GUARDS

---

<a id="q12"></a>
**Q12. Phân biệt `canActivate`, `canMatch`, `canActivateChild`, `canDeactivate`?**

| Guard | Khi nào chạy | Dùng cho |
|-------|-------------|---------|
| `canMatch` | Trước khi route được match, TRƯỚC khi lazy chunk load | Role/feature flag — ngăn flash |
| `canActivate` | Sau khi route matched, trước khi component render | Authentication check |
| `canActivateChild` | Trước khi child route activate | Guard cho toàn bộ child routes |
| `canDeactivate` | Khi rời khỏi route | Cảnh báo "Form chưa lưu" |
| `resolve` | Trước khi component render | Pre-load data |

```typescript
// Functional guard (Angular 14+) — không cần class
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

// canDeactivate — cảnh báo unsaved changes
export const unsavedGuard: CanDeactivateFn<HasPendingChanges> = (component) => {
  if (component.hasUnsavedChanges()) {
    return confirm('Bạn có thay đổi chưa lưu. Thoát không?');
  }
  return true;
};
```

---

<a id="q13"></a>
**Q13. Tại sao dùng `canMatch` thay vì `canActivate` để ngăn flash admin screen?**

**Vấn đề với chỉ `canActivate`:**

Khi lazy chunk đã được browser cache (lần navigate thứ 2+):
1. Route matched
2. Chunk load tức thì (từ cache)
3. Component bắt đầu render → **flash xảy ra**
4. `canActivate` guard chạy → fail → redirect

**Giải pháp `canMatch`:**

`canMatch` chạy TRƯỚC KHI route được match → trước khi lazy chunk load:
1. `canMatch` guard chạy → fail → route không được match
2. Chunk KHÔNG được load → component KHÔNG render → **không có flash**

```typescript
// auth.guard.ts
export const roleMatchGuard = (requiredRole: string): CanMatchFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) return true; // Để canActivate xử lý → redirect login
    if (auth.hasRole(requiredRole)) return true;
    return router.createUrlTree(['/forbidden']); // Redirect, không load chunk
  };

// app.routes.ts
{
  path: 'admin',
  loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
  canActivate: [authGuard],            // Xử lý: chưa login → /login
  canMatch: [roleMatchGuard('Admin')], // Xử lý: không có role → /forbidden (không load chunk)
}
```

---

<a id="q14"></a>
**Q14. Lazy Loading trong Angular — cấu hình như thế nào?**

```typescript
// Lazy load component (Angular 17)
{
  path: 'users',
  loadComponent: () =>
    import('./users/user-list.component').then(m => m.UserListComponent),
}

// Lazy load routes (module-less, Angular 17)
{
  path: 'admin',
  loadChildren: () =>
    import('./features/admin/admin.routes').then(m => m.adminRoutes),
}

// admin.routes.ts — trả về Routes array (không cần NgModule)
export const adminRoutes: Routes = [
  { path: 'users', loadComponent: () => import('./users/user-list.component')... },
  { path: 'roles', loadComponent: () => import('./roles/role-list.component')... },
];
```

**Lợi ích Lazy Loading:**
- Bundle ban đầu nhỏ hơn → load nhanh hơn
- Chỉ download code khi user thực sự cần
- Chrome DevTools → Network → xem chunk được load khi navigate

---

<a id="q15"></a>
**Q15. Router params, query params, và data — phân biệt?**

```typescript
// Route params — là một phần của URL path
{ path: 'roles/:id/permissions', ... }
// URL: /admin/roles/abc-123/permissions
// Lấy: this.route.snapshot.paramMap.get('id')
// Reactive: this.route.paramMap.pipe(map(p => p.get('id')))

// Query params — sau dấu ?
// URL: /api/users?page=2&search=admin
// Lấy: this.route.snapshot.queryParamMap.get('page')
// Set: this.router.navigate(['/users'], { queryParams: { page: 2 } })

// Route data — static data trong route config
{ path: 'admin', data: { title: 'Admin Panel', requiredRole: 'Admin' } }
// Lấy: this.route.snapshot.data['title']
```

---

<a id="phan-5"></a>
## PHẦN 5: FORMS

---

<a id="q16"></a>
**Q16. Template-driven Forms vs Reactive Forms — khi nào dùng cái nào?**

| | Template-driven | Reactive Forms |
|--|-----------------|----------------|
| Setup | `[(ngModel)]` | `FormBuilder.group()` |
| Validation | HTML attributes | Validators array |
| Testing | Khó (DOM) | Dễ (pure TS) |
| Complex logic | Khó | Dễ |
| Async validation | Phức tạp | Đơn giản |
| Dùng cho | Form đơn giản, ít field | Form phức tạp, nhiều validation |

```typescript
// Reactive Form
this.userForm = this.fb.group({
  email: ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(8)]],
  roles: [[], Validators.required],
});

// Custom Validator
function passwordStrength(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value;
  if (!value) return null;
  const hasUpperCase = /[A-Z]/.test(value);
  const hasNumber = /[0-9]/.test(value);
  return hasUpperCase && hasNumber ? null : { weakPassword: true };
}

// Cross-field Validator
function passwordMatch(group: AbstractControl): ValidationErrors | null {
  const pass = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return pass === confirm ? null : { passwordMismatch: true };
}
```

---

<a id="q17"></a>
**Q17. Làm thế nào hiển thị lỗi validation trong template?**

```typescript
// Component
hasError(field: string): boolean {
  const control = this.form.get(field);
  return !!(control?.invalid && control.touched);
}
```

```html
<!-- Template -->
<input formControlName="email" [class.invalid]="hasError('email')" />
<span *ngIf="hasError('email')">
  <span *ngIf="form.get('email')?.errors?.['required']">Email bắt buộc nhập</span>
  <span *ngIf="form.get('email')?.errors?.['email']">Email không hợp lệ</span>
</span>

<!-- Hoặc Angular 17 new control flow -->
@if (hasError('email')) {
  @if (form.get('email')?.errors?.['required']) { <span>Bắt buộc</span> }
  @if (form.get('email')?.errors?.['email']) { <span>Không hợp lệ</span> }
}
```

---

<a id="phan-6"></a>
## PHẦN 6: HTTP & INTERCEPTORS

---

<a id="q18"></a>
**Q18. HTTP Interceptor là gì? Giải thích auth interceptor trong project?**

Interceptor là middleware cho HttpClient — can thiệp vào tất cả request trước khi gửi và response sau khi nhận.

```typescript
// Functional interceptor (Angular 15+)
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = tokenService.getAccessToken();
  if (!token) return next(req);

  // Clone request và thêm Authorization header
  const authReq = req.clone({
    headers: req.headers.set('Authorization', `Bearer ${token}`)
  });

  return next(authReq).pipe(
    catchError(err => {
      if (err.status === 401) {
        // Token hết hạn → thử refresh
        return authService.refreshAccessToken().pipe(
          switchMap(newToken => {
            const retryReq = req.clone({
              headers: req.headers.set('Authorization', `Bearer ${newToken.access_token}`)
            });
            return next(retryReq);
          }),
          catchError(() => {
            // Refresh fail → logout
            router.navigate(['/login']);
            return throwError(() => err);
          })
        );
      }
      return throwError(() => err);
    })
  );
};

// Đăng ký trong app.config.ts
provideHttpClient(withInterceptors([authInterceptor]))
```

---

<a id="q19"></a>
**Q19. `withCredentials: true` là gì và khi nào cần?**

`withCredentials: true` yêu cầu browser đính kèm cookies, authorization headers, và TLS client certificates vào cross-origin request.

```typescript
// Cần withCredentials khi:
// - Server set HttpOnly cookie (refresh_token)
// - Cross-origin request (Angular trên port 4200, API trên port 5000)

this.http.post('/connect/token', body, {
  withCredentials: true  // ← Browser sẽ gửi HttpOnly cookie kèm request
});

// CORS trên server phải cho phép credentials:
// policy.WithOrigins("http://localhost:4200").AllowCredentials()
// Không thể dùng AllowAnyOrigin() với AllowCredentials()
```

---

<a id="q20"></a>
**Q20. Phân biệt `Observable` với `async pipe` trong template?**

```typescript
// Cách 1: Subscribe thủ công — cần unsubscribe
ngOnInit() {
  this.userService.getAll().subscribe(data => this.users = data);
}

// Cách 2: async pipe — tự unsubscribe, tự trigger change detection
// Template:
// <div *ngFor="let user of users$ | async">{{ user.name }}</div>
users$ = this.userService.getAll(); // Không subscribe
```

**async pipe tốt hơn vì:**
- Tự unsubscribe khi component destroy (không leak)
- Tương thích với OnPush change detection
- Code đơn giản hơn

---

<a id="phan-7"></a>
## PHẦN 7: PERFORMANCE

---

<a id="q21"></a>
**Q21. Các kỹ thuật tối ưu performance Angular?**

```typescript
// 1. OnPush Change Detection
@Component({ changeDetection: ChangeDetectionStrategy.OnPush })

// 2. trackBy trong *ngFor — tránh re-render toàn bộ list
<div *ngFor="let user of users; trackBy: trackByUserId">
trackByUserId(index: number, user: UserDto): string { return user.id; }

// 3. Lazy Loading — chỉ load module khi cần
loadChildren: () => import('./admin.routes')...

// 4. async pipe thay vì subscribe thủ công

// 5. Signals — fine-grained reactivity (Angular 17)

// 6. Pure Pipe thay vì method trong template
// Template method được gọi mỗi CD cycle:
// ❌ {{ formatDate(user.createdAt) }} — gọi mỗi lần CD
// ✅ {{ user.createdAt | customDate }} — chỉ tính lại khi input thay đổi

// 7. Virtual Scrolling cho list dài
import { ScrollingModule } from '@angular/cdk/scrolling';
<cdk-virtual-scroll-viewport itemSize="50">
  <div *cdkVirtualFor="let user of users">{{ user.name }}</div>
</cdk-virtual-scroll-viewport>
```

---

<a id="q22"></a>
**Q22. Bundle size optimization — cách giảm bundle size?**

```typescript
// 1. Lazy loading (quan trọng nhất)
// 2. Tree shaking — import cụ thể, không import cả library
import { map, filter } from 'rxjs/operators'; // ✅
// import * as Rx from 'rxjs'; // ❌

// 3. Dùng standalone components (tốt hơn NgModule cho tree-shaking)

// 4. Analyze bundle
// ng build --stats-json
// npx webpack-bundle-analyzer dist/stats.json

// 5. Preload strategy — balance giữa initial load và UX
// NoPreloading: chỉ load khi cần (tốt cho ít route)
// PreloadAllModules: preload background sau initial load
// Custom strategy: chỉ preload route user likely sẽ visit
```

---

<a id="phan-8"></a>
## PHẦN 8: TESTING

---

<a id="q23"></a>
**Q23. Unit Test Angular Component với TestBed?**

```typescript
describe('UserListComponent', () => {
  let component: UserListComponent;
  let fixture: ComponentFixture<UserListComponent>;
  let userService: jasmine.SpyObj<UserService>;

  beforeEach(async () => {
    const userSpy = jasmine.createSpyObj('UserService', ['getAll', 'delete']);
    userSpy.getAll.and.returnValue(of({ data: mockUsers, totalCount: 2 }));

    await TestBed.configureTestingModule({
      imports: [UserListComponent], // Standalone component
      providers: [
        { provide: UserService, useValue: userSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load users on init', () => {
    expect(component.users().length).toBe(2);
    expect(component.totalCount()).toBe(2);
  });

  it('should call delete when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    userService.delete.and.returnValue(of(undefined));

    component.confirmDelete(mockUsers[0]);

    expect(userService.delete).toHaveBeenCalledWith(mockUsers[0].id);
  });
});
```

---

<a id="phan-9"></a>
## PHẦN 9: SECURITY

---

<a id="q24"></a>
**Q24. XSS trong Angular — Angular có tự bảo vệ không?**

Angular **tự động escape** tất cả string binding trong template để ngăn XSS:

```html
<!-- Angular escape HTML — safe -->
<p>{{ userInput }}</p>
<!-- Nếu userInput = "<script>alert('xss')</script>"
     Render ra: &lt;script&gt;alert('xss')&lt;/script&gt; -->

<!-- NGUY HIỂM — bypass sanitization -->
<div [innerHTML]="userInput"></div>
<!-- Angular sẽ sanitize, nhưng tốt nhất tránh innerHTML với user input -->

<!-- Nếu PHẢI dùng HTML động, dùng DomSanitizer -->
constructor(private sanitizer: DomSanitizer) {}
safeHtml = this.sanitizer.bypassSecurityTrustHtml(trustedHtml);
// Chỉ dùng với content TIN TƯỞNG HOÀN TOÀN
```

---

<a id="q25"></a>
**Q25. CSRF và SameSite Cookie trong Angular?**

```
CSRF (Cross-Site Request Forgery):
Attacker dụ user click link → browser tự gửi cookie → API nhận và thực thi

Phòng chống:
1. SameSite=Strict/Lax cookie → browser không gửi cookie cross-site
2. CSRF token (Double Submit Cookie pattern)
3. Custom header (X-Requested-With) — không bị CSRF tự động gửi

HttpOnly cookie trong project:
- Refresh token trong HttpOnly cookie với SameSite=Strict
- JS không đọc được → XSS không lấy được token
- SameSite → CSRF không thể gửi refresh request
```

---

<a id="q26"></a>
**Q26. Content Security Policy (CSP) trong Angular?**

CSP là HTTP header chỉ định nguồn nào được phép load script, style, image...

```typescript
// ASP.NET Core set CSP header
context.Response.Headers.Append("Content-Security-Policy",
  "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

// Development CSP nới lỏng hơn (Swagger cần unsafe-inline)
// Production CSP chặt, không có 'unsafe-inline'
```

---

<a id="phan-10"></a>
## PHẦN 10: CÂU HỎI THỰC TẾ TỪ PROJECT

---

<a id="q27"></a>
**Q27. APP_INITIALIZER dùng để làm gì trong project?**

```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () => authService.initAuth(),
      deps: [AuthService],
      multi: true, // Có thể có nhiều APP_INITIALIZER
    }
  ]
};
```

`APP_INITIALIZER` chạy **trước khi app bootstrap**. `initAuth()` thử refresh token từ HttpOnly cookie:
- Thành công → user tự động đăng nhập (không cần F5 → login lại)
- Thất bại → không sao, user sẽ thấy trang login khi vào protected route

---

<a id="q28"></a>
**Q28. Tại sao `roleMatchGuard` trả về `true` khi user chưa đăng nhập?**

```typescript
export const roleMatchGuard = (requiredRole: string): CanMatchFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) return true; // ← Tại sao true?
    // ...
  };
```

Vì `canMatch` và `canActivate` chạy **cùng route**, theo thứ tự:
1. `canMatch` chạy trước
2. Nếu `canMatch` trả `true` → route matched → `canActivate` chạy tiếp
3. `authGuard` trong `canActivate` xử lý redirect về `/login` với `returnUrl`

Nếu `canMatch` trả về UrlTree(`/forbidden`) cho user chưa đăng nhập → sai UX (nên về login, không phải forbidden).

---

<a id="q29"></a>
**Q29. `computed()` signal có thể gọi trong template Angular không?**

Có. Angular 17 tích hợp signal reactivity với template engine:

```typescript
// Component
totalPages = computed(() => Math.ceil(this.totalCount() / this.PAGE_SIZE));
```

```html
<!-- Template — gọi computed signal như function -->
<span>Trang {{ page() }} / {{ totalPages() }}</span>

<!-- Angular track dependency: khi totalCount() hoặc PAGE_SIZE thay đổi,
     totalPages() được tính lại, template re-render tự động -->
```

---

<a id="q30"></a>
**Q30. Giải thích flow từ lúc user login đến lúc gọi API bảo vệ?**

```
1. User nhập username/password → LoginComponent.onSubmit()
2. AuthService.login() → POST /connect/token (form-urlencoded)
3. Server validate → trả access_token (15 phút) + set HttpOnly cookie (refresh_token)
4. Angular lưu access_token trong TokenService (in-memory)
5. AuthService.fetchUserInfo() → GET /connect/userinfo → lấy name, email, roles
6. AuthService._currentUser.set(userInfo), _isAuthenticated.set(true)
7. Router navigate về returnUrl (hoặc /dashboard)

--- Gọi API ---
8. Component gọi this.http.get('/api/users')
9. AuthInterceptor chạy → lấy access_token từ TokenService
10. Thêm header: Authorization: Bearer <token>
11. Request đến server → UseAuthentication() parse JWT → ClaimsPrincipal
12. UseAuthorization() → [HasPermission("USER","VIEW")] → check DB
13. Trả data hoặc 403

--- Token hết hạn (sau 15 phút) ---
14. Server trả 401 → AuthInterceptor bắt
15. authService.refreshAccessToken() → POST /connect/token (grant_type=refresh_token)
16. Browser tự gửi HttpOnly cookie → Server validate → trả access_token mới
17. Retry request gốc với token mới
```

---

<a id="phan-11"></a>
## PHẦN 11: DIRECTIVES & PIPES

---

<a id="q31"></a>
**Q31. Phân biệt Structural Directive và Attribute Directive? Tự tạo directive như thế nào?**

- **Structural Directive**: Thay đổi cấu trúc DOM (thêm/xóa element) — `*ngIf`, `*ngFor`, `*ngSwitch`
- **Attribute Directive**: Thay đổi appearance/behavior của element hiện tại — `ngClass`, `ngStyle`

```typescript
// Attribute Directive thực tế: Highlight khi hover, tooltip tùy chỉnh
@Directive({
  selector: '[appHighlight]',
  standalone: true,
})
export class HighlightDirective {
  @Input('appHighlight') color = 'yellow'; // Nhận color từ attribute
  @Input() defaultColor = 'transparent';

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  @HostListener('mouseenter')
  onMouseEnter() {
    this.renderer.setStyle(this.el.nativeElement, 'background-color', this.color);
  }

  @HostListener('mouseleave')
  onMouseLeave() {
    this.renderer.setStyle(this.el.nativeElement, 'background-color', this.defaultColor);
  }
}

// Dùng: <td [appHighlight]="'#fef3c7'" defaultColor="white">{{ user.name }}</td>

// Structural Directive thực tế: Hiển thị theo role (giống *ngIf nhưng check role)
@Directive({
  selector: '[hasRole]',
  standalone: true,
})
export class HasRoleDirective {
  constructor(
    private templateRef: TemplateRef<unknown>,
    private viewContainer: ViewContainerRef,
    private authService: AuthService,
  ) {}

  @Input() set hasRole(requiredRole: string) {
    if (this.authService.hasRole(requiredRole)) {
      this.viewContainer.createEmbeddedView(this.templateRef); // Thêm vào DOM
    } else {
      this.viewContainer.clear(); // Xóa khỏi DOM
    }
  }
}

// Dùng: <button *hasRole="'Admin'">Xóa user</button>
// → Button không render (không chỉ hidden) nếu không có role Admin
```

---

<a id="q32"></a>
**Q32. Pure Pipe vs Impure Pipe — khi nào dùng impure? Use case thực tế?**

```typescript
// PURE Pipe (default) — chỉ tính lại khi input reference thay đổi
// Được cache → hiệu quả
@Pipe({ name: 'formatDate', standalone: true, pure: true })
export class FormatDatePipe implements PipeTransform {
  transform(value: string | Date, format = 'dd/MM/yyyy'): string {
    return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short' })
      .format(new Date(value));
  }
}

// IMPURE Pipe — chạy mỗi change detection cycle
// Dùng khi: pipe phụ thuộc vào state bên ngoài input
@Pipe({ name: 'translate', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  constructor(private i18nService: I18nService) {}

  transform(key: string): string {
    return this.i18nService.get(key); // Phụ thuộc vào ngôn ngữ hiện tại
    // Khi user đổi ngôn ngữ, i18nService thay đổi nhưng key input không đổi
    // → Pure pipe sẽ KHÔNG tính lại → phải dùng impure
  }
}

// Use case impure pipe khác: Filter list theo state bên ngoài
@Pipe({ name: 'filterByRole', standalone: true, pure: false })
export class FilterByRolePipe implements PipeTransform {
  transform(users: UserDto[], filterSignal: Signal<string>): UserDto[] {
    const role = filterSignal(); // Đọc signal — impure vì phụ thuộc signal
    return role ? users.filter(u => u.roles.includes(role)) : users;
  }
}
```

**Rule of thumb**: Mặc định dùng pure. Chỉ impure khi pipe phụ thuộc mutable external state. Impure pipe nặng → cân nhắc computed signal thay thế.

---

<a id="q33"></a>
**Q33. Content Projection (`ng-content`) — component composition pattern?**

```typescript
// Use case: Card component tái sử dụng với nội dung linh hoạt
@Component({
  selector: 'app-card',
  standalone: true,
  template: `
    <div class="card">
      <div class="card-header">
        <ng-content select="[slot=header]" />  <!-- Named slot -->
      </div>
      <div class="card-body">
        <ng-content />                          <!-- Default slot -->
      </div>
      <div class="card-footer" *ngIf="hasFooter">
        <ng-content select="[slot=footer]" />
      </div>
    </div>
  `,
})
export class CardComponent {
  @ContentChild('[slot=footer]') footerContent?: ElementRef;
  get hasFooter() { return !!this.footerContent; }
}

// Sử dụng — parent quyết định nội dung
<app-card>
  <h2 slot="header">Quản lý User</h2>

  <!-- Default slot content -->
  <app-user-list [users]="users" />

  <div slot="footer">
    <button (click)="save()">Lưu thay đổi</button>
  </div>
</app-card>

// Use case thực tế: Modal dialog component tái sử dụng
@Component({
  selector: 'app-modal',
  template: `
    <div class="modal-overlay" (click)="closeOnBackdrop($event)">
      <div class="modal-container">
        <div class="modal-title"><ng-content select="[modal-title]" /></div>
        <div class="modal-body"><ng-content /></div>
        <div class="modal-footer"><ng-content select="[modal-footer]" /></div>
      </div>
    </div>
  `,
})
export class ModalComponent {
  @Output() closed = new EventEmitter<void>();
  closeOnBackdrop(e: MouseEvent) {
    if ((e.target as HTMLElement).classList.contains('modal-overlay'))
      this.closed.emit();
  }
}
```

---

<a id="q34"></a>
**Q34. `ViewChild`, `ContentChild` — phân biệt và use case?**

```typescript
// ViewChild — access element/component trong TEMPLATE của chính component
@Component({
  template: `
    <input #searchInput type="text" />
    <app-chart #chartRef [data]="chartData" />
  `,
})
export class DashboardComponent {
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @ViewChild('chartRef') chart!: ChartComponent;
  @ViewChild(ChartComponent) chart2!: ChartComponent; // By type

  ngAfterViewInit() {
    // ViewChild khả dụng sau ngAfterViewInit (không phải ngOnInit)
    this.searchInput.nativeElement.focus();
    this.chart.refreshData();
  }

  focusSearch() {
    this.searchInput.nativeElement.focus(); // Direct DOM access
  }
}

// ContentChild — access element được PROJECT vào qua ng-content
@Component({
  selector: 'app-tab-group',
  template: `<ng-content />`,
})
export class TabGroupComponent {
  @ContentChildren(TabComponent) tabs!: QueryList<TabComponent>;

  ngAfterContentInit() {
    // ContentChild khả dụng sau ngAfterContentInit
    this.tabs.first?.activate();
  }
}

// Use case: Auto-focus modal input khi mở
@Component({
  selector: 'app-user-form-modal',
  template: `<input #firstInput formControlName="email" />`,
})
export class UserFormModalComponent {
  @ViewChild('firstInput') firstInput!: ElementRef;

  @Input() set visible(val: boolean) {
    if (val) {
      // Delay để DOM render trước khi focus
      setTimeout(() => this.firstInput?.nativeElement.focus(), 50);
    }
  }
}
```

---

<a id="phan-12"></a>
## PHẦN 12: DEPENDENCY INJECTION NÂNG CAO

---

<a id="q35"></a>
**Q35. `InjectionToken` — khi nào dùng thay vì class/interface?**

```typescript
// Vấn đề: Không thể inject primitive values hoặc interface (TypeScript interface bị erase)
// InjectionToken là typed token để inject bất kỳ value nào

// Use case 1: Inject configuration object
export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export interface AppConfig {
  apiBaseUrl: string;
  maxUploadSize: number;
  features: { enableAnalytics: boolean };
}

// Đăng ký
providers: [
  {
    provide: APP_CONFIG,
    useValue: {
      apiBaseUrl: environment.apiUrl,
      maxUploadSize: 10 * 1024 * 1024, // 10MB
      features: { enableAnalytics: !environment.development }
    }
  }
]

// Inject
@Injectable({ providedIn: 'root' })
export class UploadService {
  constructor(@Inject(APP_CONFIG) private config: AppConfig) {}

  upload(file: File) {
    if (file.size > this.config.maxUploadSize) {
      throw new Error(`File vượt quá ${this.config.maxUploadSize / 1024 / 1024}MB`);
    }
  }
}

// Use case 2: Feature flags
export const FEATURE_FLAGS = new InjectionToken<FeatureFlags>('FEATURE_FLAGS', {
  providedIn: 'root',
  factory: () => ({
    enableExport: true,
    enableBulkDelete: false,
  })
});

// Use case 3: Inject Window/Document (testing-friendly)
export const WINDOW = new InjectionToken<Window>('WINDOW', {
  providedIn: 'root',
  factory: () => window,
});

// Dễ mock trong test: { provide: WINDOW, useValue: mockWindow }
```

---

<a id="q36"></a>
**Q36. `@defer` block — Angular 17 lazy rendering? Use case thực tế?**

`@defer` là tính năng Angular 17 cho phép lazy load component/template ngay trong template, không cần code splitting thủ công.

```html
<!-- Use case: Tab content chỉ load khi user click tab -->
<div class="tabs">
  <button (click)="activeTab = 'analytics'">Analytics</button>
  <button (click)="activeTab = 'users'">Users</button>
</div>

<!-- Defer analytics chart — chỉ load khi tab active -->
@defer (when activeTab === 'analytics') {
  <app-analytics-chart [data]="analyticsData" />
} @placeholder {
  <div class="tab-placeholder">Click tab Analytics để xem báo cáo</div>
} @loading (minimum 200ms) {
  <div class="skeleton-chart"></div>
} @error {
  <div class="error">Không thể tải biểu đồ. <button (click)="retry()">Thử lại</button></div>
}

<!-- Defer khi visible trong viewport (intersection observer) -->
@defer (on viewport) {
  <app-comment-section [postId]="post.id" />
} @placeholder {
  <div style="height: 300px"><!-- Giữ scroll position --></div>
}

<!-- Defer khi idle (requestIdleCallback) -->
@defer (on idle) {
  <app-recommendation-panel />
}

<!-- Defer khi hover -->
@defer (on hover(triggerRef)) {
  <app-user-tooltip [userId]="userId" />
} @placeholder {
  <span #triggerRef>{{ userName }}</span>
}
```

**Lợi ích thực tế:**
- Không cần `loadComponent` trong routes — defer ngay trong template
- Bundle chart library (3MB) không load cho user không click Analytics tab
- Cải thiện FCP và LCP

---

<a id="q37"></a>
**Q37. New Control Flow (`@if`, `@for`, `@switch`) — khác gì `*ngIf`, `*ngFor`?**

```html
<!-- Angular 17 new syntax vs old syntax -->

<!-- @if — không cần import CommonModule -->
@if (user.isAdmin) {
  <admin-panel />
} @else if (user.isManager) {
  <manager-panel />
} @else {
  <viewer-panel />
}
<!-- Cũ: <ng-container *ngIf="user.isAdmin; else elseBlock"> -->

<!-- @for — bắt buộc có 'track' (trackBy tích hợp sẵn) -->
@for (user of users(); track user.id) {
  <user-card [user]="user" />
} @empty {
  <p>Không có user nào</p>  <!-- Thay thế *ngIf="users.length === 0" riêng lẻ -->
}
<!-- Cũ: <div *ngFor="let user of users; trackBy: trackById"> -->

<!-- @switch -->
@switch (user.status) {
  @case ('active') { <span class="badge-green">Hoạt động</span> }
  @case ('locked') { <span class="badge-red">Bị khóa</span> }
  @default { <span class="badge-gray">Không xác định</span> }
}

<!-- Lợi ích so với structural directives:
  1. Không cần import CommonModule trong standalone component
  2. TypeScript type narrowing hoạt động trong @if block
  3. @for track là bắt buộc → tránh quên trackBy
  4. @for có biến $index, $first, $last, $even, $odd, $count built-in
-->
@for (item of items(); track item.id; let i = $index, isLast = $last) {
  <li [class.last]="isLast">{{ i + 1 }}. {{ item.name }}</li>
}
```

---

<a id="q38"></a>
**Q38. `effect()` trong Signals — khi nào dùng? Pitfalls cần tránh?**

```typescript
@Component({ ... })
export class UserListComponent {
  searchTerm = signal('');
  users = signal<UserDto[]>([]);

  constructor() {
    // effect() chạy khi signal thay đổi — dùng cho side effects
    // Use case 1: Sync search term với URL query params
    effect(() => {
      const term = this.searchTerm();
      this.router.navigate([], {
        queryParams: { search: term || null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    });

    // Use case 2: Debug — log khi state thay đổi
    effect(() => {
      console.log('[DEBUG] Users updated:', this.users().length);
    });

    // Use case 3: Sync với localStorage
    effect(() => {
      const settings = this.userSettings();
      localStorage.setItem('userSettings', JSON.stringify(settings));
    });
  }

  // ❌ PITFALL 1: Không update signal trong effect (vòng lặp vô hạn)
  badEffect = effect(() => {
    const count = this.count();
    this.count.set(count + 1); // → Lặp vô hạn!
  });

  // ❌ PITFALL 2: Không dùng effect để fetch data (dùng computed + service call)
  // Dùng effect để fetch: không có loading state, khó handle error

  // ✅ ĐÚNG: Fetch data trong ngOnInit hoặc component signal change
  loadUsers() {
    this.userService.getAll(this.page(), this.searchTerm())
      .subscribe(res => {
        this.users.set(res.data);
        this.totalCount.set(res.totalCount);
      });
  }

  // Cleanup trong effect (giống ngOnDestroy cho subscription)
  timerEffect = effect((onCleanup) => {
    const interval = setInterval(() => this.refresh(), 30000);
    onCleanup(() => clearInterval(interval)); // ← Cleanup khi component destroy
  });
}
```

---

<a id="phan-13"></a>
## PHẦN 13: COMPONENT PATTERNS NÂNG CAO

---

<a id="q39"></a>
**Q39. `HostListener` và `HostBinding` — use case thực tế?**

```typescript
// Use case: Directive tự thêm CSS class và xử lý keyboard
@Directive({
  selector: '[appDropzone]',
  standalone: true,
})
export class DropzoneDirective {
  @HostBinding('class.drag-over') isDragOver = false;
  @HostBinding('class.drag-error') hasError = false;
  @HostBinding('attr.aria-dropeffect') ariaDropEffect = 'copy';

  @Output() fileDrop = new EventEmitter<File[]>();

  @HostListener('dragover', ['$event'])
  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = true;
  }

  @HostListener('dragleave')
  onDragLeave() {
    this.isDragOver = false;
  }

  @HostListener('drop', ['$event'])
  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;
    const files = Array.from(event.dataTransfer?.files ?? []);
    this.fileDrop.emit(files);
  }

  // Global event listener — ví dụ click outside để close dropdown
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    // Kiểm tra click có nằm trong element không
  }

  // Keyboard shortcut global
  @HostListener('document:keydown.escape')
  onEscapeKey() { this.isDragOver = false; }
}

// Dùng:
// <div appDropzone (fileDrop)="handleFiles($event)">
//   Kéo thả file vào đây
// </div>
// CSS: .drag-over { border: 2px dashed blue; background: #e0f0ff; }
```

---

<a id="q40"></a>
**Q40. Signal-based `input()` và `output()` — Angular 17.1+?**

```typescript
// Angular 17.1 giới thiệu input() và output() functions
// Thay thế @Input() và @Output() decorator

import { input, output, model } from '@angular/core';

@Component({
  selector: 'app-user-card',
  template: `
    <div [class.selected]="selected()">
      <h3>{{ user().fullName }}</h3>
      <button (click)="onSelect()">Chọn</button>
    </div>
  `,
})
export class UserCardComponent {
  // input() — trả về Signal (readonly)
  user = input.required<UserDto>();       // Required input
  selected = input(false);               // Optional với default value
  highlight = input<string>('yellow');   // Optional với default

  // output() — thay thế @Output + EventEmitter
  selected$ = output<string>();          // Emit userId khi chọn

  // model() — two-way binding (thay thế @Input + @Output kết hợp)
  isExpanded = model(false);             // [isExpanded]="..." (isExpandedChange)="..."

  onSelect() {
    this.selected$.emit(this.user().id);
    this.isExpanded.set(true); // model có thể set
  }
}

// Template dùng input signal:
// {{ user().fullName }} — không phải {{ user.fullName }}
// Lợi ích: input là Signal → computed có thể phụ thuộc vào input
@Component({})
export class ParentComponent {
  selectedUserId = signal<string | null>(null);
}
// <app-user-card [user]="user" (selected$)="selectedUserId.set($event)" />
```

---

<a id="q41"></a>
**Q41. Preloading Strategy trong Angular Router — tối ưu UX?**

```typescript
// Vấn đề: Lazy loading → delay khi user navigate lần đầu
// Preloading: Load lazy chunks ngầm sau khi initial load xong

// Chiến lược 1: PreloadAllModules — load tất cả sau initial
RouterModule.forRoot(routes, {
  preloadingStrategy: PreloadAllModules
})

// Chiến lược 2: Custom — chỉ preload route được đánh dấu
@Injectable({ providedIn: 'root' })
export class SelectivePreloadStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    // Chỉ preload nếu route có data.preload = true
    return route.data?.['preload'] === true ? load() : EMPTY;
  }
}

// Route config
export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard.component')...,
    data: { preload: true }, // ← Sẽ được preload ngay sau initial load
  },
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.routes')...,
    data: { preload: false }, // ← Chỉ load khi navigate (ít dùng)
  }
];

// Đăng ký strategy
providers: [
  provideRouter(routes, withPreloading(SelectivePreloadStrategy))
]

// Chiến lược 3: QuicklinkStrategy (Intersection Observer)
// Preload route khi link xuất hiện trong viewport (như Next.js)
// Cần @ngx-quicklink package
```

---

<a id="q42"></a>
**Q42. State Management không dùng NgRx — khi nào đủ dùng Signal/Service?**

```typescript
// Use case: Shopping cart state — đủ dùng Service + Signal, không cần NgRx

@Injectable({ providedIn: 'root' })
export class CartService {
  // State
  private _items = signal<CartItem[]>([]);

  // Derived state
  readonly items = this._items.asReadonly();
  readonly count = computed(() => this._items().reduce((sum, i) => sum + i.quantity, 0));
  readonly total = computed(() =>
    this._items().reduce((sum, i) => sum + i.price * i.quantity, 0)
  );
  readonly isEmpty = computed(() => this._items().length === 0);

  // Actions
  addItem(product: ProductDto) {
    this._items.update(items => {
      const existing = items.find(i => i.productId === product.id);
      if (existing) {
        return items.map(i => i.productId === product.id
          ? { ...i, quantity: i.quantity + 1 }
          : i);
      }
      return [...items, { productId: product.id, name: product.name, price: product.price, quantity: 1 }];
    });
  }

  removeItem(productId: string) {
    this._items.update(items => items.filter(i => i.productId !== productId));
  }

  clear() { this._items.set([]); }
}

// Dùng trong component — không cần boilerplate NgRx
@Component({})
export class CartComponent {
  cart = inject(CartService); // Inject service
}
// Template: {{ cart.count() }} items — {{ cart.total() | currency:'VND' }}

// KHI NÀO nên dùng NgRx:
// - Nhiều feature cần chia sẻ complex state
// - Cần DevTools time-travel debugging
// - Team lớn, cần enforce unidirectional data flow
// - State phức tạp với nhiều async actions
```

---

<a id="q43"></a>
**Q43. Angular `NgOptimizedImage` — image optimization?**

```typescript
// Angular 15+ — directive tối ưu image loading tự động
import { NgOptimizedImage } from '@angular/common';

@Component({
  imports: [NgOptimizedImage],
  template: `
    <!-- Thay <img src="..."> bằng ngSrc -->
    <img
      ngSrc="/assets/avatar.jpg"
      width="64"
      height="64"
      alt="User avatar"
    />

    <!-- Priority image — preload (above the fold) -->
    <img
      ngSrc="/assets/hero.jpg"
      width="1200"
      height="600"
      priority
      alt="Hero banner"
    />

    <!-- Responsive với fill -->
    <div style="position: relative; width: 100%; height: 300px">
      <img ngSrc="/assets/cover.jpg" fill alt="Cover" />
    </div>
  `,
})

// NgOptimizedImage tự động:
// 1. Lazy loading (loading="lazy") cho non-priority images
// 2. fetchpriority="high" cho priority images
// 3. Cảnh báo nếu thiếu width/height (gây layout shift)
// 4. Cảnh báo nếu image quá lớn so với render size
// 5. srcset generation (nếu dùng image CDN loader)

// Thêm image CDN loader (Cloudinary, Imgix, ImageKit)
providers: [
  provideImageKitLoader('https://ik.imagekit.io/your-id/')
]
// → tự generate: srcset="...?tr=w-640 640w, ...?tr=w-1280 1280w"
```

---

<a id="q44"></a>
**Q44. Standalone component testing — pattern và best practices?**

```typescript
// Pattern: TestBed với minimal setup, mock services bằng jasmine spy
describe('UserListComponent', () => {
  let component: UserListComponent;
  let fixture: ComponentFixture<UserListComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let roleServiceSpy: jasmine.SpyObj<RoleService>;

  const mockUsers: UserDto[] = [
    { id: '1', userName: 'admin', email: 'admin@test.com', isActive: true,
      isLockedOut: false, roles: ['Admin'], createdAt: '2025-01-01', emailConfirmed: true },
  ];

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['getAll', 'delete', 'toggleLock']);
    userServiceSpy.getAll.and.returnValue(of({
      data: mockUsers, totalCount: 1, page: 1, pageSize: 10
    }));

    await TestBed.configureTestingModule({
      imports: [UserListComponent], // Standalone — import trực tiếp
      providers: [
        { provide: UserService, useValue: userServiceSpy },
        { provide: RoleService, useValue: roleServiceSpy },
        provideRouter([]), // Cần router inject
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // Trigger ngOnInit
  });

  it('should display users after load', () => {
    expect(component.users().length).toBe(1);
    expect(component.users()[0].userName).toBe('admin');
    expect(userServiceSpy.getAll).toHaveBeenCalledWith(1, 10, '');
  });

  it('should call delete service and update list', fakeAsync(() => {
    userServiceSpy.delete = jasmine.createSpy().and.returnValue(of(undefined));
    spyOn(window, 'confirm').and.returnValue(true);

    component.confirmDelete(mockUsers[0]);
    tick(); // Resolve observables

    expect(userServiceSpy.delete).toHaveBeenCalledWith('1');
    expect(component.users().length).toBe(0); // Removed from signal
  }));

  it('should NOT delete when user cancels confirm', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    component.confirmDelete(mockUsers[0]);
    expect(userServiceSpy.delete).not.toHaveBeenCalled();
  });

  // Test signal reactivity
  it('should update totalPages when totalCount changes', () => {
    component.totalCount.set(25);
    expect(component.totalPages()).toBe(3); // 25 / 10 = 2.5 → ceil = 3
  });
});
```

---

<a id="q45"></a>
**Q45. Server-side Rendering (SSR) với Angular 17 — khi nào cần? Những vấn đề gì?**

```typescript
// Angular 17 — SSR mặc định khi tạo project với --ssr flag
// ng new my-app --ssr

// app.config.server.ts — cấu hình SSR
export const serverConfig: ApplicationConfig = {
  providers: [provideServerRendering()]
};

// Vấn đề thường gặp với SSR:

// 1. KHÔNG có Window/Document trên server
@Component({})
export class HeroComponent implements OnInit {
  ngOnInit() {
    // ❌ Crash trên server
    // window.localStorage.getItem('token');

    // ✅ Check isPlatformBrowser
    if (isPlatformBrowser(inject(PLATFORM_ID))) {
      const token = localStorage.getItem('token');
    }
  }
}

// 2. HttpClient phải dùng withFetch() để SSR hoạt động
providers: [provideHttpClient(withFetch())]

// 3. Transfer State — tránh double fetch (server fetch + client fetch)
@Component({})
export class ProductListComponent {
  products = signal<Product[]>([]);

  constructor() {
    const transferState = inject(TransferState);
    const PRODUCTS_KEY = makeStateKey<Product[]>('products');

    // Kiểm tra có data từ SSR không
    const cached = transferState.get(PRODUCTS_KEY, null);
    if (cached) {
      this.products.set(cached);
      return; // Không fetch lại
    }

    inject(ProductService).getAll().subscribe(data => {
      this.products.set(data);
      transferState.set(PRODUCTS_KEY, data); // Truyền xuống client
    });
  }
}

// Khi nào cần SSR:
// ✅ SEO quan trọng (blog, e-commerce public)
// ✅ First Contentful Paint chậm
// ✅ Social media preview (og:image, og:title)
// ❌ Không cần: Admin dashboard, nội bộ company (không cần SEO)
```

---

<a id="phan-14"></a>
## PHẦN 14: KIẾN TRÚC FRONTEND (TECHNICAL LEADER)

---

<a id="q46"></a>
**Q46. Micro-Frontend Architecture — khi nào cần? Implement với Module Federation?**

```
Vấn đề Monorepo Frontend lớn:
├── Build time 15 phút cho toàn bộ app
├── 5 team cùng deploy → conflict, bottleneck
├── Team A muốn dùng React, Team B muốn Angular
└── Feature release độc lập không thể nếu chung bundle

Micro-Frontend: Chia app thành các "mini-apps" độc lập

APPROACHES:

1. Iframe — đơn giản nhất, isolation tốt nhất
   Nhược: UX poor (routing, auth, communication khó)

2. Web Components — framework-agnostic
   Nhược: Styling isolation complex

3. Module Federation (Webpack 5) — production-ready
   → Runtime share code giữa apps (Angular, React cùng sống)
   → Recommended cho enterprise

4. Single-SPA — orchestrator framework
```

```typescript
// Shell App (Host) — webpack.config.js
new ModuleFederationPlugin({
  name: 'shell',
  remotes: {
    // Remote apps — load lúc runtime
    'admin': 'admin@http://localhost:4201/remoteEntry.js',
    'orders': 'orders@http://localhost:4202/remoteEntry.js',
  },
  shared: {
    '@angular/core': { singleton: true, strictVersion: true },
    '@angular/common': { singleton: true, strictVersion: true },
    '@angular/router': { singleton: true, strictVersion: true },
  }
})

// Shell routes — lazy load micro-frontend
{
  path: 'admin',
  loadChildren: () => loadRemoteModule({
    type: 'module',
    remoteEntry: 'http://localhost:4201/remoteEntry.js',
    exposedModule: './AdminModule'
  }).then(m => m.AdminModule)
}

// Remote App (Admin) — expose module
new ModuleFederationPlugin({
  name: 'admin',
  filename: 'remoteEntry.js',
  exposes: {
    './AdminModule': './src/app/admin/admin.module.ts',
  },
  shared: { '@angular/core': { singleton: true } }
})
```

```
Khi nào NÊN dùng Micro-Frontend:
✅ 3+ team phát triển độc lập
✅ Deploy cycle khác nhau (Admin monthly, Orders daily)
✅ Muốn migration dần (React → Angular, hoặc ngược lại)

Khi nào KHÔNG nên:
❌ 1-2 team — overhead không đáng
❌ UX cần rất consistent — harder với micro-frontends
❌ Team chưa có kinh nghiệm distributed system
```

---

<a id="q47"></a>
**Q47. Nx Monorepo — tổ chức large-scale Angular project?**

```
PAIN POINTS khi không dùng Monorepo:
├── 5 Angular apps chia sẻ components → copy-paste hoặc npm package riêng
├── Thay đổi shared type → publish package → update tất cả apps → tedious
└── Code reuse khó enforce

NX MONOREPO STRUCTURE:
monorepo/
├── apps/
│   ├── admin-portal/          ← Angular app
│   ├── customer-portal/       ← Angular app
│   └── mobile-app/            ← React Native (cùng mono!)
├── libs/
│   ├── shared/
│   │   ├── ui/                ← Component library (Button, Modal, Table)
│   │   ├── data-access/       ← API services, models
│   │   └── util/              ← Pure functions, pipes, validators
│   ├── admin/
│   │   ├── feature-users/     ← User management feature
│   │   └── feature-roles/     ← Role management feature
│   └── customer/
│       └── feature-orders/
└── nx.json
```

```bash
# Nx commands
nx generate @nx/angular:library shared-ui --directory=libs/shared/ui
nx generate @nx/angular:application new-portal

# Affected — chỉ build/test những gì thay đổi
nx affected:build  # Chỉ build apps bị ảnh hưởng bởi changes
nx affected:test   # Chỉ test libs bị thay đổi
# → Tiết kiệm 80% CI time khi repo lớn

# Dependency graph — visualize
nx graph  # Mở browser, hiện dependency graph

# Enforce boundaries — libs không được import ngược
// nx.json
{
  "projects": {
    "admin-portal": {
      "tags": ["scope:admin", "type:app"]
    },
    "shared-ui": {
      "tags": ["scope:shared", "type:ui"]
    }
  },
  "targetDefaults": {
    "lint": {
      "dependsOn": ["^lint"]
    }
  }
}

// .eslintrc.json — enforce import rules
{
  "rules": {
    "@nx/enforce-module-boundaries": ["error", {
      "depConstraints": [
        // App chỉ import từ libs, không import từ app khác
        { "sourceTag": "type:app", "onlyDependOnLibsWithTags": ["type:feature", "type:ui", "type:data-access", "type:util"] },
        // Feature không import từ feature khác (avoid circular)
        { "sourceTag": "type:feature", "onlyDependOnLibsWithTags": ["type:ui", "type:data-access", "type:util"] },
        // UI lib không depend vào data-access
        { "sourceTag": "type:ui", "onlyDependOnLibsWithTags": ["type:util"] }
      ]
    }]
  }
}
```

---

<a id="q48"></a>
**Q48. Design System — cách xây dựng component library cho enterprise?**

```
Component Library giúp:
├── Consistency UI/UX toàn bộ product
├── Team feature không cần re-implement Button, Modal, Table
├── A11y (accessibility) chỉ cần implement 1 lần
└── Designer ↔ Developer sync qua shared vocabulary

STRUCTURE:
```

```typescript
// Tiered component system:
// LEVEL 1: Primitive (atoms) — Button, Input, Icon, Badge, Spinner
// LEVEL 2: Composite (molecules) — SearchInput, UserAvatar, StatusBadge
// LEVEL 3: Pattern (organisms) — DataTable, FormSection, PageHeader

// Good component API design:
@Component({
  selector: 'ds-button',  // ds = design-system prefix
  standalone: true,
  template: `
    <button
      [type]="type"
      [disabled]="disabled || loading"
      [class]="buttonClass"
      [attr.aria-busy]="loading"
      [attr.aria-label]="ariaLabel || null"
    >
      @if (loading) {
        <ds-spinner size="sm" />
      }
      <ng-content />
    </button>
  `,
})
export class ButtonComponent {
  variant = input<'primary' | 'secondary' | 'danger' | 'ghost'>('primary');
  size = input<'sm' | 'md' | 'lg'>('md');
  type = input<'button' | 'submit' | 'reset'>('button');
  disabled = input(false);
  loading = input(false);
  ariaLabel = input<string>();

  buttonClass = computed(() =>
    `btn btn-${this.variant()} btn-${this.size()} ${this.loading() ? 'btn-loading' : ''}`
  );
}

// Dùng: <ds-button variant="danger" [loading]="isDeleting">Xóa</ds-button>

// Storybook cho documentation + visual testing
// stories/button.stories.ts
export const DangerButton: Story = {
  args: { variant: 'danger', children: 'Delete User' },
};
export const LoadingButton: Story = {
  args: { variant: 'primary', loading: true, children: 'Saving...' },
};
```

---

<a id="q49"></a>
**Q49. Performance Profiling Angular app — cách tìm và fix bottleneck?**

```
TOOLS:

1. Angular DevTools (Chrome extension)
   → Profiler tab: record, xem component render time
   → "Why did this component re-render?" trả lời chính xác

2. Chrome Performance tab
   → Long Tasks (> 50ms) → cần investigate
   → JavaScript execution breakdown

3. Core Web Vitals (thực tế user):
   LCP (Largest Contentful Paint): < 2.5s (load chính)
   FID/INP (Interaction to Next Paint): < 200ms (responsiveness)
   CLS (Cumulative Layout Shift): < 0.1 (layout stability)
```

```typescript
// COMMON BOTTLENECKS VÀ FIX:

// 1. Excessive Change Detection
// Component re-render quá nhiều → OnPush + Signals
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent {
  users = signal<UserDto[]>([]); // Signal-driven, không cần Zone.js scan
}

// 2. Heavy computation trong template (chạy mỗi CD cycle)
// ❌
<td>{{ calculateAge(user.dateOfBirth) }}</td>  // Gọi mỗi lần CD

// ✅ Dùng pipe (pure) hoặc computed signal
<td>{{ user.dateOfBirth | age }}</td>  // Chỉ tính lại khi dateOfBirth thay đổi

// 3. Rendering list lớn (1000+ items)
// ❌ Render tất cả 1000 rows → DOM nặng
*ngFor="let item of thousandItems"

// ✅ Virtual scroll — chỉ render 20 rows visible trong viewport
<cdk-virtual-scroll-viewport itemSize="52" style="height: 500px">
  <table>
    <tr *cdkVirtualFor="let user of users; trackBy: trackById">
      <td>{{ user.name }}</td>
    </tr>
  </table>
</cdk-virtual-scroll-viewport>

// 4. Bundle too large — analyze với source-map-explorer
ng build --source-map
npx source-map-explorer dist/main.js
// → Thấy thư viện nào chiếm nhiều nhất → xem có thể tree-shake không

// 5. Images không optimize → dùng NgOptimizedImage
// 6. HTTP waterfall → route resolver preload data song song

// PROFILING WORKFLOW:
// Measure (Lighthouse) → Profile (DevTools) → Fix → Measure lại
// Không optimize dự đoán — chỉ fix bottleneck đã đo được
```

---

<a id="q50"></a>
**Q50. NgRx ở quy mô lớn — khi nào cần? Alternatives?**

```typescript
// NgRx Feature Store — Scale với nhiều features

// Dùng NgRx khi:
// ✅ State phức tạp, nhiều async action
// ✅ Nhiều component unrelated cùng dùng state
// ✅ Cần time-travel debugging (Redux DevTools)
// ✅ Team lớn, cần enforce unidirectional data flow
// ❌ Simple CRUD app → over-engineering

// Feature State (NgRx Component Store — ít boilerplate hơn)
@Injectable()
export class UserListStore extends ComponentStore<UserListState> {
  constructor(private userService: UserService) {
    super({ users: [], loading: false, error: null, totalCount: 0 });
  }

  // Selectors
  readonly users$ = this.select(s => s.users);
  readonly loading$ = this.select(s => s.loading);
  readonly vm$ = this.select(
    this.users$, this.loading$,
    (users, loading) => ({ users, loading }) // View Model
  );

  // Updaters (sync)
  readonly setLoading = this.updater((state, loading: boolean) =>
    ({ ...state, loading }));

  // Effects (async)
  readonly loadUsers = this.effect((trigger$: Observable<void>) =>
    trigger$.pipe(
      tap(() => this.setLoading(true)),
      switchMap(() => this.userService.getAll().pipe(
        tapResponse(
          users => this.patchState({ users, loading: false }),
          err => this.patchState({ error: err.message, loading: false })
        )
      ))
    )
  );
}

// Component dùng store
@Component({
  providers: [UserListStore], // Scoped to component
})
export class UserListComponent {
  store = inject(UserListStore);
  vm$ = this.store.vm$;

  ngOnInit() { this.store.loadUsers(); }
}
```

---

<a id="q51"></a>
**Q51. Testing Strategy cho Angular — Test Pyramid áp dụng như thế nào?**

```
TEST PYRAMID:
         /\
        /  \  E2E Tests (Playwright)
       /----\  → Happy path flows, 5-10 tests
      /      \
     / Integ  \ Integration Tests (TestBed + real services)
    /----------\ → Feature flows, 20-30 tests
   /            \
  /  Unit Tests  \ → Component logic, services, pipes, guards
 /----------------\ → 100+ tests, chạy < 30 giây

RULE OF THUMB:
Unit: 70% | Integration: 20% | E2E: 10%
```

```typescript
// Unit Test — isolate, mock dependencies
describe('PermissionService', () => {
  it('should return false for action not in permissions', () => {
    const service = new PermissionService();
    service.setPermissions(['USER:VIEW', 'USER:EDIT']);

    expect(service.hasPermission('USER', 'DELETE')).toBeFalse();
    expect(service.hasPermission('USER', 'VIEW')).toBeTrue();
  });
});

// Integration Test — TestBed, mock HTTP
describe('UserListComponent (Integration)', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserListComponent, HttpClientTestingModule],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should show users from API', fakeAsync(() => {
    const httpMock = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(UserListComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/users?page=1&pageSize=10');
    req.flush({ data: [mockUser], totalCount: 1, page: 1, pageSize: 10 });

    tick();
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain(mockUser.email);
  }));
});

// E2E Test với Playwright
test('Admin can create user and user appears in list', async ({ page }) => {
  await page.goto('/auth/login');
  await page.fill('[name=username]', 'admin@demo.com');
  await page.fill('[name=password]', 'Admin@123456');
  await page.click('button[type=submit]');

  await page.waitForURL('/dashboard');
  await page.click('text=Vào trang quản trị Admin');

  await page.click('text=Thêm mới');
  await page.fill('[formcontrolname=email]', 'newuser@test.com');
  await page.fill('[formcontrolname=password]', 'Test@123456');
  await page.click('text=Lưu');

  await expect(page.locator('td:has-text("newuser@test.com")')).toBeVisible();
});
```

---

<a id="q52"></a>
**Q52. CI/CD Pipeline cho Angular — GitHub Actions best practices?**

```yaml
# .github/workflows/angular-ci.yml
name: Angular CI/CD

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  quality-checks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'  # Cache node_modules

      - run: npm ci  # Dùng ci thay install — reproducible, faster

      - name: Lint
        run: npm run lint

      - name: Type Check
        run: npx tsc --noEmit  # Check TypeScript errors không build

      - name: Unit Tests
        run: npm run test -- --watch=false --browsers=ChromeHeadless
          --code-coverage --coverage-reporters=lcov

      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          files: ./coverage/lcov.info

      - name: Build Production
        run: npm run build -- --configuration production

      - name: Bundle Size Check
        run: |
          # Fail nếu main bundle > 500KB (gzipped)
          npx bundlesize
          # bundlesize.config.json: { "files": [{ "path": "dist/main*.js", "maxSize": "500 kB" }] }

  e2e-tests:
    runs-on: ubuntu-latest
    needs: quality-checks
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20', cache: 'npm' }

      - name: Install Playwright browsers
        run: npx playwright install --with-deps chromium

      - name: Start App & API
        run: |
          docker compose up -d --wait
          npm run build -- --configuration production
          npx http-server dist/browser -p 4200 &
          sleep 5

      - name: Run E2E Tests
        run: npx playwright test

      - name: Upload Playwright Report
        if: failure()  # Chỉ upload khi test fail
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: playwright-report/

  deploy-staging:
    needs: [quality-checks, e2e-tests]
    if: github.ref == 'refs/heads/develop'
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Staging (Azure Static Web Apps / S3)
        run: |
          aws s3 sync dist/browser s3://my-app-staging --delete
          aws cloudfront create-invalidation --distribution-id $CF_ID --paths "/*"
```

---

<a id="q53"></a>
**Q53. Accessibility (a11y) — TL cần đảm bảo gì trong Angular app?**

```typescript
// WCAG 2.1 AA compliance — checklist quan trọng:

// 1. Semantic HTML — không dùng div cho mọi thứ
// ❌
<div class="btn" (click)="submit()">Submit</div>
// Không focusable, không Enter-key, screen reader nói "div"

// ✅
<button type="submit">Submit</button>
// Focusable, Enter/Space activate, screen reader nói "Submit, button"

// 2. ARIA labels cho icon-only buttons
<button (click)="delete(user)" aria-label="Xóa user {{ user.email }}">
  <svg>...</svg>  <!-- Icon không có text -->
</button>

// 3. Form labels kết nối với input
<label for="email">Email</label>
<input id="email" type="email" formControlName="email" />
<!-- Không: <input placeholder="Email" /> — placeholder không thay thế label -->

// 4. Error messages announce cho screen reader
<input
  formControlName="email"
  [attr.aria-describedby]="emailError.id"
  [attr.aria-invalid]="emailControl.invalid && emailControl.touched"
/>
<span #emailError id="email-error" role="alert" *ngIf="emailControl.errors && emailControl.touched">
  Email không hợp lệ
</span>
<!-- role="alert" → screen reader đọc ngay khi xuất hiện -->

// 5. Focus management — modal dialog
@Component({ selector: 'app-modal' })
export class ModalComponent implements AfterViewInit {
  @ViewChild('closeButton') closeButton!: ElementRef;
  private previousFocus!: HTMLElement;

  ngAfterViewInit() {
    this.previousFocus = document.activeElement as HTMLElement;
    this.closeButton.nativeElement.focus(); // Focus vào modal khi mở
  }

  close() {
    this.previousFocus?.focus(); // Restore focus khi đóng
  }
}

// 6. Skip navigation link
<a href="#main-content" class="skip-link">Bỏ qua điều hướng</a>
<nav>...</nav>
<main id="main-content">...</main>
// CSS: .skip-link { position: absolute; left: -9999px }
// .skip-link:focus { left: 0 } → Xuất hiện khi Tab

// 7. Automated a11y testing
// ng add @axe-core/angular
// Runs axe-core on each component → báo cáo a11y violations trong browser console
```

---

<a id="q54"></a>
**Q54. Internationalization (i18n) — Angular localization strategy?**

```typescript
// Cách 1: Angular built-in i18n (ngx-translate alternative)
// angular.json configure locales
{
  "i18n": {
    "sourceLocale": "vi",
    "locales": {
      "en-US": "src/locale/messages.en.xlf",
      "ja": "src/locale/messages.ja.xlf"
    }
  },
  "build": {
    "configurations": {
      "production-en": {
        "localize": ["en-US"]
      },
      "production-ja": {
        "localize": ["ja"]
      }
    }
  }
}

// Template markup
<h1 i18n="@@welcome.title">Chào mừng</h1>
<p i18n="@@users.count">{count, plural, =0 {Không có user} =1 {1 user} other {{{count}} users}}</p>

// ng extract-i18n → sinh messages.xlf
// Gửi cho translator → messages.en.xlf, messages.ja.xlf
// Build: ng build --configuration production-en → /en/* bundle

// Cách 2: @ngx-translate/core (runtime, linh hoạt hơn)
// Cho phép đổi ngôn ngữ không cần refresh page

TranslateModule.forRoot({
  loader: {
    provide: TranslateLoader,
    useFactory: (http: HttpClient) =>
      new TranslateHttpLoader(http, '/assets/i18n/', '.json'),
    deps: [HttpClient]
  }
})

// assets/i18n/vi.json
{ "users.title": "Quản lý người dùng", "users.count": "{{count}} người dùng" }

// assets/i18n/en.json
{ "users.title": "User Management", "users.count": "{{count}} users" }

// Template
<h1>{{ 'users.title' | translate }}</h1>
<p>{{ 'users.count' | translate:{ count: users.length } }}</p>

// Component
constructor(private translate: TranslateService) {
  translate.setDefaultLang('vi');
  translate.use(localStorage.getItem('lang') ?? 'vi');
}

switchLanguage(lang: 'vi' | 'en') {
  this.translate.use(lang);
  localStorage.setItem('lang', lang);
}
```

---

<a id="phan-15"></a>
## PHẦN 15: TECHNICAL LEADERSHIP MINDSET

---

<a id="q55"></a>
**Q55. Quyết định chọn thư viện / framework — TL evaluate như thế nào?**

```
EVALUATION CRITERIA (DORA metrics + practical):

1. MATURITY & COMMUNITY
   □ Bao nhiêu GitHub stars? (> 5k là dấu hiệu tốt)
   □ Last commit khi nào? (> 6 tháng không commit → red flag)
   □ Issues open/close ratio? (> 50% open → slow maintenance)
   □ Weekly npm downloads? (trending down → consider alternative)

2. BUNDLE IMPACT
   □ Thêm bao nhiêu KB vào bundle?
   bundlephobia.com → instant size analysis
   □ Tree-shakeable không? (Import cả lib hay chỉ phần dùng)

3. ANGULAR COMPATIBILITY
   □ Hỗ trợ Angular version đang dùng?
   □ Standalone component compatible?
   □ Signal-ready hay vẫn dùng Subject?

4. LONG-TERM RISK
   □ Company behind (Google, Microsoft) hay individual maintainer?
   □ License phù hợp (MIT, Apache 2.0 → OK; GPL → cần review)
   □ Có alternative nếu bị abandon?

5. PRACTICAL TEST
   □ PoC nhỏ trong 2h — cảm giác API có natural không?
   □ Error messages có helpful không?
   □ Documentation có ví dụ thực tế không?

DECISION LOG (ADR):
"Chọn ngx-translate thay vì Angular built-in i18n vì:
 - Runtime language switch không cần reload (UX tốt hơn)
 - Không cần build nhiều bundle (infra đơn giản hơn)
 - Trade-off: bundle thêm 20KB, không auto-optimize translations"
```

---

<a id="q56"></a>
**Q56. Khi developer trong team viết code không theo convention — TL xử lý thế nào?**

```
TIẾP CẬN THEO TÌNH HUỐNG:

SCENARIO 1: Junior dev không biết convention
→ Teach, không blame
→ "Tôi thấy phần này có thể dùng signal thay cho BehaviorSubject để consistent với codebase
   — để tôi show ví dụ, sau này bạn sẽ thấy quen dần"
→ Pair programming 1 buổi hiệu quả hơn 10 comments review

SCENARIO 2: Senior dev cố tình không follow
→ Understand WHY first — có thể họ có lý do chính đáng
→ Nếu không có lý do: "Team đã quyết định dùng approach X vì Y.
   Nếu bạn nghĩ có cách tốt hơn, hãy đề xuất để cả team thảo luận —
   nhưng PR này phải consistent với quyết định hiện tại"
→ 1-on-1, không chỉ trích trước team

SCENARIO 3: Convention không còn phù hợp
→ "Chúng ta đang dùng ngModule approach từ Angular 14.
   Với Angular 17, standalone là default. Team có muốn migrate không?
   Đây là trade-off: ..."
→ Tạo RFC/ADR, vote, commit tập thể

TOOLING THAY VÌ MANUAL ENFORCEMENT:
├── ESLint + @angular-eslint: enforce linting rules tự động
├── Prettier: format code tự động (không cần review style)
├── Husky + lint-staged: lint trước khi commit
└── GitHub Actions: fail CI nếu lint không pass

// .husky/pre-commit
npx lint-staged
// lint-staged.config.js
{ "*.ts": ["eslint --fix", "prettier --write"] }
// → Developer không thể commit code vi phạm convention
```

---

<a id="q57"></a>
**Q57. Upgrade Angular version — strategy cho large codebase?**

```
Angular release cycle: Major version mỗi 6 tháng
LTS support: 18 tháng
→ Nên upgrade ít nhất 1-2 versions/năm để không bị quá cũ

UPGRADE STRATEGY:

1. PRE-UPGRADE CHECKLIST
   □ Đọc CHANGELOG + Migration Guide tại update.angular.io
   □ Check tất cả dependencies hỗ trợ target version
   □ Chạy `ng update` --dry-run để xem changes
   □ Coverage test đủ cao chưa? (< 60% → risky)

2. UPGRADE PROCESS
   # Upgrade từng version một, không skip
   ng update @angular/core@17 @angular/cli@17
   # Fix migration warnings
   # Run tests — nếu pass → commit
   # Tiếp tục upgrade lên 18, 19

3. BREAKING CHANGES MANAGEMENT
   # Angular cung cấp migration schematics tự động
   ng update @angular/core --migrate-only --from=16 --to=17
   # Tự convert *ngIf → @if, *ngFor → @for, v.v.

4. INCREMENTAL MIGRATION (không big-bang)
   # Hybrid: Vừa dùng NgModule vừa dùng standalone
   # Từng feature lib migrate dần → không break toàn bộ app

5. POST-UPGRADE
   □ Run full test suite
   □ Manual smoke test critical flows
   □ Monitor error rate 24h sau deploy
   □ Document issues gặp phải cho team

TOOLS:
angular.io/update — interactive upgrade guide
sheriffs.io — check deprecated APIs
```

---

<a id="q58"></a>
**Q58. Web Performance — Core Web Vitals và cách improve cho Angular SPA?**

```
CORE WEB VITALS (Google ranking factor):

LCP - Largest Contentful Paint (< 2.5s)
→ Thời gian render phần tử lớn nhất trong viewport

FID/INP - Interaction to Next Paint (< 200ms)
→ Responsive khi user interact (click, type)

CLS - Cumulative Layout Shift (< 0.1)
→ Layout không nhảy lung tung khi load

FIXES CHO ANGULAR SPA:
```

```typescript
// 1. LCP — Critical resource load nhanh hơn
// Preconnect: khai báo domain cần kết nối sớm
<link rel="preconnect" href="https://api.myapp.com">
<link rel="preconnect" href="https://fonts.googleapis.com">

// Preload hero image
<link rel="preload" href="/assets/hero.jpg" as="image">
// Hoặc dùng NgOptimizedImage priority attribute

// 2. INP — Angular specific
// Long task → break với scheduler
import { ɵpendingTasks } from '@angular/core';

// Chia heavy computation thành chunks
async function processLargeList(items: Item[]) {
  const CHUNK_SIZE = 100;
  for (let i = 0; i < items.length; i += CHUNK_SIZE) {
    const chunk = items.slice(i, i + CHUNK_SIZE);
    processChunk(chunk);

    // Yield control về browser sau mỗi chunk
    await new Promise(resolve => setTimeout(resolve, 0));
    // → Browser có cơ hội handle user interaction
  }
}

// Zoneless Angular (Angular 18+) → giảm overhead Zone.js
// bootstrapApplication(AppComponent, {
//   providers: [provideExperimentalZonelessChangeDetection()]
// })

// 3. CLS — Đặt kích thước cố định cho dynamic content
// ❌ Image không có width/height → layout shift khi load
<img src="/avatar.jpg" />

// ✅ Explicit size → browser reserve space trước
<img src="/avatar.jpg" width="64" height="64" />
// Hoặc aspect-ratio CSS: img { aspect-ratio: 1 / 1; width: 64px; }

// Skeleton loading — giữ layout ổn định
@defer (when dataLoaded()) {
  <user-profile [user]="user()" />
} @placeholder {
  <user-profile-skeleton />  <!-- Same dimensions as real component -->
}
```

---

<a id="q59"></a>
**Q59. Làm thế nào để làm Technical Roadmap cho team Frontend?**

```
QUARTERLY TECHNICAL ROADMAP FRAMEWORK:

PHÂN TÍCH HIỆN TẠI:
┌─────────────────────────────────────────────┐
│ Metric hiện tại (đo trước khi làm roadmap)  │
├─────────────────┬───────────────────────────┤
│ Build time      │ 8 phút → target: 3 phút   │
│ LCP (prod)      │ 4.2s → target: < 2.5s     │
│ Test coverage   │ 45% → target: 75%          │
│ Angular version │ v16 → target: v18          │
│ Bundle size     │ 1.2MB → target: 600KB      │
└─────────────────┴───────────────────────────┘

Q1 (Foundation):
├── Migrate Angular 16 → 18 (3 sprints)
├── Setup Nx monorepo structure
├── Enforce ESLint rules + Prettier
└── CI pipeline với test coverage gate

Q2 (Performance):
├── Audit và fix Core Web Vitals
│   ├── NgOptimizedImage migration
│   ├── @defer cho heavy components
│   └── Virtual scroll cho danh sách lớn
├── Bundle analysis + tree-shaking
└── OnPush migration cho critical components

Q3 (Developer Experience):
├── Design System — shared component library
├── Storybook documentation
├── E2E test setup (Playwright)
└── Component playground cho design team

Q4 (Advanced):
├── Micro-frontend PoC cho Admin module
├── Signal migration (BehaviorSubject → signal)
├── Zoneless change detection experiment
└── Performance dashboard (automated LCP tracking)

PRESENTING TO STAKEHOLDERS:
→ Không nói "chúng ta cần upgrade Angular" (không có business value)
→ Nói: "Upgrade Angular giúp build time giảm 5 phút mỗi deploy
   × 20 deploys/ngày × 5 developers = 500 phút/ngày dev time tiết kiệm
   = $X mỗi tháng + giảm rủi ro security từ outdated dependencies"
```

---

<a id="phan-16-angular"></a>
## PHẦN 16: DOCKER & DEPLOYMENT ANGULAR (PRODUCTION-GRADE)

---

<a id="q60"></a>
**Q60. Docker deployment Angular SPA — Nginx multi-stage build chuẩn production?**

```dockerfile
# Dockerfile — Angular 17 + Nginx production

# ─── STAGE 1: BUILD ──────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# TRICK: Copy package files trước để tận dụng Docker layer cache
# Nếu chỉ thay đổi code (không thêm package), npm install không chạy lại
COPY package.json package-lock.json ./
RUN npm ci --prefer-offline  # ci = reproducible, nhanh hơn install

COPY . .

# Build với production config
RUN npm run build -- \
    --configuration production \
    --output-path /dist/app \
    --base-href /

# ─── STAGE 2: RUNTIME (Nginx) ────────────────────────────────
FROM nginx:alpine AS runtime

# Xóa default config
RUN rm /etc/nginx/conf.d/default.conf

# Custom Nginx config tối ưu cho Angular SPA
COPY nginx.conf /etc/nginx/conf.d/

# Copy Angular build output từ stage 1
COPY --from=build /dist/app /usr/share/nginx/html

# Security headers script
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost/health || exit 1

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
```

```nginx
# nginx.conf — tối ưu cho Angular SPA
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_types text/plain text/css application/json application/javascript
               text/xml application/xml application/xml+rss text/javascript;
    gzip_min_length 1024;

    # Security headers
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
    # CSP — adjust theo app cụ thể
    add_header Content-Security-Policy
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;"
        always;

    # Health check endpoint (không cần file)
    location /health {
        return 200 'OK';
        add_header Content-Type text/plain;
    }

    # Static assets — cache 1 năm (Angular hash-based filenames)
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    # index.html — không cache (để app update ngay lập tức)
    location = /index.html {
        add_header Cache-Control "no-cache, no-store, must-revalidate";
        add_header Pragma "no-cache";
        add_header Expires "0";
    }

    # Angular routing — fallback về index.html cho SPA routing
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

```bash
# docker-entrypoint.sh — inject runtime config vào Angular
#!/bin/sh
set -e

# Runtime config — Angular đọc từ /assets/config.json
# Không cần rebuild image khi đổi API URL
cat > /usr/share/nginx/html/assets/config.json << EOF
{
  "apiBaseUrl": "${API_BASE_URL:-http://localhost:5000}",
  "authBaseUrl": "${AUTH_BASE_URL:-http://localhost:5000}",
  "clientId": "${CLIENT_ID:-angular-spa}",
  "environment": "${ENVIRONMENT:-production}"
}
EOF

exec "$@"
```

```bash
# Build và run
docker build -t angular-app:latest .
docker run -d \
  -p 4200:80 \
  -e API_BASE_URL=https://api.yourdomain.com \
  -e AUTH_BASE_URL=https://api.yourdomain.com \
  --name angular-app \
  angular-app:latest

# Kiểm tra image size
docker images angular-app
# Build stage (node): ~300MB → Runtime (nginx): ~25MB
```

**So sánh image size:**
```
WITHOUT multi-stage:  Node + Angular dist = ~400MB
WITH multi-stage:     Nginx + dist only   = ~25MB ← 16x nhỏ hơn!
```

---

<a id="q61"></a>
**Q61. Full-stack Docker Compose — Angular + .NET API + SQL Server + Redis?**

```yaml
# docker-compose.yml — full stack local development
# Chạy: docker compose up -d

services:
  # ─── ANGULAR FRONTEND ──────────────────────────────────────────
  angular:
    build:
      context: ./AuthDemo.Angular
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    environment:
      API_BASE_URL: http://localhost:5000
      AUTH_BASE_URL: http://localhost:5000
      CLIENT_ID: angular-spa
      ENVIRONMENT: development
    depends_on:
      api:
        condition: service_healthy
    networks:
      - frontend
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost/health"]
      interval: 30s
      timeout: 5s
      retries: 3

  # ─── .NET API ──────────────────────────────────────────────────
  api:
    build:
      context: ./AuthDemo.Api
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      # Connection strings reference other services by container name
      ConnectionStrings__Default: >-
        Server=sqlserver,1433;
        Database=AuthDemo;
        User=sa;
        Password=${SA_PASSWORD:-YourPassword@123};
        TrustServerCertificate=true;
        Connection Timeout=30;
      Redis__ConnectionString: "redis:6379,password=${REDIS_PASSWORD:-redis@123}"
      # CORS — allow Angular container
      AllowedOrigins__0: "http://localhost:4200"
      AllowedOrigins__1: "http://angular"
    depends_on:
      sqlserver:
        condition: service_healthy
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 10s
      start_period: 60s      # EF migrations chạy khi startup
      retries: 5
    restart: unless-stopped
    networks:
      - frontend
      - backend
    volumes:
      # Dev only: mount source để hot reload với dotnet watch
      - ./AuthDemo.Api:/src:delegated
      - /src/bin                           # Exclude bin từ mount

  # ─── SQL SERVER ────────────────────────────────────────────────
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"                        # Expose để dùng SSMS/Azure Data Studio
    environment:
      SA_PASSWORD: ${SA_PASSWORD:-YourPassword@123}
      ACCEPT_EULA: Y
      MSSQL_PID: Developer
    volumes:
      - sqlserver_data:/var/opt/mssql
      # Seed data script
      - ./scripts/seed-data.sql:/docker-entrypoint-initdb.d/seed.sql
    healthcheck:
      test: >
        /opt/mssql-tools18/bin/sqlcmd
        -S localhost -U sa
        -P "${SA_PASSWORD:-YourPassword@123}"
        -Q "SELECT 1" -C -b
      interval: 15s
      timeout: 10s
      retries: 5
      start_period: 30s
    networks:
      - backend
    deploy:
      resources:
        limits:
          memory: 2G

  # ─── REDIS ─────────────────────────────────────────────────────
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: >
      redis-server
      --requirepass ${REDIS_PASSWORD:-redis@123}
      --appendonly yes
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD:-redis@123}", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - backend

  # ─── MAILPIT (Dev email testing) ────────────────────────────────
  mailpit:
    image: axllent/mailpit:latest
    ports:
      - "8025:8025"    # Web UI xem email
      - "1025:1025"    # SMTP server
    networks:
      - backend

  # ─── REDIS COMMANDER (Dev Redis UI) ─────────────────────────────
  redis-commander:
    image: rediscommander/redis-commander:latest
    environment:
      REDIS_HOSTS: local:redis:6379:0:${REDIS_PASSWORD:-redis@123}
    ports:
      - "8081:8081"
    depends_on:
      - redis
    networks:
      - backend
    profiles:
      - debug             # Chỉ start khi: docker compose --profile debug up

# ─── VOLUMES ───────────────────────────────────────────────────────
volumes:
  sqlserver_data:
  redis_data:

# ─── NETWORKS ──────────────────────────────────────────────────────
networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true        # Backend services không ra internet
```

```bash
# Development workflow:

# 1. Tạo .env file (không commit)
cat > .env << 'EOF'
SA_PASSWORD=YourStrongPassword@123
REDIS_PASSWORD=RedisStrongPassword@123
EOF

# 2. Start toàn bộ stack
docker compose up -d

# 3. Xem logs
docker compose logs -f api        # .NET API logs
docker compose logs -f angular    # Nginx logs

# 4. Chạy EF migrations
docker compose exec api dotnet ef database update

# 5. Seed test data
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrongPassword@123" \
  -i /docker-entrypoint-initdb.d/seed.sql -C

# 6. Stop và xóa volumes khi cần reset
docker compose down -v             # -v xóa cả volumes (data mất!)
docker compose down                # Chỉ stop, data giữ lại

# URLs:
# Angular:       http://localhost:4200
# API Swagger:   http://localhost:5000/swagger
# Email UI:      http://localhost:8025
# Redis UI:      docker compose --profile debug up
```

**GitHub Actions CI — build và test full stack:**

```yaml
# .github/workflows/docker-build.yml
name: Docker Build & Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Build images (parallel)
        run: |
          docker compose build --parallel

      - name: Start stack
        run: |
          docker compose up -d
          # Chờ health checks pass
          docker compose wait sqlserver redis
          sleep 30

      - name: Run .NET tests
        run: |
          docker compose exec -T api dotnet test \
            --filter "Category=Integration" \
            --logger "trx;LogFileName=results.xml"

      - name: Run E2E tests (Playwright)
        run: |
          npm ci
          npx playwright test --reporter=html

      - name: Push to registry (chỉ main branch)
        if: github.ref == 'refs/heads/main'
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
```

---

<a id="q62"></a>
**Q62. Runtime configuration Angular — không hardcode API URL trong build?**

**Vấn đề**: Angular mặc định hardcode `environment.ts` vào bundle lúc build. Muốn deploy cùng 1 image cho staging và production (khác API URL) → phải rebuild → sai nguyên tắc "build once, deploy anywhere".

```typescript
// ❌ CÁCH SAI — hardcode trong build
// src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.production.com',  // ← Hardcode!
};
// Muốn dùng staging phải build lại với URL khác

// ✅ CÁCH ĐÚNG — Runtime config
// Đọc config từ file JSON được inject lúc container start

// 1. src/assets/config.json (placeholder — được overwrite bởi Docker)
{
  "apiBaseUrl": "http://localhost:5000",
  "authBaseUrl": "http://localhost:5000",
  "clientId": "angular-spa"
}

// 2. AppConfig interface
export interface AppConfig {
  apiBaseUrl: string;
  authBaseUrl: string;
  clientId: string;
}

// 3. InjectionToken
export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

// 4. Load config trước khi app bootstrap
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    // Đọc config file TRƯỚC KHI app start
    {
      provide: APP_INITIALIZER,
      useFactory: (http: HttpClient, config: AppConfig) =>
        () => http.get<AppConfig>('/assets/config.json').pipe(
          tap(cfg => {
            // Merge vào token
            Object.assign(config, cfg);
          })
        ).toPromise(),
      deps: [HttpClient, APP_CONFIG],
      multi: true,
    },
    // Provide empty config — sẽ được điền bởi APP_INITIALIZER
    {
      provide: APP_CONFIG,
      useValue: {} as AppConfig,
    }
  ]
};

// 5. Inject và sử dụng
@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl: string;

  constructor(@Inject(APP_CONFIG) config: AppConfig, private http: HttpClient) {
    this.baseUrl = config.apiBaseUrl;
  }

  getUsers() {
    return this.http.get<UserDto[]>(`${this.baseUrl}/api/users`);
  }
}
```

```bash
# Docker Nginx config inject runtime config
# docker-entrypoint.sh — chạy trước nginx start

#!/bin/sh
# Tạo config.json từ environment variables
cat > /usr/share/nginx/html/assets/config.json << EOF
{
  "apiBaseUrl": "${API_BASE_URL:-http://localhost:5000}",
  "authBaseUrl": "${AUTH_BASE_URL:-http://localhost:5000}",
  "clientId": "${CLIENT_ID:-angular-spa}",
  "version": "${APP_VERSION:-local}"
}
EOF

echo "Config generated:"
cat /usr/share/nginx/html/assets/config.json

exec "$@"
```

```yaml
# docker-compose — inject config qua environment variables
angular:
  image: ghcr.io/your-org/angular-app:latest
  environment:
    API_BASE_URL: https://api.staging.yourdomain.com
    AUTH_BASE_URL: https://api.staging.yourdomain.com
    CLIENT_ID: angular-spa
    APP_VERSION: "1.2.3"
  # Cùng image, khác environment variables:
  # Staging:    API_BASE_URL=https://api.staging.yourdomain.com
  # Production: API_BASE_URL=https://api.yourdomain.com
  # → 1 Docker image, deploy được mọi environment
```

---

<a id="q63"></a>
**Q63. Kubernetes deploy Angular SPA — Deployment, ConfigMap, Ingress?**

```yaml
# k8s/angular-configmap.yml — runtime config
apiVersion: v1
kind: ConfigMap
metadata:
  name: angular-config
  namespace: production
data:
  config.json: |
    {
      "apiBaseUrl": "https://api.yourdomain.com",
      "authBaseUrl": "https://api.yourdomain.com",
      "clientId": "angular-spa"
    }
  nginx.conf: |
    server {
      listen 80;
      root /usr/share/nginx/html;
      index index.html;

      gzip on;
      gzip_types text/plain text/css application/json application/javascript;

      location /health { return 200 'OK'; add_header Content-Type text/plain; }

      location ~* \.(js|css|png|jpg|ico|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
      }

      location = /index.html {
        add_header Cache-Control "no-cache";
      }

      location / {
        try_files $uri $uri/ /index.html;
      }
    }

---
# k8s/angular-deployment.yml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: angular-app
  namespace: production
spec:
  replicas: 2              # Nginx stateless → scale dễ
  selector:
    matchLabels:
      app: angular-app
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: angular-app
    spec:
      containers:
        - name: angular
          image: ghcr.io/your-org/angular-app:1.2.3
          ports:
            - containerPort: 80
          resources:
            requests:
              cpu: "50m"      # Nginx nhẹ, không cần nhiều CPU
              memory: "64Mi"
            limits:
              cpu: "200m"
              memory: "128Mi"
          readinessProbe:
            httpGet:
              path: /health
              port: 80
            initialDelaySeconds: 5
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /health
              port: 80
            periodSeconds: 30
          volumeMounts:
            # Mount config từ ConfigMap — dễ update config mà không rebuild
            - name: app-config
              mountPath: /usr/share/nginx/html/assets/config.json
              subPath: config.json
            - name: app-config
              mountPath: /etc/nginx/conf.d/default.conf
              subPath: nginx.conf
      volumes:
        - name: app-config
          configMap:
            name: angular-config

---
# k8s/angular-service.yml
apiVersion: v1
kind: Service
metadata:
  name: angular-service
  namespace: production
spec:
  selector:
    app: angular-app
  ports:
    - port: 80
      targetPort: 80
  type: ClusterIP

---
# k8s/ingress.yml — expose Angular + route /api đến .NET API
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: app-ingress
  namespace: production
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    # Cache static assets tại Nginx Ingress
    nginx.ingress.kubernetes.io/proxy-buffering: "on"
spec:
  ingressClassName: nginx
  tls:
    - hosts:
        - yourdomain.com
      secretName: app-tls-cert
  rules:
    - host: yourdomain.com
      http:
        paths:
          # API requests → .NET service
          - path: /api
            pathType: Prefix
            backend:
              service:
                name: authdemo-api-service
                port:
                  number: 80
          # OpenIddict
          - path: /connect
            pathType: Prefix
            backend:
              service:
                name: authdemo-api-service
                port:
                  number: 80
          # Tất cả còn lại → Angular SPA
          - path: /
            pathType: Prefix
            backend:
              service:
                name: angular-service
                port:
                  number: 80
```

```bash
# Deploy workflow:

# Update config (không cần rebuild image!)
kubectl edit configmap angular-config -n production
# → K8s tự update volume → Nginx reload cấu hình

# Deploy new version
kubectl set image deployment/angular-app angular=ghcr.io/your-org/angular-app:1.2.4 -n production
kubectl rollout status deployment/angular-app -n production

# Rollback
kubectl rollout undo deployment/angular-app -n production
```

---

<a id="phan-17-angular"></a>
## PHẦN 17: ANGULAR 18/19 & ADVANCED SIGNALS

---

<a id="q64"></a>
**Q64. Angular 18/19 — Zoneless Change Detection thực tế?**

Zone.js là thư viện Angular dùng để monkey-patch browser APIs (setTimeout, fetch, event listeners) nhằm tự động trigger change detection. Zoneless = bỏ Zone.js, dùng Signals để drive CD.

```typescript
// Angular 18: Experimental Zoneless (đã stable trong v19)
// main.ts
bootstrapApplication(AppComponent, {
  providers: [
    provideExperimentalZonelessChangeDetection(), // Angular 18
    // Angular 19:
    provideZonelessChangeDetection()              // Stable API
  ]
});

// package.json — xóa zone.js khỏi polyfills
// angular.json: xóa "src/polyfills.ts" → "zone.js" entry
```

```typescript
// Với Zoneless, change detection chỉ chạy khi:
// 1. Signal thay đổi
// 2. markForCheck() gọi thủ công
// 3. async pipe emit

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush, // Vẫn cần OnPush
  template: `
    <!-- Signal trong template — tự trigger CD khi thay đổi -->
    <h1>{{ title() }}</h1>
    <p>Count: {{ count() }}</p>
    <button (click)="increment()">+</button>
  `
})
export class CounterComponent {
  title = signal('Zoneless Demo');
  count = signal(0);

  increment() {
    this.count.update(v => v + 1);
    // Zoneless: không cần Zone.js monitor click event
    // Signal.update() tự trigger CD chỉ cho component này
  }
}

// Vấn đề với Zoneless + legacy code:
// setTimeout, setInterval KHÔNG trigger CD nữa
// ❌
setTimeout(() => {
  this.data = newData; // data không phải signal → không re-render!
}, 1000);

// ✅ Dùng Signal
setTimeout(() => {
  this.data.set(newData); // Signal trigger CD đúng cách
}, 1000);

// ✅ Hoặc dùng markForCheck() với ChangeDetectorRef
setTimeout(() => {
  this.legacyData = newData;
  this.cdr.markForCheck(); // Báo Angular re-render component này
}, 1000);
```

**Lợi ích thực tế của Zoneless:**

```
Performance gains (đo từ Angular team):
├── Initial bundle: -13KB (không include zone.js)
├── Change detection: Chỉ chạy khi cần (signal-driven) vs Zone.js scan toàn cây
├── Frame time: Giảm 30-50% trong heavy-update scenarios
└── Memory: Ít GC pressure hơn (không monkey-patch toàn bộ async APIs)

Khi nào migrate sang Zoneless:
✅ New project — default là Zoneless trong Angular 19
✅ Existing project đã dùng Signal và OnPush nhiều
❌ Còn nhiều third-party lib chưa support Zoneless
❌ Nhiều legacy code dùng imperative mutation (không signal-based)
```

---

<a id="q65"></a>
**Q65. `linkedSignal()` và `resource()` — Angular 19 mới nhất?**

```typescript
// linkedSignal() — Angular 19
// Signal phụ thuộc vào signal khác nhưng VẪN writable
// Dùng khi: state derive từ parent nhưng có thể bị override locally

@Component({})
export class UserFormComponent {
  // Nhận user từ parent
  user = input<UserDto>();

  // linkedSignal: default = user().name, nhưng user có thể tự sửa
  // Khi user input thay đổi → editableName reset về giá trị mới
  editableName = linkedSignal(() => this.user()?.name ?? '');

  // Reset khi user input thay đổi, nhưng vẫn editable:
  editableEmail = linkedSignal({
    source: this.user,          // Trigger reset khi source thay đổi
    computation: (user, prev) => {
      // Nếu user thay đổi HOÀN TOÀN → dùng email mới
      // Nếu chỉ refresh cùng user → giữ giá trị user đang sửa
      if (user?.id !== prev?.previousValue?.id) {
        return user?.email ?? '';
      }
      return prev?.value ?? user?.email ?? '';
    }
  });

  updateName(name: string) {
    this.editableName.set(name); // Writable — user gõ vào form
  }
}

// resource() — Angular 19: async data fetching tích hợp với Signals
// Thay thế pattern: signal + effect + subscription

@Component({
  template: `
    @if (userResource.isLoading()) {
      <div class="skeleton">Loading...</div>
    } @else if (userResource.error()) {
      <div class="error">{{ userResource.error() }}</div>
    } @else {
      <user-profile [user]="userResource.value()" />
    }
  `
})
export class UserDetailComponent {
  userId = input.required<string>();

  // resource() tự reload khi userId thay đổi
  // Không cần effect() + subscription + unsubscribe
  userResource = resource({
    request: () => ({ id: this.userId() }),   // Re-trigger khi userId thay đổi
    loader: async ({ request, abortSignal }) => {
      const response = await fetch(`/api/users/${request.id}`, { signal: abortSignal });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.json() as Promise<UserDto>;
    }
  });

  refresh() {
    this.userResource.reload(); // Force reload
  }
}

// rxResource() — dùng với Observable thay vì fetch
import { rxResource } from '@angular/core/rxjs-interop';

userResource = rxResource({
  request: () => this.userId(),
  loader: ({ request }) => this.userService.getById(request)
  // Observable-based, automatic cleanup
});
```

---

<a id="q66"></a>
**Q66. `toSignal()` và `fromSignal()` — bridge RxJS với Signals?**

```typescript
// toSignal() — convert Observable → Signal
// Dùng khi: có Observable từ service nhưng muốn dùng trong template bằng Signal

@Component({
  template: `
    <!-- Không cần async pipe -->
    <div *ngFor="let user of users(); track user.id">
      {{ user.name }}
    </div>
    <p>Connection: {{ networkStatus() }}</p>
  `
})
export class UserListComponent {
  // Cách cũ với async pipe:
  users$ = this.userService.getAll();  // Observable
  // Template: *ngFor="let user of users$ | async"

  // Signal approach — cleaner template, works with OnPush
  users = toSignal(
    this.userService.getAll(),
    { initialValue: [] as UserDto[] }  // Tránh undefined lúc đầu
  );

  // toSignal với error handling
  networkStatus = toSignal(
    fromEvent(window, 'online').pipe(
      startWith(navigator.onLine),
      map(() => navigator.onLine ? 'Online' : 'Offline')
    ),
    { initialValue: 'Online' }
  );

  // toSignal inject context (phải gọi trong constructor hoặc field init)
  private destroyRef = inject(DestroyRef);

  searchResults = toSignal(
    toObservable(this.searchTerm).pipe(  // Signal → Observable → back to Signal
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term =>
        term ? this.userService.search(term) : of([])
      )
    ),
    { initialValue: [] as UserDto[], injector: inject(Injector) }
  );

  constructor(private userService: UserService) {}
}

// toObservable() — convert Signal → Observable
// Dùng khi: cần RxJS operators (debounceTime, switchMap) cho signal

@Component({})
export class SearchComponent {
  searchTerm = signal('');

  // Signal → Observable để dùng RxJS operators
  searchResults$ = toObservable(this.searchTerm).pipe(
    debounceTime(300),
    distinctUntilChanged(),
    filter(term => term.length >= 2),
    switchMap(term => this.http.get<SearchResult[]>(`/api/search?q=${term}`)),
    catchError(() => of([]))
  );

  // Hoặc back to Signal ngay:
  searchResults = toSignal(
    toObservable(this.searchTerm).pipe(
      debounceTime(300),
      switchMap(term => term ? this.http.get<SearchResult[]>(`/api/search?q=${term}`) : of([]))
    ),
    { initialValue: [] }
  );
}
```

**Best practices khi bridge RxJS ↔ Signals:**

```typescript
// Quy tắc đơn giản:
//
// Dùng Signal khi: state local của component, derived state, simple data
// Dùng Observable khi: cần debounce/throttle, complex operators, streaming
// Bridge (toSignal/toObservable): ranh giới giữa 2 thế giới

// Pattern thực tế trong project:
@Injectable({ providedIn: 'root' })
export class UserService {
  // Service expose Observable (flexible, có thể retry/catchError)
  getAll(params: UserParams): Observable<PagedResult<UserDto>> {
    return this.http.get<PagedResult<UserDto>>('/api/users', { params });
  }
}

// Component dùng toSignal để có reactive template
@Component({})
export class UserListComponent {
  private userService = inject(UserService);

  // Params từ signals
  page = signal(1);
  search = signal('');

  // Combine params → Observable → Signal
  users = toSignal(
    combineLatest([
      toObservable(this.page),
      toObservable(this.search).pipe(debounceTime(300)),
    ]).pipe(
      switchMap(([page, search]) => this.userService.getAll({ page, search }))
    ),
    { initialValue: { data: [], totalCount: 0 } as PagedResult<UserDto> }
  );

  // Template đơn giản: {{ users().totalCount }} — không có async pipe
}
```

---

<a id="phan-18-angular"></a>
## PHẦN 18: MICRO-FRONTEND & MODULE FEDERATION NÂNG CAO

---

<a id="q67"></a>
**Q67. Module Federation runtime sharing — pitfalls thực tế và cách fix?**

```typescript
// Pitfall 1: Version mismatch — 2 apps dùng Angular version khác nhau
// Shell: Angular 17.3, Remote: Angular 18.0
// → Multiple Angular instances chạy cùng lúc → bugs khó hiểu

// webpack.config.js — shell
new ModuleFederationPlugin({
  shared: {
    '@angular/core': {
      singleton: true,
      strictVersion: true,     // ← ERROR nếu version không match
      requiredVersion: '^17.0.0'
    },
    '@angular/common': { singleton: true, strictVersion: false }, // Lỏng hơn cho minor
    '@angular/router': { singleton: true, strictVersion: true },
  }
})

// FIX: Dùng strictVersion: false + requiredVersion: '>=17.0.0' cho minor diffs
// Hoặc: lock tất cả remotes cùng version Angular
```

```typescript
// Pitfall 2: Remote không available → Shell crash
// Lazy load remote fail → blank page, không có error handling

// ❌ Không xử lý lỗi
{
  path: 'admin',
  loadChildren: () => loadRemoteModule({
    type: 'module',
    remoteEntry: 'http://admin-service/remoteEntry.js',
    exposedModule: './AdminRoutes'
  }).then(m => m.adminRoutes)
}

// ✅ Graceful degradation khi remote down
{
  path: 'admin',
  loadChildren: () =>
    loadRemoteModule({
      type: 'module',
      remoteEntry: environment.adminRemoteUrl,
      exposedModule: './AdminRoutes'
    })
    .then(m => m.adminRoutes)
    .catch(err => {
      console.error('Admin remote unavailable:', err);
      // Hiện maintenance page thay vì crash
      return import('./fallback/maintenance.routes').then(m => m.maintenanceRoutes);
    })
}

// Pitfall 3: CSS isolation — style của shell "chảy" vào remote và ngược lại
// Fix: Shadow DOM hoặc CSS naming convention
@Component({
  encapsulation: ViewEncapsulation.ShadowDom,  // True isolation
  // Cons: không inherit global styles
})

// Hoặc: CSS prefix convention
// Shell: .shell-header { ... }
// Admin remote: .admin-sidebar { ... }
// Không dùng generic class names như .btn, .table
```

```typescript
// Pitfall 4: Shared state không sync giữa shell và remotes
// Shell có AuthService, Remote cần biết user info

// ❌ Remote tự inject AuthService → 2 instances khác nhau (nếu không singleton)
// ✅ Communication qua Custom Event hoặc Shared Service trong singleton module

// Shell publish auth state qua CustomEvent
window.dispatchEvent(new CustomEvent('auth:userChanged', {
  detail: { user: currentUser, token: accessToken }
}));

// Remote listen
window.addEventListener('auth:userChanged', (event: CustomEvent) => {
  const { user, token } = event.detail;
  this.authFacade.setUser(user, token);
});

// Hoặc elegant: Shared singleton service trong shell
// Shell expose AuthService qua shared[] trong Module Federation config
shared: {
  './services/auth.service': {
    singleton: true,
    import: './src/app/core/auth/auth.service'
  }
}
```

---

<a id="q68"></a>
**Q68. State sharing giữa Micro-Frontend apps — các pattern và trade-off?**

```
CHALLENGE: MFE apps độc lập nhưng cần share state
(Auth token, user preferences, shopping cart...)

3 PATTERNS:

1. Shell-owned State  → Shell quản lý, remotes subscribe
2. Shared Singleton Service → Qua Module Federation shared[]
3. URL as State → Router params, query params
```

```typescript
// Pattern 1: Shell-owned State + Custom Events (recommended for most cases)

// shell/src/app/state/shell-state.service.ts
@Injectable({ providedIn: 'root' })
export class ShellStateService {
  private _currentUser = signal<UserDto | null>(null);
  private _cart = signal<CartItem[]>([]);

  readonly currentUser = this._currentUser.asReadonly();
  readonly cart = this._cart.asReadonly();

  setUser(user: UserDto | null) {
    this._currentUser.set(user);
    // Broadcast cho tất cả remotes
    window.dispatchEvent(new CustomEvent('shell:userChanged', {
      detail: user,
      bubbles: true
    }));
  }

  addToCart(item: CartItem) {
    this._cart.update(items => [...items, item]);
    window.dispatchEvent(new CustomEvent('shell:cartUpdated', {
      detail: this._cart(),
      bubbles: true
    }));
  }
}

// remote/src/app/core/shell-bridge.service.ts — trong mỗi remote app
@Injectable({ providedIn: 'root' })
export class ShellBridgeService {
  readonly user = signal<UserDto | null>(null);
  readonly cart = signal<CartItem[]>([]);

  constructor() {
    // Listen events từ shell
    window.addEventListener('shell:userChanged', (e: CustomEvent) => {
      this.user.set(e.detail);
    });

    window.addEventListener('shell:cartUpdated', (e: CustomEvent) => {
      this.cart.set(e.detail);
    });

    // Request current state khi remote load
    window.dispatchEvent(new CustomEvent('shell:requestState'));
  }
}

// Pattern 2: Shared Singleton (via Module Federation)
// webpack.config.js — shell
shared: {
  '@myorg/shared-state': {
    singleton: true,
    eager: true,               // Load ngay, không lazy
    requiredVersion: '^1.0.0'
  }
}

// @myorg/shared-state/index.ts — published npm package
@Injectable({ providedIn: 'root' })
export class SharedStateService {
  // Shared giữa shell và tất cả remotes — 1 instance duy nhất
  readonly authToken = signal<string | null>(null);
  readonly currentUser = signal<UserDto | null>(null);
}

// Pattern 3: URL as State (stateless remotes)
// Remote đọc state từ URL params — không cần share service
{
  path: 'products/:category',  // Category trong URL
  queryParams: { filter: 'sale', sort: 'price' }  // Filter state trong URL
}
// ✅ URL shareable, bookmark-able, refresh-safe
// ❌ Không phù hợp cho sensitive data (auth token, user data)
```

**Khi nào dùng pattern nào:**

```
Custom Events: Auth state, user preferences
  ✅ Loose coupling — remotes không depend vào shell code
  ✅ Works across frameworks (Angular shell + React remote)
  ❌ Manual type safety

Shared Singleton Service: Frequently accessed, type-safe shared state
  ✅ TypeScript types
  ✅ Reactive (Signal/Observable)
  ❌ Tight coupling với shared package version

URL as State: Navigation state, filters, pagination
  ✅ Deep linking, shareable URL
  ✅ Browser back/forward works
  ❌ URL length limit, không cho sensitive data
```

---

## TIPS CHO PHỎNG VẤN ANGULAR

1. **Signals là tương lai**: Angular 17+ ưu tiên signals — nói về zoneless CD là điểm cộng
2. **Standalone by default**: Angular 19 default standalone — biết cả NgModule là lợi thế
3. **Performance mindset**: Luôn nhắc OnPush, trackBy, lazy loading, async pipe
4. **Security**: Biết về XSS, CSRF, CSP trong context Angular
5. **RxJS operators**: `switchMap`, `mergeMap`, `exhaustMap` — giải thích được sự khác nhau
6. **Thực hành**: "Tôi đã implement..." tốt hơn "Tôi biết lý thuyết về..."
7. **canMatch vs canActivate**: Một câu hỏi hay, ít người biết sâu về canMatch
8. **@defer và new control flow**: Tính năng Angular 17 mới — ít người đã dùng thực tế
9. **SSR awareness**: Biết khi nào cần SSR và các pitfalls (window, transfer state)
10. **State management không dùng NgRx**: Signal + Service đủ cho nhiều use case — thể hiện judgment tốt
11. **TL mindset — Architecture**: Micro-frontend, Nx, Module Federation — biết khi nào cần
12. **TL mindset — Team**: Enforce convention bằng tooling, không bằng manual review
13. **TL mindset — Business**: Đo impact bằng số (build time, LCP, developer velocity)
14. **Core Web Vitals**: LCP/INP/CLS — biết cách đo và improve cho Angular SPA
15. **Upgrade strategy**: Không big-bang — incremental, có test coverage, schematics tự động
16. **Docker/Deployment**: Multi-stage build, Nginx config, runtime config injection — thể hiện production experience
17. **Angular 18/19 mới**: Zoneless CD, `resource()`, `linkedSignal()`, `toSignal()` — điểm cộng mạnh
18. **toSignal/toObservable bridge**: Biết cách mix RxJS và Signals — kỹ năng thực tế cao
19. **Runtime config (không hardcode API URL)**: "Build once, deploy anywhere" — thể hiện DevOps mindset
20. **MFE pitfalls**: Version mismatch, CSS isolation, state sharing — kinh nghiệm thực chiến
