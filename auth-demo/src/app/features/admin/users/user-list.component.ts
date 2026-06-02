import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { RoleService } from '../../../core/services/role.service';
import { UserDto, CreateUserRequest, UpdateUserRequest } from '../../../core/models/user.model';
import { RoleDto } from '../../../core/models/role.model';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="page-header">
      <h2>Quản lý User</h2>
      <button class="btn btn-primary" (click)="openCreateForm()">+ Tạo mới</button>
    </div>

    <div class="toolbar">
      <input class="search-input" type="text" placeholder="Tìm theo tên / email..."
        [(ngModel)]="searchTerm" (keyup.enter)="onSearch()" />
      <button class="btn btn-secondary" (click)="onSearch()">Tìm</button>
    </div>

    <div class="alert-error" *ngIf="error()">{{ error() }}</div>

    <div class="table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th>Tên / Username</th>
            <th>Email</th>
            <th>Roles</th>
            <th>Trạng thái</th>
            <th>Ngày tạo</th>
            <th style="width:200px">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngIf="loading()">
            <td colspan="6" class="empty-cell">Đang tải...</td>
          </tr>
          <tr *ngIf="!loading() && users().length === 0">
            <td colspan="6" class="empty-cell">Không có dữ liệu</td>
          </tr>
          <tr *ngFor="let user of users()" [class.row-inactive]="!user.isActive">
            <td>
              <div class="cell-name">{{ user.fullName || user.userName }}</div>
              <div class="cell-sub">{{ user.userName }}</div>
            </td>
            <td>{{ user.email }}</td>
            <td>
              <span class="badge-role" *ngFor="let r of user.roles">{{ r }}</span>
              <span class="text-muted" *ngIf="user.roles.length === 0">—</span>
            </td>
            <td>
              <span class="status-badge"
                [class.st-active]="user.isActive && !user.isLockedOut"
                [class.st-locked]="user.isLockedOut"
                [class.st-inactive]="!user.isActive && !user.isLockedOut">
                {{ user.isLockedOut ? 'Đã khóa' : (user.isActive ? 'Hoạt động' : 'Vô hiệu') }}
              </span>
            </td>
            <td>{{ user.createdAt | date:'dd/MM/yyyy' }}</td>
            <td>
              <div class="action-btns">
                <button class="btn-icon" title="Sửa" (click)="openEditForm(user)">✏️</button>
                <button class="btn-icon" title="Gán Role" (click)="openRolesDialog(user)">👤</button>
                <button class="btn-icon" [title]="user.isLockedOut ? 'Mở khóa' : 'Khóa'"
                  (click)="toggleLock(user)">{{ user.isLockedOut ? '🔓' : '🔒' }}</button>
                <button class="btn-icon" title="Đổi mật khẩu" (click)="openPasswordDialog(user)">🔑</button>
                <button class="btn-icon btn-del" title="Xóa" (click)="confirmDelete(user)">🗑️</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pagination" *ngIf="totalCount() > 0">
      <button class="btn-page" [disabled]="page() === 1" (click)="changePage(page() - 1)">‹ Trước</button>
      <span class="page-info">Trang {{ page() }} / {{ totalPages() }} &nbsp;|&nbsp; {{ totalCount() }} bản ghi</span>
      <button class="btn-page" [disabled]="page() >= totalPages()" (click)="changePage(page() + 1)">Sau ›</button>
    </div>

    <!-- ─── Modal: Create / Edit User ─── -->
    <div class="modal-overlay" *ngIf="showUserForm()">
      <div class="modal-card">
        <div class="modal-head">
          <h3>{{ editingUser() ? 'Sửa User' : 'Tạo User mới' }}</h3>
          <button class="btn-close" (click)="closeUserForm()">✕</button>
        </div>
        <form [formGroup]="userForm" (ngSubmit)="submitUserForm()">
          <div class="form-grid">
            <div class="form-group" *ngIf="!editingUser()">
              <label>Username *</label>
              <input formControlName="userName" type="text"
                [class.invalid]="hasErr('userName')" />
              <span class="ferr" *ngIf="hasErr('userName')">Bắt buộc, tối thiểu 3 ký tự</span>
            </div>
            <div class="form-group">
              <label>Email *</label>
              <input formControlName="email" type="email"
                [class.invalid]="hasErr('email')" />
              <span class="ferr" *ngIf="hasErr('email')">Email không hợp lệ</span>
            </div>
            <div class="form-group" *ngIf="!editingUser()">
              <label>Mật khẩu *</label>
              <input formControlName="password" type="password"
                [class.invalid]="hasErr('password')" />
              <span class="ferr" *ngIf="hasErr('password')">Tối thiểu 8 ký tự</span>
            </div>
            <div class="form-group">
              <label>Họ và tên</label>
              <input formControlName="fullName" type="text" />
            </div>
            <div class="form-group">
              <label>Họ</label>
              <input formControlName="firstName" type="text" />
            </div>
            <div class="form-group">
              <label>Tên</label>
              <input formControlName="lastName" type="text" />
            </div>
            <div class="form-group">
              <label>Ngày sinh</label>
              <input formControlName="dateOfBirth" type="date" />
            </div>
            <div class="form-group" *ngIf="editingUser()">
              <label>Trạng thái</label>
              <select formControlName="isActive">
                <option [value]="true">Hoạt động</option>
                <option [value]="false">Vô hiệu</option>
              </select>
            </div>
          </div>

          <div class="form-section" *ngIf="!editingUser()">
            <label class="section-label">Roles khởi tạo</label>
            <div class="checks-row">
              <label class="check-label" *ngFor="let role of availableRoles()">
                <input type="checkbox" [checked]="isRoleInForm(role.name)"
                  (change)="toggleFormRole(role.name, $event)" />
                {{ role.name }}
              </label>
            </div>
          </div>

          <div class="alert-error form-err" *ngIf="formError()">{{ formError() }}</div>
          <div class="modal-foot">
            <button type="button" class="btn btn-secondary" (click)="closeUserForm()">Hủy</button>
            <button type="submit" class="btn btn-primary" [disabled]="formSubmitting()">
              {{ formSubmitting() ? 'Đang lưu...' : 'Lưu' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ─── Modal: Assign Roles ─── -->
    <div class="modal-overlay" *ngIf="showRolesDialog()">
      <div class="modal-card modal-sm">
        <div class="modal-head">
          <h3>Gán Role — {{ rolesDialogUser()?.fullName || rolesDialogUser()?.userName }}</h3>
          <button class="btn-close" (click)="closeRolesDialog()">✕</button>
        </div>
        <div class="checks-col">
          <label class="check-label" *ngFor="let role of availableRoles()">
            <input type="checkbox" [checked]="selectedRoles().includes(role.name)"
              (change)="toggleSelectedRole(role.name, $event)" />
            {{ role.name }}
          </label>
        </div>
        <div class="alert-error form-err" *ngIf="rolesError()">{{ rolesError() }}</div>
        <div class="modal-foot">
          <button class="btn btn-secondary" (click)="closeRolesDialog()">Hủy</button>
          <button class="btn btn-primary" (click)="saveRoles()" [disabled]="formSubmitting()">
            {{ formSubmitting() ? 'Đang lưu...' : 'Lưu' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Modal: Change Password ─── -->
    <div class="modal-overlay" *ngIf="showPasswordDialog()">
      <div class="modal-card modal-sm">
        <div class="modal-head">
          <h3>Đổi mật khẩu — {{ passwordUser()?.userName }}</h3>
          <button class="btn-close" (click)="closePasswordDialog()">✕</button>
        </div>
        <form [formGroup]="passwordForm" (ngSubmit)="submitPassword()" class="pwd-form">
          <div class="form-group">
            <label>Mật khẩu mới *</label>
            <input formControlName="newPassword" type="password"
              [class.invalid]="pwdInvalid()" />
            <span class="ferr" *ngIf="pwdInvalid()">Tối thiểu 8 ký tự</span>
          </div>
          <div class="alert-error form-err" *ngIf="formError()">{{ formError() }}</div>
          <div class="modal-foot">
            <button type="button" class="btn btn-secondary" (click)="closePasswordDialog()">Hủy</button>
            <button type="submit" class="btn btn-primary" [disabled]="formSubmitting()">
              {{ formSubmitting() ? 'Đang lưu...' : 'Đổi mật khẩu' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:1rem; }
    .page-header h2 { margin:0; font-size:1.25rem; color:#1e293b; }
    .toolbar { display:flex; gap:0.5rem; margin-bottom:1rem; }
    .search-input { flex:1; max-width:320px; padding:0.45rem 0.75rem; border:1px solid #cbd5e1; border-radius:4px; font-size:0.875rem; }
    .alert-error { padding:0.5rem 0.75rem; background:#fee2e2; color:#991b1b; border-radius:4px; font-size:0.85rem; margin-bottom:1rem; }
    .table-wrap { overflow-x:auto; }
    .data-table { width:100%; border-collapse:collapse; background:white; border-radius:8px; overflow:hidden; box-shadow:0 1px 4px rgba(0,0,0,.08); }
    .data-table th { background:#f1f5f9; padding:0.625rem 0.75rem; text-align:left; font-size:0.8rem; font-weight:600; color:#475569; border-bottom:1px solid #e2e8f0; }
    .data-table td { padding:0.625rem 0.75rem; border-bottom:1px solid #f1f5f9; font-size:0.875rem; vertical-align:middle; }
    .data-table tr:last-child td { border-bottom:none; }
    .data-table tr:hover td { background:#f8fafc; }
    .row-inactive td { opacity:0.6; }
    .empty-cell { text-align:center; color:#94a3b8; padding:1.5rem; }
    .cell-name { font-weight:500; color:#1e293b; }
    .cell-sub { font-size:0.75rem; color:#94a3b8; }
    .text-muted { color:#94a3b8; }
    .badge-role { display:inline-block; padding:0.15rem 0.45rem; background:#dbeafe; color:#1d4ed8; border-radius:999px; font-size:0.7rem; font-weight:600; margin-right:2px; }
    .status-badge { display:inline-block; padding:0.2rem 0.6rem; border-radius:999px; font-size:0.75rem; font-weight:600; }
    .st-active { background:#dcfce7; color:#166534; }
    .st-inactive { background:#f1f5f9; color:#475569; }
    .st-locked { background:#fee2e2; color:#991b1b; }
    .action-btns { display:flex; gap:4px; flex-wrap:wrap; }
    .btn-icon { background:none; border:1px solid #e2e8f0; border-radius:4px; cursor:pointer; padding:3px 6px; font-size:0.9rem; }
    .btn-icon:hover { border-color:#94a3b8; background:#f8fafc; }
    .btn-del { border-color:#fecaca; }
    .btn-del:hover { background:#fee2e2; border-color:#f87171; }
    .pagination { display:flex; align-items:center; justify-content:center; gap:1rem; margin-top:1rem; }
    .btn-page { padding:0.4rem 0.75rem; border:1px solid #cbd5e1; border-radius:4px; background:white; cursor:pointer; font-size:0.85rem; }
    .btn-page:disabled { opacity:0.4; cursor:not-allowed; }
    .page-info { font-size:0.85rem; color:#475569; }
    .modal-overlay { position:fixed; inset:0; background:rgba(0,0,0,.4); display:flex; align-items:center; justify-content:center; z-index:1000; padding:1rem; }
    .modal-card { background:white; border-radius:10px; width:100%; max-width:560px; max-height:90vh; overflow-y:auto; box-shadow:0 20px 60px rgba(0,0,0,.2); }
    .modal-sm { max-width:380px; }
    .modal-head { display:flex; justify-content:space-between; align-items:center; padding:1rem 1.25rem; border-bottom:1px solid #e2e8f0; }
    .modal-head h3 { margin:0; font-size:1rem; color:#1e293b; }
    .btn-close { background:none; border:none; font-size:1.1rem; cursor:pointer; color:#94a3b8; }
    .btn-close:hover { color:#475569; }
    .modal-foot { display:flex; justify-content:flex-end; gap:0.5rem; padding:1rem 1.25rem; border-top:1px solid #e2e8f0; }
    .form-grid { display:grid; grid-template-columns:1fr 1fr; gap:0.75rem; padding:1.25rem 1.25rem 0; }
    .form-section { padding:0.75rem 1.25rem 0; }
    .section-label { display:block; font-size:0.8rem; font-weight:600; color:#475569; margin-bottom:0.4rem; }
    .checks-row { display:flex; flex-wrap:wrap; gap:0.75rem; }
    .checks-col { display:flex; flex-direction:column; gap:0.5rem; padding:1.25rem; }
    .check-label { display:flex; align-items:center; gap:0.5rem; font-size:0.875rem; cursor:pointer; }
    .check-label input { cursor:pointer; }
    .form-group { display:flex; flex-direction:column; gap:4px; }
    .form-group label { font-size:0.8rem; font-weight:600; color:#475569; }
    .form-group input, .form-group select { padding:0.45rem 0.65rem; border:1px solid #cbd5e1; border-radius:4px; font-size:0.875rem; }
    .form-group input:focus, .form-group select:focus { border-color:#3b82f6; outline:none; }
    .form-group input.invalid { border-color:#ef4444; }
    .ferr { font-size:0.75rem; color:#ef4444; }
    .form-err { margin:0 1.25rem 0; }
    .pwd-form { padding:1.25rem; }
    .btn { padding:0.5rem 1.25rem; border:none; border-radius:5px; cursor:pointer; font-size:0.875rem; font-weight:500; }
    .btn-primary { background:#2563eb; color:white; }
    .btn-primary:hover:not(:disabled) { background:#1d4ed8; }
    .btn-primary:disabled { opacity:0.6; cursor:not-allowed; }
    .btn-secondary { background:white; border:1px solid #cbd5e1; color:#475569; }
    .btn-secondary:hover { background:#f8fafc; }
  `],
})
export class UserListComponent implements OnInit {
  users = signal<UserDto[]>([]);
  totalCount = signal(0);
  page = signal(1);
  readonly PAGE_SIZE = 10;
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.PAGE_SIZE)));
  loading = signal(false);
  error = signal<string | null>(null);
  searchTerm = '';

  showUserForm = signal(false);
  editingUser = signal<UserDto | null>(null);
  userForm!: FormGroup;
  formSubmitting = signal(false);
  formError = signal<string | null>(null);
  private formRoles: string[] = [];

  showRolesDialog = signal(false);
  rolesDialogUser = signal<UserDto | null>(null);
  selectedRoles = signal<string[]>([]);
  rolesError = signal<string | null>(null);

  showPasswordDialog = signal(false);
  passwordUser = signal<UserDto | null>(null);
  passwordForm!: FormGroup;

  availableRoles = signal<RoleDto[]>([]);

  constructor(
    private userSvc: UserService,
    private roleSvc: RoleService,
    private fb: FormBuilder,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.roleSvc.getAll().subscribe({ next: r => this.availableRoles.set(r) });
  }

  // ─── Load ──────────────────────────────────────────────────────────────────────

  loadUsers(): void {
    this.loading.set(true);
    this.error.set(null);
    this.userSvc.getAll(this.page(), this.PAGE_SIZE, this.searchTerm).subscribe({
      next: res => {
        this.users.set(res.data);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Lỗi tải danh sách user.');
        this.loading.set(false);
      },
    });
  }

  onSearch(): void {
    this.page.set(1);
    this.loadUsers();
  }

  changePage(p: number): void {
    this.page.set(p);
    this.loadUsers();
  }

  // ─── Create / Edit form ────────────────────────────────────────────────────────

  openCreateForm(): void {
    this.editingUser.set(null);
    this.formRoles = [];
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      fullName: [''],
      firstName: [''],
      lastName: [''],
      dateOfBirth: [''],
    });
    this.formError.set(null);
    this.showUserForm.set(true);
  }

  openEditForm(user: UserDto): void {
    this.editingUser.set(user);
    this.userForm = this.fb.group({
      email: [user.email, [Validators.required, Validators.email]],
      fullName: [user.fullName ?? ''],
      firstName: [user.firstName ?? ''],
      lastName: [user.lastName ?? ''],
      dateOfBirth: [user.dateOfBirth ? user.dateOfBirth.substring(0, 10) : ''],
      isActive: [user.isActive],
    });
    this.formError.set(null);
    this.showUserForm.set(true);
  }

  closeUserForm(): void {
    this.showUserForm.set(false);
    this.editingUser.set(null);
  }

  hasErr(field: string): boolean {
    const c = this.userForm.get(field);
    return !!(c?.invalid && c.touched);
  }

  isRoleInForm(name: string): boolean {
    return this.formRoles.includes(name);
  }

  toggleFormRole(name: string, event: Event): void {
    const chk = (event.target as HTMLInputElement).checked;
    this.formRoles = chk
      ? [...this.formRoles, name]
      : this.formRoles.filter(r => r !== name);
  }

  submitUserForm(): void {
    if (this.userForm.invalid) { this.userForm.markAllAsTouched(); return; }
    this.formSubmitting.set(true);
    this.formError.set(null);

    const editing = this.editingUser();
    if (editing) {
      const req: UpdateUserRequest = { ...this.userForm.value };
      this.userSvc.update(editing.id, req).subscribe({
        next: () => { this.formSubmitting.set(false); this.closeUserForm(); this.loadUsers(); },
        error: err => {
          this.formSubmitting.set(false);
          this.formError.set(err?.error?.errors?.join(', ') ?? 'Lỗi khi lưu.');
        },
      });
    } else {
      const req: CreateUserRequest = { ...this.userForm.value, roles: this.formRoles };
      this.userSvc.create(req).subscribe({
        next: () => { this.formSubmitting.set(false); this.closeUserForm(); this.loadUsers(); },
        error: err => {
          this.formSubmitting.set(false);
          this.formError.set(err?.error?.errors?.join(', ') ?? 'Lỗi khi tạo.');
        },
      });
    }
  }

  // ─── Toggle lock ───────────────────────────────────────────────────────────────

  toggleLock(user: UserDto): void {
    this.userSvc.toggleLock(user.id).subscribe({
      next: res => {
        this.users.update(list =>
          list.map(u => u.id === user.id ? { ...u, isLockedOut: res.isLocked } : u)
        );
      },
      error: () => this.error.set('Lỗi khi thay đổi trạng thái khóa.'),
    });
  }

  // ─── Delete ────────────────────────────────────────────────────────────────────

  confirmDelete(user: UserDto): void {
    if (!confirm(`Xóa user "${user.email}"?`)) return;
    this.userSvc.delete(user.id).subscribe({
      next: () => this.loadUsers(),
      error: () => this.error.set('Lỗi khi xóa user.'),
    });
  }

  // ─── Assign roles dialog ───────────────────────────────────────────────────────

  openRolesDialog(user: UserDto): void {
    this.rolesDialogUser.set(user);
    this.selectedRoles.set([...user.roles]);
    this.rolesError.set(null);
    this.showRolesDialog.set(true);
  }

  closeRolesDialog(): void {
    this.showRolesDialog.set(false);
    this.rolesDialogUser.set(null);
  }

  toggleSelectedRole(name: string, event: Event): void {
    const chk = (event.target as HTMLInputElement).checked;
    this.selectedRoles.update(r => chk ? [...r, name] : r.filter(x => x !== name));
  }

  saveRoles(): void {
    const user = this.rolesDialogUser();
    if (!user) return;
    this.formSubmitting.set(true);
    this.rolesError.set(null);
    this.userSvc.assignRoles(user.id, this.selectedRoles()).subscribe({
      next: () => {
        this.formSubmitting.set(false);
        this.closeRolesDialog();
        this.loadUsers();
      },
      error: err => {
        this.formSubmitting.set(false);
        this.rolesError.set(err?.error?.errors?.join(', ') ?? 'Lỗi khi lưu roles.');
      },
    });
  }

  // ─── Change password dialog ────────────────────────────────────────────────────

  openPasswordDialog(user: UserDto): void {
    this.passwordUser.set(user);
    this.passwordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
    });
    this.formError.set(null);
    this.showPasswordDialog.set(true);
  }

  closePasswordDialog(): void {
    this.showPasswordDialog.set(false);
    this.passwordUser.set(null);
  }

  pwdInvalid(): boolean {
    const c = this.passwordForm.get('newPassword');
    return !!(c?.invalid && c.touched);
  }

  submitPassword(): void {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    const user = this.passwordUser();
    if (!user) return;
    this.formSubmitting.set(true);
    this.formError.set(null);
    this.userSvc.changePassword(user.id, this.passwordForm.value.newPassword).subscribe({
      next: () => { this.formSubmitting.set(false); this.closePasswordDialog(); },
      error: err => {
        this.formSubmitting.set(false);
        this.formError.set(err?.error?.errors?.join(', ') ?? 'Lỗi khi đổi mật khẩu.');
      },
    });
  }
}
