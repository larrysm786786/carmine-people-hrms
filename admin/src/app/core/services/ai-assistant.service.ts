import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface AiChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

@Injectable({ providedIn: 'root' })
export class AiAssistantService {
  private readonly http = inject(HttpClient);

  async isConfigured(): Promise<boolean> {
    try {
      const response = await firstValueFrom(this.http.get<ApiEnvelope<{ configured: boolean }>>(`${API_BASE_URL}/ai/status`));
      return response.success ? response.data.configured : false;
    } catch {
      return false;
    }
  }

  async sendMessage(message: string, history: AiChatMessage[]): Promise<string> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<{ reply: string }>>(`${API_BASE_URL}/ai/chat/hr`, { message, history }),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data.reply;
  }
}
