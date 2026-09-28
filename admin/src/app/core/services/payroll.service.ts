import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private readonly http = inject(HttpClient);

  async getMonthlySalary(employeeId: string): Promise<number | null> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<{ monthlySalary: number | null }>>(`${API_BASE_URL}/payroll/salary`, {
        params: { employeeId },
      }),
    );
    return response.success ? response.data.monthlySalary : null;
  }

  async setMonthlySalary(employeeId: string, monthlySalary: number): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<boolean>>(`${API_BASE_URL}/payroll/salary`, { employeeId, monthlySalary }),
    );
    if (!response.success) throw new Error(response.error.message);
  }
}
