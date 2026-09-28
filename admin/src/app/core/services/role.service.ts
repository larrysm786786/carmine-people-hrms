import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface Permission {
  id: string;
  code: string;
  description: string | null;
}

export interface Role {
  id: string;
  name: string;
  description: string | null;
  permissionCodes: string[];
}

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);

  async getRoles(): Promise<Role[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Role[]>>(`${API_BASE_URL}/roles`));
    return response.success ? response.data : [];
  }

  async getPermissions(): Promise<Permission[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Permission[]>>(`${API_BASE_URL}/roles/permissions`));
    return response.success ? response.data : [];
  }

  async createRole(name: string, description?: string): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Role>>(`${API_BASE_URL}/roles`, { name, description }));
    if (!response.success) throw new Error(response.error.message);
  }

  async setPermissions(roleId: string, permissionIds: string[]): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<Role>>(`${API_BASE_URL}/roles/${roleId}/permissions`, { permissionIds }),
    );
    if (!response.success) throw new Error(response.error.message);
  }
}
