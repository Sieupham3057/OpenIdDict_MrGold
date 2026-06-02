import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      {
        path: 'users',
        loadComponent: () =>
          import('./users/user-list.component').then(m => m.UserListComponent),
      },
      {
        path: 'roles',
        loadComponent: () =>
          import('./roles/role-list.component').then(m => m.RoleListComponent),
      },
      {
        path: 'roles/:id/permissions',
        loadComponent: () =>
          import('./roles/role-permissions.component').then(
            m => m.RolePermissionsComponent
          ),
      },
      { path: '', redirectTo: 'users', pathMatch: 'full' },
    ],
  },
];
