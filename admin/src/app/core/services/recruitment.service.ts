import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export const CANDIDATE_STAGE_LABELS = ['Applied', 'Screening', 'Interview', 'Offer', 'Hired', 'Rejected'];

export interface JobPosting {
  id: string;
  title: string;
  department: string;
  description: string | null;
  status: number;
  candidateCount: number;
  createdAt: string;
}

export interface Candidate {
  id: string;
  jobPostingId: string;
  fullName: string;
  email: string;
  phone: string | null;
  resumeUrl: string | null;
  notes: string | null;
  stage: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class RecruitmentService {
  private readonly http = inject(HttpClient);

  async getJobPostings(): Promise<JobPosting[]> {
    const response = await firstValueFrom(this.http.get<ApiEnvelope<JobPosting[]>>(`${API_BASE_URL}/recruitment/job-postings`));
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async createJobPosting(payload: { title: string; department: string; description: string | null }): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<JobPosting>>(`${API_BASE_URL}/recruitment/job-postings`, payload),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async closeJobPosting(id: string): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<null>>(`${API_BASE_URL}/recruitment/job-postings/${id}/close`, {}));
    if (!response.success) throw new Error(response.error.message);
  }

  async getCandidates(jobPostingId: string): Promise<Candidate[]> {
    const response = await firstValueFrom(
      this.http.get<ApiEnvelope<Candidate[]>>(`${API_BASE_URL}/recruitment/job-postings/${jobPostingId}/candidates`),
    );
    if (!response.success) throw new Error(response.error.message);
    return response.data;
  }

  async addCandidate(
    jobPostingId: string,
    payload: { fullName: string; email: string; phone: string | null; resumeUrl: string | null; notes: string | null },
  ): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiEnvelope<Candidate>>(`${API_BASE_URL}/recruitment/job-postings/${jobPostingId}/candidates`, payload),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async updateStage(candidateId: string, stage: number): Promise<void> {
    const response = await firstValueFrom(
      this.http.put<ApiEnvelope<Candidate>>(`${API_BASE_URL}/recruitment/candidates/${candidateId}/stage`, { stage, notes: null }),
    );
    if (!response.success) throw new Error(response.error.message);
  }

  async deleteCandidate(candidateId: string): Promise<void> {
    const response = await firstValueFrom(this.http.delete<ApiEnvelope<null>>(`${API_BASE_URL}/recruitment/candidates/${candidateId}`));
    if (!response.success) throw new Error(response.error.message);
  }
}
