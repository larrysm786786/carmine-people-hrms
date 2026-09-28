import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface DashboardKpis {
  totalEmployees: number;
  presentToday: number;
  absentToday: number;
  onLeaveToday: number;
  lateToday: number;
  wfhToday: number;
}

export interface AttendanceTrendPoint {
  date: string;
  present: number;
  absent: number;
  late: number;
}

export interface DepartmentAttendance {
  departmentName: string;
  present: number;
  total: number;
}

export interface LeaveTrendPoint {
  month: string;
  applications: number;
  daysTaken: number;
}

export interface WorkingHoursPoint {
  date: string;
  averageHours: number;
}

export interface RecentAttendanceRow {
  id: string;
  employeeName: string;
  date: string;
  status: string;
  checkInAt: string | null;
  checkOutAt: string | null;
}

export interface PendingApproval {
  id: string;
  type: string;
  employeeName: string;
  summary: string;
  requestedAt: string;
}

export interface RecentEmployee {
  id: string;
  fullName: string;
  department: string;
  designation: string;
  dateOfJoining: string;
}

export interface RecentAnnouncement {
  id: string;
  title: string;
  publishAt: string;
}

export interface Dashboard {
  kpis: DashboardKpis;
  attendanceTrend: AttendanceTrendPoint[];
  departmentAttendance: DepartmentAttendance[];
  leaveTrend: LeaveTrendPoint[];
  workingHours: WorkingHoursPoint[];
  recentAttendance: RecentAttendanceRow[];
  pendingApprovals: PendingApproval[];
  recentEmployees: RecentEmployee[];
  recentAnnouncements: RecentAnnouncement[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  async get(): Promise<Dashboard> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Dashboard>>(`${API_BASE_URL}/dashboard`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }
}
