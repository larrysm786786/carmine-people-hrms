import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface Holiday {
  id: string;
  name: string;
  description: string | null;
  date: string;
  locationId: string | null;
  isOptional: boolean;
}

@Injectable({ providedIn: 'root' })
export class HolidayService {
  private readonly http = inject(HttpClient);

  async getList(year?: number): Promise<Holiday[]> {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Holiday[]>>(`${API_BASE_URL}/holidays`, { params }));
    return response.success ? response.data : [];
  }

  async create(name: string, description: string | null, date: string, isOptional: boolean): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<Holiday>>(`${API_BASE_URL}/holidays`, { name, description, date, isOptional }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async delete(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/holidays/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }
}
