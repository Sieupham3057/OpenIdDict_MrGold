import { Injectable, signal, computed } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError, of } from 'rxjs';
import { tap, catchError, switchMap, map } from 'rxjs/operators';
import { TokenService } from './token.service';
import { TokenResponse, UserInfo } from '../models/auth.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = environment.apiUrl;

  private _isAuthenticated = signal(false);
  private _currentUser = signal<UserInfo | null>(null);
  private _refreshInProgress = false;

  readonly isAuthenticated = computed(() => this._isAuthenticated());
  readonly currentUser = computed(() => this._currentUser());

  constructor(
    private http: HttpClient,
    private router: Router,
    private tokenService: TokenService,
  ) {}

  /**
   * Đăng nhập — ROPC Flow
   * Angular gửi username/password → Backend trả access_token
   * Backend set refresh_token vào HttpOnly cookie (JS không đọc được)
   */
  login(username: string, password: string): Observable<UserInfo> {
    // OAuth2 spec yêu cầu application/x-www-form-urlencoded, KHÔNG phải JSON
    const body = new HttpParams()
      .set('grant_type', 'password')
      .set('username', username)
      .set('password', password)
      .set('client_id', 'angular-spa')
      .set('scope', 'profile email roles');

    return this.http
      .post<TokenResponse>(`${this.API}/connect/token`, body.toString(), {
        headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }),
        // withCredentials: true → trình duyệt nhận và lưu HttpOnly cookie từ server
        withCredentials: true,
      })
      .pipe(
        tap(res => this.tokenService.setAccessToken(res.access_token, res.expires_in)),
        switchMap(() => this.fetchUserInfo()),
        tap(user => {
          this.tokenService.setUserInfo(user);
          this._currentUser.set(user);
          this._isAuthenticated.set(true);
        }),
        catchError(err => throwError(() => this.parseError(err))),
      );
  }

  fetchUserInfo(): Observable<UserInfo> {
    return this.http.get<UserInfo>(`${this.API}/connect/userinfo`);
  }

  /**
   * Refresh access token — dùng HttpOnly cookie chứa refresh token
   * Angular không biết nội dung cookie, trình duyệt tự gửi kèm
   */
  refreshAccessToken(): Observable<TokenResponse> {
    if (this._refreshInProgress) {
      return throwError(() => new Error('Đang refresh'));
    }
    this._refreshInProgress = true;

    const body = new HttpParams()
      .set('grant_type', 'refresh_token')
      .set('client_id', 'angular-spa');

    return this.http
      .post<TokenResponse>(`${this.API}/connect/token`, body.toString(), {
        headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }),
        // withCredentials: true → trình duyệt tự đính kèm HttpOnly refresh token cookie
        withCredentials: true,
      })
      .pipe(
        tap(res => {
          this.tokenService.setAccessToken(res.access_token, res.expires_in);
          this._isAuthenticated.set(true);
          this._refreshInProgress = false;
        }),
        catchError(err => {
          this._refreshInProgress = false;
          this.clearState();
          return throwError(() => err);
        }),
      );
  }

  /**
   * Khởi tạo auth sau khi F5 — gọi trong APP_INITIALIZER
   * Thử refresh token (HttpOnly cookie) → nếu hết hạn thì user chưa đăng nhập
   */
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
        return of(false);
      }),
    );
  }

  logout(): void {
    // Gọi server để revoke token trong OpenIddict DB
    this.http.post(`${this.API}/connect/logout`, {}, { withCredentials: true })
      .pipe(catchError(() => of(null)))
      .subscribe(() => this.clearState());
  }

  hasRole(role: string): boolean {
    return this._currentUser()?.roles?.includes(role) ?? false;
  }

  private clearState(): void {
    this.tokenService.clearAll();
    this._isAuthenticated.set(false);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  private parseError(error: any): string {
    return error?.error?.error_description
      ?? error?.error?.error
      ?? 'Đã xảy ra lỗi. Vui lòng thử lại.';
  }
}
