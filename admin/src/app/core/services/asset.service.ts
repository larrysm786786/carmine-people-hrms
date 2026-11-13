import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const ASSET_STATUS_LABELS = ['Available', 'Assigned', 'Under Repair', 'Retired'];

export interface Asset {
  id: string;
  name: string;
  category: string;
  serialNumber: string | null;
  assignedToEmployeeId: string | null;
  assignedToEmployeeName: string | null;
  assignedDate: string | null;
  status: number;
  notes: string | null;
}

export interface AssetQuery {
  category?: string;
  status?: number;
  employeeId?: string;
}

export interface CreateAssetRequest {
  name: string;
  category: string;
  serialNumber: string | null;
  notes: string | null;
}

export interface UpdateAssetRequest extends CreateAssetRequest {
  status: number;
}

@Injectable({ providedIn: 'root' })
export class AssetService {
  private readonly http = inject(HttpClient);

  async getList(query: AssetQuery = {}): Promise<Asset[]> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && (value as unknown) !== '') {
        params = params.set(key, String(value));
      }
    });
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Asset[]>>(`${API_BASE_URL}/assets`, { params }));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async create(request: CreateAssetRequest): Promise<Asset> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Asset>>(`${API_BASE_URL}/assets`, request));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async update(id: string, request: UpdateAssetRequest): Promise<Asset> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<Asset>>(`${API_BASE_URL}/assets/${id}`, request));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async assign(id: string, employeeId: string): Promise<Asset> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Asset>>(`${API_BASE_URL}/assets/${id}/assign`, { employeeId }));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async unassign(id: string): Promise<Asset> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Asset>>(`${API_BASE_URL}/assets/${id}/unassign`, {}));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async delete(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/assets/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }
}
