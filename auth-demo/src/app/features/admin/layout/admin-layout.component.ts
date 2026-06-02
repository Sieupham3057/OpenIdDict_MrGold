import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="admin-shell">
      <aside class="sidebar">
        <div class="sidebar-brand">⚙️ Admin Panel</div>
        <nav>
          <a routerLink="/admin/users" routerLinkActive="active" class="nav-item">
            👤 Quản lý User
          </a>
          <a routerLink="/admin/roles" routerLinkActive="active" class="nav-item">
            🔐 Vai trò & Quyền
          </a>
        </nav>
        <div class="sidebar-footer">
          <a routerLink="/dashboard" class="nav-item nav-back">← Dashboard</a>
          <button (click)="logout()" class="btn-logout">Đăng xuất</button>
        </div>
      </aside>
      <main class="admin-content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .admin-shell { display: flex; height: 100vh; overflow: hidden; }
    .sidebar {
      width: 220px; background: #1e293b; color: #f8fafc;
      display: flex; flex-direction: column; flex-shrink: 0;
    }
    .sidebar-brand {
      padding: 1.25rem 1rem; font-size: 1rem; font-weight: 700;
      border-bottom: 1px solid #334155;
    }
    nav { flex: 1; padding: 0.75rem 0; }
    .nav-item {
      display: block; padding: 0.625rem 1rem; color: #cbd5e1;
      text-decoration: none; font-size: 0.875rem;
      border-radius: 4px; margin: 2px 8px; transition: background 0.15s;
    }
    .nav-item:hover, .nav-item.active { background: #334155; color: white; }
    .sidebar-footer { padding: 0.75rem; border-top: 1px solid #334155; }
    .nav-back { margin-bottom: 0.5rem; }
    .btn-logout {
      width: 100%; padding: 0.5rem; background: transparent;
      border: 1px solid #ef4444; color: #ef4444;
      border-radius: 4px; cursor: pointer; font-size: 0.85rem;
    }
    .btn-logout:hover { background: #ef4444; color: white; }
    .admin-content { flex: 1; overflow-y: auto; padding: 1.5rem; background: #f8fafc; }
  `],
})
export class AdminLayoutComponent {
  constructor(private authService: AuthService) {}
  logout(): void { this.authService.logout(); }
}
