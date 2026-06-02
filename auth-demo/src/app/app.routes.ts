import { Routes } from '@angular/router';
import { authGuard, roleMatchGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard],
  },
  {
    path: 'admin',
    loadChildren: () =>
      import('./features/admin/admin.routes').then(m => m.adminRoutes),
    canActivate: [authGuard],
    canMatch: [roleMatchGuard('Admin')],
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./features/shared/forbidden.component').then(m => m.ForbiddenComponent),
  },
  { path: '**', redirectTo: '/dashboard' },
];
