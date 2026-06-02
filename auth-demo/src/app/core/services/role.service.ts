import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RoleDto, RolePermissionDto, PermissionEntry } from '../models/role.model';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly BASE = `${environment.apiUrl}/api/roles`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(this.BASE);
  }

  create(name: string): Observable<RoleDto> {
    return this.http.post<RoleDto>(this.BASE, JSON.stringify(name), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/${id}`);
  }

  getPermissions(id: string): Observable<RolePermissionDto> {
    return this.http.get<RolePermissionDto>(`${this.BASE}/${id}/permissions`);
  }

  savePermissions(id: string, permissions: PermissionEntry[]): Observable<any> {
    return this.http.put(`${this.BASE}/${id}/permissions`, { permissions });
  }
}
