export interface UserDto {
  id: string;
  userName: string;
  email: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  isActive: boolean;
  avatarUrl?: string;
  createdAt: string;
  emailConfirmed: boolean;
  isLockedOut: boolean;
  roles: string[];
}

export interface UserListResponse {
  data: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  password: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  roles: string[];
}

export interface UpdateUserRequest {
  email?: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  isActive?: boolean;
}
