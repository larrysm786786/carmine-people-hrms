import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const OVERTIME_STATUS_LABELS = ['Pending', 'Approved', 'Rejected', 'Cancelled'];

export interface OvertimeRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  hours: number;
  reason: string | null;
  status: number;
  decisionComment: string | null;
}

@Injectable({ providedIn: 'root' })
export class OvertimeService {
  private readonly http = inject(HttpClient);

  async getList(params: { employeeId?: string; status?: number; page?: number; pageSize?: number } = {}) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<OvertimeRequest[]>>(`${API_BASE_URL}/overtime`, { params: httpParams }),
    );
    if (!response.success) throw new Error(response.error.message);
    return { items: response.data, total: response.meta?.total ?? 0 };
  }

  async approve(id: string, comment?: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/overtime/${id}/approve`, { comment }));
    if (!response.success) throw new Error(response.error.message);
  }

  async reject(id: string, comment?: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/overtime/${id}/reject`, { comment }));
    if (!response.success) throw new Error(response.error.message);
  }
}
