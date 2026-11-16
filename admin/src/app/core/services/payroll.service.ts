import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface PayrollComplianceSettings {
  isPfEnabled: boolean;
  pfEmployeeRatePercent: number;
  pfEmployerRatePercent: number;
  pfWageCeiling: number | null;
  isEsiEnabled: boolean;
  esiEmployeeRatePercent: number;
  esiEmployerRatePercent: number;
  esiEligibilityThreshold: number | null;
  isTdsEnabled: boolean;
}

export interface TdsSlab {
  id: string;
  upToAnnualIncome: number | null;
  ratePercent: number;
  sortOrder: number;
}

export interface ComplianceCalculation {
  pfEmployeeAmount: number;
  pfEmployerAmount: number;
  esiEmployeeAmount: number;
  esiEmployerAmount: number;
  tdsAmount: number;
  totalEmployeeDeductions: number;
}

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

  async getComplianceSettings(): Promise<PayrollComplianceSettings> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<PayrollComplianceSettings>>(`${API_BASE_URL}/payroll/compliance/settings`),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async updateComplianceSettings(settings: PayrollComplianceSettings): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<PayrollComplianceSettings>>(`${API_BASE_URL}/payroll/compliance/settings`, settings),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async getTdsSlabs(): Promise<TdsSlab[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<TdsSlab[]>>(`${API_BASE_URL}/payroll/compliance/tds-slabs`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async addTdsSlab(payload: { upToAnnualIncome: number | null; ratePercent: number; sortOrder: number }): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<TdsSlab>>(`${API_BASE_URL}/payroll/compliance/tds-slabs`, payload),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteTdsSlab(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/payroll/compliance/tds-slabs/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }

  async calculateCompliance(monthlyGrossAmount: number): Promise<ComplianceCalculation> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<ComplianceCalculation>>(`${API_BASE_URL}/payroll/compliance/calculate`, { monthlyGrossAmount }),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }
}
