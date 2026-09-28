import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface Department {
  id: string;
  name: string;
  code: string | null;
  description: string | null;
  managerId: string | null;
  managerName: string | null;
  isActive: boolean;
}

export interface DepartmentPayload {
  name: string;
  code: string | null;
  description: string | null;
  managerId: string | null;
  isActive: boolean;
}

export interface Designation {
  id: string;
  title: string;
  description: string | null;
  departmentId: string;
  departmentName: string;
  isActive: boolean;
}

export interface DesignationPayload {
  title: string;
  description: string | null;
  departmentId: string;
  isActive: boolean;
}

export interface Location {
  id: string;
  name: string;
  address: string | null;
  latitude: number;
  longitude: number;
  geofenceRadiusMeters: number;
  isActive: boolean;
  officeWifiSsid: string | null;
}

export const SHIFT_TYPE_LABELS = ['General', 'Morning', 'Evening', 'Night', 'Flexible'];

export interface Shift {
  id: string;
  name: string;
  type: number;
  startTime: string;
  endTime: string;
  gracePeriodMinutes: number;
  breakMinutes: number;
  workingMinutes: number;
  requiredWorkMinutes: number;
}

export interface EmployeeShift {
  id: string;
  employeeId: string;
  shiftId: string;
  shiftName: string;
  effectiveFrom: string;
  effectiveTo: string | null;
}

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);

  // Departments
  async getDepartments(): Promise<Department[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Department[]>>(`${API_BASE_URL}/departments`));
    return response.success ? response.data : [];
  }

  async createDepartment(payload: DepartmentPayload): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Department>>(`${API_BASE_URL}/departments`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async updateDepartment(id: string, payload: DepartmentPayload): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<Department>>(`${API_BASE_URL}/departments/${id}`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteDepartment(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/departments/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }

  // Designations
  async getDesignations(departmentId?: string): Promise<Designation[]> {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Designation[]>>(`${API_BASE_URL}/designations`, { params }));
    return response.success ? response.data : [];
  }

  async createDesignation(payload: DesignationPayload): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Designation>>(`${API_BASE_URL}/designations`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async updateDesignation(id: string, payload: DesignationPayload): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<Designation>>(`${API_BASE_URL}/designations/${id}`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteDesignation(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/designations/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }

  // Locations
  async getLocations(): Promise<Location[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Location[]>>(`${API_BASE_URL}/locations`));
    return response.success ? response.data : [];
  }

  async createLocation(payload: Omit<Location, 'id'>): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Location>>(`${API_BASE_URL}/locations`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async updateLocation(id: string, payload: Omit<Location, 'id'>): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<Location>>(`${API_BASE_URL}/locations/${id}`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteLocation(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/locations/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }

  // Shifts
  async getShifts(): Promise<Shift[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<Shift[]>>(`${API_BASE_URL}/shifts`));
    return response.success ? response.data : [];
  }

  async createShift(payload: { name: string; type: number; startTime: string; endTime: string; gracePeriodMinutes: number; breakMinutes: number; requiredWorkMinutes: number }): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<Shift>>(`${API_BASE_URL}/shifts`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async updateShift(id: string, payload: { name: string; type: number; startTime: string; endTime: string; gracePeriodMinutes: number; breakMinutes: number; requiredWorkMinutes: number }): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<Shift>>(`${API_BASE_URL}/shifts/${id}`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async assignShift(employeeId: string, shiftId: string, effectiveFrom: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<unknown>>(`${API_BASE_URL}/shifts/assign`, { employeeId, shiftId, effectiveFrom }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async getEmployeeShifts(): Promise<EmployeeShift[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<EmployeeShift[]>>(`${API_BASE_URL}/employee-shifts`));
    return response.success ? response.data : [];
  }
}
