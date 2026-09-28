import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface AttendanceAnomaly {
  employeeId: string;
  employeeName: string;
  reasons: string[];
  severity: number;
}

@Injectable({ providedIn: 'root' })
export class AttendanceAnomalyService {
  private readonly http = inject(HttpClient);

  async getAnomalies(): Promise<AttendanceAnomaly[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<AttendanceAnomaly[]>>(`${API_BASE_URL}/attendance-anomalies`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }
}
