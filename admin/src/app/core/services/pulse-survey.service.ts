import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface PulseSurvey {
  id: string;
  question: string;
  isActive: boolean;
  responseCount: number;
  averageRating: number | null;
  createdAt: string;
}

export interface PulseSurveyResponse {
  id: string;
  employeeName: string | null;
  rating: number;
  comment: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class PulseSurveyService {
  private readonly http = inject(HttpClient);

  async getList(): Promise<PulseSurvey[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<PulseSurvey[]>>(`${API_BASE_URL}/pulse-surveys`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async create(question: string): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<PulseSurvey>>(`${API_BASE_URL}/pulse-surveys`, { question }));
    if (!response.success) throw new Error(response.error.message);
  }

  async getResponses(id: string): Promise<PulseSurveyResponse[]> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<PulseSurveyResponse[]>>(`${API_BASE_URL}/pulse-surveys/${id}/responses`),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async close(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/pulse-surveys/${id}/close`, {}));
    if (!response.success) throw new Error(response.error.message);
  }
}
