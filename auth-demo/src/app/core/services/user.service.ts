import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UserDto,
  UserListResponse,
  CreateUserRequest,
  UpdateUserRequest,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly BASE = `${environment.apiUrl}/api/users`;

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 10, search?: string): Observable<UserListResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search?.trim()) params = params.set('search', search.trim());
    return this.http.get<UserListResponse>(this.BASE, { params });
  }

  create(request: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>(this.BASE, request);
  }

  update(id: string, request: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.BASE}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/${id}`);
  }

  changePassword(id: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.BASE}/${id}/change-password`, { newPassword });
  }

  assignRoles(id: string, roles: string[]): Observable<any> {
    return this.http.put(`${this.BASE}/${id}/roles`, { roles });
  }

  toggleLock(id: string): Observable<{ isLocked: boolean; message: string }> {
    return this.http.post<{ isLocked: boolean; message: string }>(
      `${this.BASE}/${id}/toggle-lock`,
      {}
    );
  }
}
