import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface LeaveApplication {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  daysCount: number;
  reason: string | null;
  status: number;
}

export const LEAVE_STATUS_LABELS = ['Pending', 'Manager Approved', 'Approved', 'Rejected', 'Cancelled'];

export interface LeaveType {
  id: string;
  name: string;
  isPaid: boolean;
  requiresAttachment: boolean;
  defaultAnnualDays: number;
}

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private readonly http = inject(HttpClient);

  async getList(params: { status?: number; employeeId?: string; page?: number; pageSize?: number }) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<LeaveApplication[]>>(`${API_BASE_URL}/leaves`, { params: httpParams }),
    );
    if (response.success) {
      return { items: response.data, total: response.meta?.total ?? 0 };
    }
    throw new Error(response.error.message);
  }

  async approve(id: string, comment?: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/leaves/${id}/approve`, { comment }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async reject(id: string, comment?: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/leaves/${id}/reject`, { comment }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async getTypes(): Promise<LeaveType[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<LeaveType[]>>(`${API_BASE_URL}/leaves/types`));
    return response.success ? response.data : [];
  }

  async createType(payload: Omit<LeaveType, 'id'>): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<LeaveType>>(`${API_BASE_URL}/leaves/types`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async updateType(id: string, payload: Omit<LeaveType, 'id'>): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<LeaveType>>(`${API_BASE_URL}/leaves/types/${id}`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  // Fills in every active employee's missing leave-type balances for a year using each type's
  // default annual days. Safe to call repeatedly — existing balances are never overwritten.
  async allocateDefaultBalances(year?: number): Promise<number> {
    let params = new HttpParams();
    if (year) params = params.set('year', String(year));
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<{ created: number }>>(`${API_BASE_URL}/leaves/balances/allocate-defaults`, null, { params }),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data.created;
  }
}
