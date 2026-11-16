import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const PERFORMANCE_REVIEW_STATUS_LABELS = ['Pending Self-Review', 'Pending Manager Review', 'Completed'];

export interface PerformanceReview {
  id: string;
  employeeId: string;
  employeeName: string;
  period: string;
  selfRating: number | null;
  selfComments: string | null;
  selfSubmittedAt: string | null;
  managerRating: number | null;
  managerComments: string | null;
  managerSubmittedAt: string | null;
  reviewedByName: string | null;
  status: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class PerformanceReviewService {
  private readonly http = inject(HttpClient);

  async create(employeeId: string, period: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<PerformanceReview>>(`${API_BASE_URL}/performance-reviews`, { employeeId, period }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async getList(params: { employeeId?: string; page?: number; pageSize?: number } = {}) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<PerformanceReview[]>>(`${API_BASE_URL}/performance-reviews`, { params: httpParams }),
    );
    if (!response.success) throw new Error(response.error.message);
    return { items: response.data, total: response.meta?.total ?? 0 };
  }

  async submitManagerReview(id: string, rating: number, comments?: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/performance-reviews/${id}/manager-review`, { rating, comments }),
    );
    if (!response.success) throw new Error(response.error.message);
  }
}
