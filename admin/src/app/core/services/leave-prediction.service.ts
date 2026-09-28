import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface LeavePrediction {
  employeeId: string;
  employeeName: string;
  targetMonth: number;
  likelihoodPercent: number;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class LeavePredictionService {
  private readonly http = inject(HttpClient);

  async getPredictions(): Promise<LeavePrediction[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<LeavePrediction[]>>(`${API_BASE_URL}/leave-predictions`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }
}
