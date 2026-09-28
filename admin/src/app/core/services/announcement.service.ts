import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const AUDIENCE_TYPE_LABELS = ['All', 'Department', 'Location', 'Specific'];

export interface Announcement {
  id: string;
  title: string;
  body: string;
  audienceType: number;
  publishAt: string;
  expiresAt: string | null;
  createdByEmployeeId: string;
  imageDocumentId: string | null;
  attachmentDocumentId: string | null;
}

export interface CreateAnnouncementPayload {
  title: string;
  body: string;
  audienceType: number;
  audienceIds: string[] | null;
  publishAt: string;
  expiresAt: string | null;
  imageDocumentId: string | null;
  attachmentDocumentId: string | null;
}

@Injectable({ providedIn: 'root' })
export class AnnouncementService {
  private readonly http = inject(HttpClient);

  async getFeed(): Promise<Announcement[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Announcement[]>>(`${API_BASE_URL}/announcements`));
    return response.success ? response.data : [];
  }

  async create(payload: CreateAnnouncementPayload): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Announcement>>(`${API_BASE_URL}/announcements`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async uploadMedia(file: File): Promise<string> {
    const form = new FormData();
    form.append('file', file);
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<{ id: string }>>(`${API_BASE_URL}/documents/upload/announcement-media`, form),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data.id;
  }
}
