import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const EXPENSE_STATUS_LABELS = ['Pending', 'Approved', 'Rejected', 'Reimbursed'];

export interface Expense {
  id: string;
  employeeId: string;
  employeeName: string;
  category: string;
  amount: number;
  expenseDate: string;
  description: string | null;
  receiptUrl: string | null;
  status: number;
  decidedByName: string | null;
  decidedAt: string | null;
  decisionComment: string | null;
  reimbursedAt: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);

  async getList(params: { employeeId?: string; status?: number; page?: number; pageSize?: number } = {}) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Expense[]>>(`${API_BASE_URL}/expenses`, { params: httpParams }));
    if (!response.success) throw new Error(response.error.message);
    return { items: response.data, total: response.meta?.total ?? 0 };
  }

  async getTeamPending(params: { page?: number; pageSize?: number } = {}) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) httpParams = httpParams.set(key, String(value));
    });
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<Expense[]>>(`${API_BASE_URL}/expenses/team-pending`, { params: httpParams }),
    );
    if (!response.success) throw new Error(response.error.message);
    return { items: response.data, total: response.meta?.total ?? 0 };
  }

  async approve(id: string, comment?: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/expenses/${id}/approve`, { comment }));
    if (!response.success) throw new Error(response.error.message);
  }

  async reject(id: string, comment?: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/expenses/${id}/reject`, { comment }));
    if (!response.success) throw new Error(response.error.message);
  }

  async reimburse(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/expenses/${id}/reimburse`, {}));
    if (!response.success) throw new Error(response.error.message);
  }
}
