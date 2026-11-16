import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RecruitmentService, JobPosting, Candidate, CANDIDATE_STAGE_LABELS } from '../../core/services/recruitment.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-recruitment',
  imports: [
    FormsModule, MatTableModule, MatChipsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  templateUrl: './recruitment.html',
  styleUrl: './recruitment.scss',
})
export class Recruitment {
  private readonly recruitmentService = inject(RecruitmentService);
  private readonly toast = inject(ToastService);

  readonly stageLabels = CANDIDATE_STAGE_LABELS;
  readonly jobPostings = signal<JobPosting[]>([]);
  readonly isLoading = signal(true);
  newTitle = '';
  newDepartment = '';
  newDescription = '';

  readonly selectedJobId = signal<string | null>(null);
  readonly candidates = signal<Candidate[]>([]);
  readonly isLoadingCandidates = signal(false);
  readonly candidateColumns = ['fullName', 'email', 'stage', 'actions'];
  newCandidate = { fullName: '', email: '', phone: '', resumeUrl: '', notes: '' };

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.jobPostings.set(await this.recruitmentService.getJobPostings());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load job postings.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async createJobPosting(): Promise<void> {
    if (!this.newTitle.trim() || !this.newDepartment.trim()) return;
    try {
      await this.recruitmentService.createJobPosting({
        title: this.newTitle.trim(),
        department: this.newDepartment.trim(),
        description: this.newDescription.trim() || null,
      });
      this.newTitle = '';
      this.newDepartment = '';
      this.newDescription = '';
      await this.load();
      this.toast.success('Job posting created.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to create job posting.');
    }
  }

  async closeJobPosting(job: JobPosting): Promise<void> {
    try {
      await this.recruitmentService.closeJobPosting(job.id);
      await this.load();
      this.toast.success('Job posting closed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to close job posting.');
    }
  }

  async selectJob(job: JobPosting): Promise<void> {
    if (this.selectedJobId() === job.id) {
      this.selectedJobId.set(null);
      return;
    }
    this.selectedJobId.set(job.id);
    this.isLoadingCandidates.set(true);
    try {
      this.candidates.set(await this.recruitmentService.getCandidates(job.id));
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load candidates.');
    } finally {
      this.isLoadingCandidates.set(false);
    }
  }

  async addCandidate(): Promise<void> {
    const jobId = this.selectedJobId();
    if (!jobId || !this.newCandidate.fullName.trim() || !this.newCandidate.email.trim()) return;
    try {
      await this.recruitmentService.addCandidate(jobId, {
        fullName: this.newCandidate.fullName.trim(),
        email: this.newCandidate.email.trim(),
        phone: this.newCandidate.phone.trim() || null,
        resumeUrl: this.newCandidate.resumeUrl.trim() || null,
        notes: this.newCandidate.notes.trim() || null,
      });
      this.newCandidate = { fullName: '', email: '', phone: '', resumeUrl: '', notes: '' };
      this.candidates.set(await this.recruitmentService.getCandidates(jobId));
      await this.load();
      this.toast.success('Candidate added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add candidate.');
    }
  }

  async updateStage(candidate: Candidate, stage: number): Promise<void> {
    try {
      await this.recruitmentService.updateStage(candidate.id, stage);
      const jobId = this.selectedJobId();
      if (jobId) this.candidates.set(await this.recruitmentService.getCandidates(jobId));
      this.toast.success('Candidate stage updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update stage.');
    }
  }

  async removeCandidate(candidate: Candidate): Promise<void> {
    try {
      await this.recruitmentService.deleteCandidate(candidate.id);
      const jobId = this.selectedJobId();
      if (jobId) this.candidates.set(await this.recruitmentService.getCandidates(jobId));
      await this.load();
      this.toast.success('Candidate removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to remove candidate.');
    }
  }

  stageChipClass(stage: number): string {
    switch (stage) {
      case 4:
        return 'status-chip--success'; // Hired
      case 5:
        return 'status-chip--danger'; // Rejected
      default:
        return 'status-chip--warning'; // In progress
    }
  }
}
