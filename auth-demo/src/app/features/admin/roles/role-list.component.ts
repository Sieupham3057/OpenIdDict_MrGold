import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { RoleService } from '../../../core/services/role.service';
import { RoleDto } from '../../../core/models/role.model';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="page-header">
      <h2>Vai trò & Phân quyền</h2>
      <button class="btn btn-primary" (click)="toggleCreateForm()">+ Tạo Role mới</button>
    </div>

    <div class="create-panel" *ngIf="showCreate()">
      <form [formGroup]="createForm" (ngSubmit)="submitCreate()">
        <div class="inline-form">
          <input formControlName="name" type="text" placeholder="Tên role..."
            [class.invalid]="nameInvalid()" />
          <button type="submit" class="btn btn-primary" [disabled]="creating()">
            {{ creating() ? 'Đang tạo...' : 'Tạo' }}
          </button>
          <button type="button" class="btn btn-secondary" (click)="toggleCreateForm()">Hủy</button>
        </div>
        <span class="ferr" *ngIf="nameInvalid()">Tên role bắt buộc nhập</span>
        <div class="alert-error mt-4" *ngIf="createErr()">{{ createErr() }}</div>
      </form>
    </div>

    <div class="alert-error" *ngIf="error()">{{ error() }}</div>

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th>Tên Role</th>
            <th class="col-id">ID</th>
            <th style="width:180px">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngIf="loading()">
            <td colspan="3" class="empty-cell">Đang tải...</td>
          </tr>
          <tr *ngIf="!loading() && roles().length === 0">
            <td colspan="3" class="empty-cell">Chưa có role nào</td>
          </tr>
          <tr *ngFor="let role of roles()">
            <td><strong>{{ role.name }}</strong></td>
            <td class="text-muted text-sm">{{ role.id }}</td>
            <td>
              <div class="action-btns">
                <a [routerLink]="['/admin/roles', role.id, 'permissions']"
                  class="btn btn-sm btn-outline">🔐 Phân quyền</a>
                <button class="btn btn-sm btn-danger" (click)="confirmDelete(role)">🗑️ Xóa</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:1rem; }
    .page-header h2 { margin:0; font-size:1.25rem; color:#1e293b; }
    .create-panel { background:white; border:1px solid #e2e8f0; border-radius:8px; padding:1rem; margin-bottom:1rem; }
    .inline-form { display:flex; gap:0.5rem; align-items:center; }
    .inline-form input { flex:1; max-width:280px; padding:0.45rem 0.75rem; border:1px solid #cbd5e1; border-radius:4px; font-size:0.875rem; }
    .inline-form input.invalid { border-color:#ef4444; }
    .ferr { display:block; font-size:0.75rem; color:#ef4444; margin-top:4px; }
    .alert-error { padding:0.5rem 0.75rem; background:#fee2e2; color:#991b1b; border-radius:4px; font-size:0.85rem; margin-bottom:1rem; }
    .mt-4 { margin-top:0.5rem; margin-bottom:0; }
    .table-wrap { overflow-x:auto; }
    .data-table { width:100%; border-collapse:collapse; background:white; border-radius:8px; overflow:hidden; box-shadow:0 1px 4px rgba(0,0,0,.08); }
    .data-table th { background:#f1f5f9; padding:0.625rem 0.75rem; text-align:left; font-size:0.8rem; font-weight:600; color:#475569; border-bottom:1px solid #e2e8f0; }
    .data-table td { padding:0.625rem 0.75rem; border-bottom:1px solid #f1f5f9; font-size:0.875rem; vertical-align:middle; }
    .data-table tr:last-child td { border-bottom:none; }
    .data-table tr:hover td { background:#f8fafc; }
    .col-id { width:280px; }
    .empty-cell { text-align:center; color:#94a3b8; padding:1.5rem; }
    .text-muted { color:#94a3b8; }
    .text-sm { font-size:0.75rem; }
    .action-btns { display:flex; gap:0.5rem; align-items:center; flex-wrap:wrap; }
    .btn { padding:0.5rem 1rem; border:none; border-radius:5px; cursor:pointer; font-size:0.875rem; font-weight:500; text-decoration:none; display:inline-flex; align-items:center; gap:4px; }
    .btn-primary { background:#2563eb; color:white; }
    .btn-primary:hover:not(:disabled) { background:#1d4ed8; }
    .btn-primary:disabled { opacity:0.6; cursor:not-allowed; }
    .btn-secondary { background:white; border:1px solid #cbd5e1; color:#475569; }
    .btn-secondary:hover { background:#f8fafc; }
    .btn-sm { padding:0.3rem 0.65rem; font-size:0.8rem; }
    .btn-outline { background:white; border:1px solid #3b82f6; color:#2563eb; }
    .btn-outline:hover { background:#eff6ff; }
    .btn-danger { background:#fee2e2; border:1px solid #fecaca; color:#dc2626; }
    .btn-danger:hover { background:#fecaca; }
  `],
})
export class RoleListComponent implements OnInit {
  roles = signal<RoleDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  showCreate = signal(false);
  createForm!: FormGroup;
  creating = signal(false);
  createErr = signal<string | null>(null);

  constructor(private roleSvc: RoleService, private fb: FormBuilder) {}

  ngOnInit(): void { this.loadRoles(); }

  loadRoles(): void {
    this.loading.set(true);
    this.roleSvc.getAll().subscribe({
      next: r => { this.roles.set(r); this.loading.set(false); },
      error: () => { this.error.set('Lỗi tải danh sách role.'); this.loading.set(false); },
    });
  }

  toggleCreateForm(): void {
    this.showCreate.update(v => !v);
    this.createForm = this.fb.group({ name: ['', Validators.required] });
    this.createErr.set(null);
  }

  nameInvalid(): boolean {
    const c = this.createForm.get('name');
    return !!(c?.invalid && c.touched);
  }

  submitCreate(): void {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.creating.set(true);
    this.createErr.set(null);
    this.roleSvc.create(this.createForm.value.name).subscribe({
      next: () => {
        this.creating.set(false);
        this.showCreate.set(false);
        this.loadRoles();
      },
      error: err => {
        this.creating.set(false);
        this.createErr.set(err?.error?.errors?.join(', ') ?? 'Lỗi khi tạo role.');
      },
    });
  }

  confirmDelete(role: RoleDto): void {
    if (!confirm(`Xóa role "${role.name}"?\nTất cả permissions của role này cũng sẽ bị xóa.`)) return;
    this.roleSvc.delete(role.id).subscribe({
      next: () => this.loadRoles(),
      error: err => this.error.set(err?.error?.message ?? 'Lỗi khi xóa role.'),
    });
  }
}
