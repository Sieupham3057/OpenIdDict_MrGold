export interface RoleDto {
  id: string;
  name: string;
}

export interface ActionPermissionDto {
  actionId: string;
  actionName: string;
  hasPermission: boolean;
}

export interface FunctionPermissionDto {
  functionId: string;
  functionName: string;
  actions: ActionPermissionDto[];
}

export interface RolePermissionDto {
  id: string;
  name: string;
  permissions: FunctionPermissionDto[];
}

export interface PermissionEntry {
  functionId: string;
  actionId: string;
}
