import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface NotificationPreference {
  channel: number;
  category: string;
  isEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationPreferenceService {
  private readonly http = inject(HttpClient);

  async getPreferences(): Promise<NotificationPreference[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<NotificationPreference[]>>(`${API_BASE_URL}/notifications/preferences`));
    return response.success ? response.data : [];
  }

  async setPreference(channel: number, category: string, isEnabled: boolean): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/notifications/preferences`, { channel, category, isEnabled }),
    );
    if (!response.success) throw new Error(response.error.message);
  }
}
