import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="dashboard">
      <header class="dash-header">
        <h1>Dashboard</h1>
        <button class="btn-logout" (click)="logout()">Đăng xuất</button>
      </header>

      <div class="user-info" *ngIf="authService.currentUser() as user">
        <h3>Xin chào, {{ user.full_name || user.name }}</h3>
        <p>Email: {{ user.email }}</p>
        <p>Roles: {{ user.roles.join(', ') }}</p>
      </div>

      <div class="admin-nav" *ngIf="authService.hasRole('Admin')">
        <a routerLink="/admin" class="btn btn-admin-link">⚙️ Vào trang quản trị Admin</a>
      </div>

      <div class="actions">
        <button class="btn" (click)="callProtectedApi()">Gọi API bảo vệ (/api/me)</button>
        <button class="btn btn-admin" (click)="callAdminApi()" *ngIf="authService.hasRole('Admin')">
          Gọi API Admin only
        </button>
      </div>

      <pre class="result" *ngIf="apiResult">{{ apiResult | json }}</pre>
    </div>
  `,
  styles: [`
    .dashboard { max-width: 800px; margin: 2rem auto; padding: 0 1rem; }
    .dash-header { display: flex; justify-content: space-between; align-items: center; }
    .user-info { background: #e3f2fd; padding: 1rem; border-radius: 6px; margin: 1rem 0; }
    .actions { display: flex; gap: 1rem; margin: 1rem 0; flex-wrap: wrap; }
    .admin-nav { margin: 1rem 0; }
    .btn-admin-link { display:inline-block; padding: 0.5rem 1rem; background: #0f766e;
                      color: white; border-radius: 4px; text-decoration: none; font-size: 0.875rem; }
    .btn-admin-link:hover { background: #0d6761; }
    .btn { padding: 0.5rem 1rem; border: none; border-radius: 4px; background: #1565c0;
           color: white; cursor: pointer; font-size: 0.875rem; }
    .btn-admin { background: #6a1b9a; }
    .btn-logout { padding: 0.5rem 1rem; border: 1px solid #e53935; border-radius: 4px;
                  background: transparent; color: #e53935; cursor: pointer; }
    .result { background: #f5f5f5; padding: 1rem; border-radius: 4px; overflow: auto; font-size: 0.8rem; }
  `],
})
export class DashboardComponent {
  apiResult: any = null;

  constructor(
    public authService: AuthService,
    private http: HttpClient,
  ) {}

  callProtectedApi(): void {
    // Interceptor tự động đính kèm Bearer token — không cần làm gì thêm
    this.http.get(`${environment.apiUrl}/api/me`).subscribe({
      next: data => (this.apiResult = data),
      error: err => (this.apiResult = { error: err.status }),
    });
  }

  callAdminApi(): void {
    this.http.get(`${environment.apiUrl}/api/admin-only`).subscribe({
      next: data => (this.apiResult = data),
      error: err => (this.apiResult = { error: err.status }),
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
