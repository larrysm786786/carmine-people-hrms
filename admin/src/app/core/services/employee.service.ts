import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { CreateEmployeeRequest, EmployeeDetail, EmployeeListItem, UpdateEmployeeRequest } from '../models/employee.model';
import { API_BASE_URL } from './api-config';

export interface EmployeeQuery {
  search?: string;
  departmentId?: string;
  locationId?: string;
  status?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);

  async getList(query: EmployeeQuery): Promise<PagedResponse<EmployeeListItem>> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        params = params.set(key, String(value));
      }
    });
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<EmployeeListItem[]>>(`${API_BASE_URL}/employees`, { params }),
    );
    if (response.success) {
      return { items: response.data, page: response.meta?.page ?? 1, pageSize: response.meta?.pageSize ?? 20, total: response.meta?.total ?? 0 };
    }
    throw new Error(response.error.message);
  }

  async getById(id: string): Promise<EmployeeDetail> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<EmployeeDetail>>(`${API_BASE_URL}/employees/${id}`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async create(request: CreateEmployeeRequest): Promise<EmployeeDetail> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<EmployeeDetail>>(`${API_BASE_URL}/employees`, request));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async update(id: string, request: UpdateEmployeeRequest): Promise<EmployeeDetail> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<EmployeeDetail>>(`${API_BASE_URL}/employees/${id}`, request));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async setPassword(id: string, newPassword: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/employees/${id}/password`, { newPassword }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async delete(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/employees/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }
}
