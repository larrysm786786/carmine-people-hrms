import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const DOCUMENT_TYPE_LABELS = [
  'Offer Letter', 'Appointment Letter', 'Policy', 'Salary Slip', 'ID Proof', 'Other', 'Leave Attachment', 'Profile Photo',
];

export interface EmployeeDocument {
  id: string;
  employeeId: string;
  documentType: number;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);

  async getForEmployee(employeeId: string): Promise<EmployeeDocument[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<EmployeeDocument[]>>(`${API_BASE_URL}/employees/${employeeId}/documents`));
    return response.success ? response.data : [];
  }

  async upload(employeeId: string, documentType: number, file: File): Promise<void> {
    const form = new FormData();
    form.append('EmployeeId', employeeId);
    form.append('DocumentType', String(documentType));
    form.append('File', file);
    const response = await firstValueFrom(this.http.post<ApiEnvelope<EmployeeDocument>>(`${API_BASE_URL}/documents/upload`, form));
    if (!response.success) throw new Error(response.error.message);
  }

  // The backend's "download URL" is a relative, authenticated API path, not a public/signed
  // URL, so it must be fetched via HttpClient (which attaches the auth header through the
  // interceptor) rather than opened directly in a new tab/window.
  async download(id: string, fileName: string): Promise<void> {
    const urlResponse = await firstValueFrom(this.http.get<ApiEnvelope<{ url: string; expiresAt: string }>>(`${API_BASE_URL}/documents/${id}/download`));
    if (!urlResponse.success) throw new Error(urlResponse.error.message);
    const blob = await firstValueFrom(this.http.get(`${API_BASE_URL}${urlResponse.data.url}`, { responseType: 'blob' }));
    const objectUrl = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(objectUrl);
  }
}
