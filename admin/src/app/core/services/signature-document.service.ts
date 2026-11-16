import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface SignatureDocument {
  id: string;
  title: string;
  content: string;
  isActive: boolean;
  signatureCount: number;
  createdAt: string;
}

export interface DocumentSignature {
  id: string;
  employeeName: string;
  signedAt: string;
}

@Injectable({ providedIn: 'root' })
export class SignatureDocumentService {
  private readonly http = inject(HttpClient);

  async getList(): Promise<SignatureDocument[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<SignatureDocument[]>>(`${API_BASE_URL}/signature-documents`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async create(payload: { title: string; content: string }): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<SignatureDocument>>(`${API_BASE_URL}/signature-documents`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async getSignatures(id: string): Promise<DocumentSignature[]> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<DocumentSignature[]>>(`${API_BASE_URL}/signature-documents/${id}/signatures`),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async close(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/signature-documents/${id}/close`, {}));
    if (!response.success) throw new Error(response.error.message);
  }
}
