import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface AuditLog {
  id: string;
  actorEmployeeId: string | null;
  actorName: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  timestamp: string;
  ipAddress: string | null;
  metadataJson: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);

  async getList(page = 1, pageSize = 20): Promise<{ items: AuditLog[]; total: number }> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    const response = await firstValueFrom(this.http.get<ApiEnvelope<AuditLog[]>>(`${API_BASE_URL}/audit`, { params }));
    if (response.success) {
      return { items: response.data, total: response.meta?.total ?? 0 };
    }
    throw new Error(response.error.message);
  }
}
