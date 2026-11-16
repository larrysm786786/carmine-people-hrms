import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface OnboardingTaskTemplate {
  id: string;
  title: string;
  description: string | null;
  sortOrder: number;
}

export interface OnboardingTask {
  id: string;
  employeeId: string;
  employeeName: string;
  title: string;
  description: string | null;
  sortOrder: number;
  isCompleted: boolean;
  completedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly http = inject(HttpClient);

  async getTemplates(): Promise<OnboardingTaskTemplate[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<OnboardingTaskTemplate[]>>(`${API_BASE_URL}/onboarding/templates`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async createTemplate(payload: { title: string; description: string | null; sortOrder: number }): Promise<void> {
    const response = await firstValueFrom(this.http.post<ApiEnvelope<OnboardingTaskTemplate>>(`${API_BASE_URL}/onboarding/templates`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async updateTemplate(id: string, payload: { title: string; description: string | null; sortOrder: number }): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<OnboardingTaskTemplate>>(`${API_BASE_URL}/onboarding/templates/${id}`, payload));
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteTemplate(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/onboarding/templates/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }

  async getTasksForEmployee(employeeId: string): Promise<OnboardingTask[]> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<OnboardingTask[]>>(`${API_BASE_URL}/onboarding/employees/${employeeId}/tasks`),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async applyTemplates(employeeId: string): Promise<number> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<{ added: number }>>(`${API_BASE_URL}/onboarding/employees/${employeeId}/apply-templates`, {}),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data.added;
  }

  async addTask(employeeId: string, payload: { title: string; description: string | null; sortOrder: number }): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<OnboardingTask>>(`${API_BASE_URL}/onboarding/employees/${employeeId}/tasks`, payload),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async complete(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/onboarding/tasks/${id}/complete`, {}));
    if (!response.success) throw new Error(response.error.message);
  }

  async reopen(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/onboarding/tasks/${id}/reopen`, {}));
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteTask(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/onboarding/tasks/${id}`));
    if (!response.success) throw new Error(response.error.message);
  }
}
