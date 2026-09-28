import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { EmployeeProfile } from '../models/employee.model';
import { ApiEnvelope, ApiError } from '../models/api-envelope.model';
import { TokenStorageService } from './token-storage.service';
import { API_BASE_URL } from './api-config';

interface LoginResponseData {
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  employee: EmployeeProfile;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);

  private readonly _profile = signal<EmployeeProfile | null>(null);
  readonly profile = this._profile.asReadonly();
  readonly isAuthenticated = computed(() => this._profile() !== null || this.tokenStorage.accessToken !== null);

  hasPermission(code: string): boolean {
    return this._profile()?.permissions.includes(code) ?? false;
  }

  async login(email: string, password: string): Promise<string | null> {
    try {
      const response = await firstValueFrom(
        this.http.post<ApiEnvelope<LoginResponseData>>(`${API_BASE_URL}/auth/login`, { email, password }),
      );
      if (response.success) {
        this.tokenStorage.saveTokens(response.data.accessToken, response.data.refreshToken);
        this._profile.set(response.data.employee);
        return null;
      }
      return response.error.message;
    } catch (err) {
      const httpError = err instanceof HttpErrorResponse ? (err.error as ApiError | undefined) : undefined;
      return httpError?.error?.message ?? 'Unable to sign in. Please try again.';
    }
  }

  async logout(): Promise<void> {
    const refreshToken = this.tokenStorage.refreshToken;
    if (refreshToken) {
      try {
        await firstValueFrom(this.http.post(`${API_BASE_URL}/auth/logout`, { refreshToken }));
      } catch {
        // best-effort
      }
    }
    this.tokenStorage.clear();
    this._profile.set(null);
  }

  async restoreSession(): Promise<void> {
    if (!this.tokenStorage.accessToken) return;
    try {
      const response = await firstValueFrom(this.http.get<ApiEnvelope<{ type: string; value: string }[]>>(`${API_BASE_URL}/auth/me`));
      if (response.success) {
        const claims = response.data;
        const permissionValues = claims.filter((c) => c.type === 'permission').map((c) => c.value);
        const email = claims.find((c) => c.type.endsWith('emailaddress'))?.value ?? '';
        const name = claims.find((c) => c.type.endsWith('name'))?.value ?? '';
        const role = claims.find((c) => c.type.endsWith('role'))?.value ?? '';
        const [firstName, ...rest] = name.split(' ');
        this._profile.set({
          id: '',
          employeeCode: '',
          firstName: firstName ?? '',
          lastName: rest.join(' '),
          email,
          roleName: role,
          departmentId: '',
          designationId: '',
          locationId: '',
          permissions: permissionValues,
        });
      }
    } catch (err) {
      // Only an actually-rejected session signs the user out. A transient failure (429 rate
      // limit, network blip, 5xx) keeps the tokens so the next request can succeed; genuine
      // 401s are already handled by the auth interceptor's refresh-or-logout flow.
      if (err instanceof HttpErrorResponse && err.status === 401) {
        this.tokenStorage.clear();
      }
    }
  }
}
