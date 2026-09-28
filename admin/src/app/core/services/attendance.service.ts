import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  date: string;
  checkInAt: string | null;
  checkOutAt: string | null;
  status: number;
  workingMinutes: number;
  breakMinutes: number;
  locationId: string;
}

export const ATTENDANCE_STATUS_LABELS = [
  'Present', 'Absent', 'Late', 'Half Day', 'Leave', 'Holiday', 'Week Off', 'WFH', 'Pending Regularization',
];

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);

  async getList(params: { from?: string; to?: string; employeeId?: string; status?: number; page?: number; pageSize?: number }) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<AttendanceRecord[]>>(`${API_BASE_URL}/attendance`, { params: httpParams }),
    );
    if (response.success) {
      return { items: response.data, total: response.meta?.total ?? 0 };
    }
    throw new Error(response.error.message);
  }
}
