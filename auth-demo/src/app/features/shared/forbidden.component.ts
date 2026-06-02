import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="page">
      <div class="card">
        <div class="icon">🚫</div>
        <h1>403 — Không có quyền truy cập</h1>
        <p>Bạn không có quyền thực hiện thao tác này.</p>
        <a routerLink="/dashboard" class="btn">← Về Dashboard</a>
      </div>
    </div>
  `,
  styles: [`
    .page { display:flex; align-items:center; justify-content:center; min-height:100vh; background:#f8fafc; }
    .card { text-align:center; background:white; padding:2.5rem; border-radius:12px; box-shadow:0 4px 20px rgba(0,0,0,.08); max-width:400px; }
    .icon { font-size:3rem; margin-bottom:1rem; }
    h1 { font-size:1.25rem; color:#1e293b; margin-bottom:0.5rem; }
    p { color:#64748b; margin-bottom:1.5rem; }
    .btn { padding:0.6rem 1.25rem; background:#2563eb; color:white; border-radius:6px; text-decoration:none; font-weight:500; }
    .btn:hover { background:#1d4ed8; }
  `],
})
export class ForbiddenComponent {}
