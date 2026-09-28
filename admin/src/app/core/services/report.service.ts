import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export type ReportType = 'attendance' | 'leave' | 'employee' | 'late' | 'working-hours';

export interface ReportQuery {
  from?: string;
  to?: string;
  departmentId?: string;
  locationId?: string;
  employeeId?: string;
  status?: number;
}

export interface AttendanceReportRow {
  employeeCode: string;
  employeeName: string;
  department: string;
  date: string;
  status: string;
  workingMinutes: number;
  checkInAt: string | null;
  checkOutAt: string | null;
}

export interface LeaveReportRow {
  employeeCode: string;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  daysCount: number;
  status: string;
}

export interface EmployeeReportRow {
  employeeCode: string;
  fullName: string;
  email: string;
  department: string;
  designation: string;
  location: string;
  status: string;
  dateOfJoining: string;
}

export interface LateReportRow {
  employeeCode: string;
  employeeName: string;
  date: string;
  checkInAt: string;
}

export interface WorkingHoursReportRow {
  employeeCode: string;
  employeeName: string;
  date: string;
  hours: number;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);

  private buildParams(query: ReportQuery): HttpParams {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }

  async getReport<T>(type: ReportType, query: ReportQuery): Promise<T[]> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<T[]>>(`${API_BASE_URL}/reports/${type}`, { params: this.buildParams(query) }),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async exportReport(type: ReportType, query: ReportQuery, format: 'csv' | 'xlsx'): Promise<void> {
    const params = this.buildParams(query).set('format', format);
    const blob = await firstValueFrom(
      this.http.get(`${API_BASE_URL}/reports/${type}/export`, { params, responseType: 'blob' }),
    );
    const objectUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = `${type}-report.${format}`;
    link.click();
    URL.revokeObjectURL(objectUrl);
  }
}
