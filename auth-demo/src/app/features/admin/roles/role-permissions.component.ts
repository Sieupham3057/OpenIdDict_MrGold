import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { RoleService } from '../../../core/services/role.service';
import {
  RolePermissionDto,
  FunctionPermissionDto,
  ActionPermissionDto,
  PermissionEntry,
} from '../../../core/models/role.model';

@Component({
  selector: 'app-role-permissions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="breadcrumb">
      <a routerLink="/admin/roles">← Vai trò & Quyền</a>
      <span> / Phân quyền: {{ roleData()?.name }}</span>
    </div>

    <div class="page-header">
      <h2>Ma trận quyền — {{ roleData()?.name }}</h2>
      <button class="btn btn-primary" (click)="save()" [disabled]="saving()">
        {{ saving() ? 'Đang lưu...' : '💾 Lưu phân quyền' }}
      </button>
    </div>

    <div class="alert-error" *ngIf="error()">{{ error() }}</div>
    <div class="alert-success" *ngIf="successMsg()">{{ successMsg() }}</div>

    <div class="loading-text" *ngIf="loading()">Đang tải ma trận quyền...</div>

    <div class="matrix-wrap" *ngIf="!loading() && roleData()">
      <table class="matrix-table">
        <thead>
          <tr>
            <th class="func-col">Chức năng / Module</th>
            <th class="action-col" *ngFor="let a of allActions()">
              <div class="action-head">
                <span>{{ a.name }}</span>
                <small>{{ a.id }}</small>
              </div>
            </th>
            <th class="all-col">Chọn tất</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let func of roleData()!.permissions">
            <td class="func-name-cell">
              <strong>{{ func.functionName }}</strong>
              <small class="func-id">{{ func.functionId }}</small>
            </td>
            <td class="perm-cell" *ngFor="let a of allActions()">
              <ng-container *ngIf="getAction(func, a.id) as act; else dash">
                <input type="checkbox" class="perm-chk"
                  [checked]="act.hasPermission"
                  (change)="toggle(func.functionId, a.id, $event)" />
              </ng-container>
              <ng-template #dash><span class="na">—</span></ng-template>
            </td>
            <td class="perm-cell all-col">
              <input type="checkbox" class="perm-chk"
                [checked]="isAllChecked(func)"
                [indeterminate]="isSomeChecked(func)"
                (change)="toggleAll(func, $event)" />
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="save-bar" *ngIf="!loading() && roleData()">
      <button class="btn btn-primary" (click)="save()" [disabled]="saving()">
        {{ saving() ? 'Đang lưu...' : '💾 Lưu phân quyền' }}
      </button>
      <span class="save-hint">{{ countChecked() }} quyền đang được chọn</span>
    </div>
  `,
  styles: [`
    .breadcrumb { margin-bottom:1rem; font-size:0.875rem; color:#64748b; }
    .breadcrumb a { color:#2563eb; text-decoration:none; }
    .breadcrumb a:hover { text-decoration:underline; }
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:1rem; }
    .page-header h2 { margin:0; font-size:1.25rem; color:#1e293b; }
    .alert-error { margin-bottom:1rem; padding:0.5rem 0.75rem; background:#fee2e2; color:#991b1b; border-radius:4px; font-size:0.85rem; }
    .alert-success { margin-bottom:1rem; padding:0.5rem 0.75rem; background:#dcfce7; color:#166534; border-radius:4px; font-size:0.85rem; }
    .loading-text { text-align:center; color:#94a3b8; padding:2rem; }
    .matrix-wrap { overflow-x:auto; background:white; border-radius:8px; box-shadow:0 1px 4px rgba(0,0,0,.08); }
    .matrix-table { width:100%; border-collapse:collapse; }
    .matrix-table th {
      background:#f1f5f9; padding:0.5rem 0.75rem; text-align:center;
      font-size:0.8rem; font-weight:600; color:#475569;
      border-bottom:2px solid #e2e8f0; position:sticky; top:0; z-index:1;
    }
    .func-col { text-align:left !important; min-width:180px; }
    .action-col { min-width:80px; }
    .all-col { min-width:90px; background:#f0f9ff !important; }
    .action-head { display:flex; flex-direction:column; align-items:center; gap:2px; }
    .action-head small { font-size:0.65rem; color:#94a3b8; }
    .matrix-table td { padding:0.5rem 0.75rem; border-bottom:1px solid #f1f5f9; text-align:center; }
    .matrix-table tr:last-child td { border-bottom:none; }
    .matrix-table tr:hover td { background:#f8fafc; }
    .func-name-cell { text-align:left !important; }
    .func-name-cell strong { display:block; font-size:0.875rem; color:#1e293b; }
    .func-id { font-size:0.7rem; color:#94a3b8; }
    .perm-cell { vertical-align:middle; }
    .perm-chk { width:16px; height:16px; cursor:pointer; accent-color:#2563eb; }
    .na { color:#cbd5e1; }
    .save-bar { display:flex; align-items:center; gap:1rem; margin-top:1rem; }
    .save-hint { font-size:0.875rem; color:#64748b; }
    .btn { padding:0.5rem 1.25rem; border:none; border-radius:5px; cursor:pointer; font-size:0.875rem; font-weight:500; }
    .btn-primary { background:#2563eb; color:white; }
    .btn-primary:hover:not(:disabled) { background:#1d4ed8; }
    .btn-primary:disabled { opacity:0.6; cursor:not-allowed; }
  `],
})
export class RolePermissionsComponent implements OnInit {
  roleData = signal<RolePermissionDto | null>(null);
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  successMsg = signal<string | null>(null);

  // All unique action IDs sorted by standard order
  allActions = computed(() => {
    const data = this.roleData();
    if (!data) return [];
    const map = new Map<string, { id: string; name: string }>();
    for (const func of data.permissions) {
      for (const act of func.actions) {
        if (!map.has(act.actionId)) {
          map.set(act.actionId, { id: act.actionId, name: act.actionName });
        }
      }
    }
    const ORDER = ['VIEW', 'CREATE', 'EDIT', 'DELETE', 'EXPORT', 'IMPORT'];
    return [...map.values()].sort((a, b) => {
      const ia = ORDER.indexOf(a.id);
      const ib = ORDER.indexOf(b.id);
      if (ia === -1 && ib === -1) return a.id.localeCompare(b.id);
      if (ia === -1) return 1;
      if (ib === -1) return -1;
      return ia - ib;
    });
  });

  countChecked = computed(() => {
    const data = this.roleData();
    if (!data) return 0;
    return data.permissions.reduce(
      (sum, f) => sum + f.actions.filter(a => a.hasPermission).length, 0
    );
  });

  private roleId = '';

  constructor(private route: ActivatedRoute, private roleSvc: RoleService) {}

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadPermissions();
  }

  loadPermissions(): void {
    this.loading.set(true);
    this.roleSvc.getPermissions(this.roleId).subscribe({
      next: data => { this.roleData.set(data); this.loading.set(false); },
      error: () => { this.error.set('Lỗi tải dữ liệu phân quyền.'); this.loading.set(false); },
    });
  }

  getAction(func: FunctionPermissionDto, actionId: string): ActionPermissionDto | null {
    return func.actions.find(a => a.actionId === actionId) ?? null;
  }

  isAllChecked(func: FunctionPermissionDto): boolean {
    return func.actions.length > 0 && func.actions.every(a => a.hasPermission);
  }

  isSomeChecked(func: FunctionPermissionDto): boolean {
    const n = func.actions.filter(a => a.hasPermission).length;
    return n > 0 && n < func.actions.length;
  }

  toggle(functionId: string, actionId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.roleData.update(data => {
      if (!data) return data;
      return {
        ...data,
        permissions: data.permissions.map(f =>
          f.functionId !== functionId ? f : {
            ...f,
            actions: f.actions.map(a =>
              a.actionId !== actionId ? a : { ...a, hasPermission: checked }
            ),
          }
        ),
      };
    });
  }

  toggleAll(func: FunctionPermissionDto, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.roleData.update(data => {
      if (!data) return data;
      return {
        ...data,
        permissions: data.permissions.map(f =>
          f.functionId !== func.functionId ? f : {
            ...f,
            actions: f.actions.map(a => ({ ...a, hasPermission: checked })),
          }
        ),
      };
    });
  }

  save(): void {
    const data = this.roleData();
    if (!data) return;

    const permissions: PermissionEntry[] = [];
    for (const func of data.permissions) {
      for (const act of func.actions) {
        if (act.hasPermission) {
          permissions.push({ functionId: func.functionId, actionId: act.actionId });
        }
      }
    }

    this.saving.set(true);
    this.error.set(null);
    this.successMsg.set(null);

    this.roleSvc.savePermissions(this.roleId, permissions).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMsg.set(`Đã lưu ${permissions.length} quyền thành công.`);
        setTimeout(() => this.successMsg.set(null), 3000);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Lỗi khi lưu phân quyền.');
      },
    });
  }
}
