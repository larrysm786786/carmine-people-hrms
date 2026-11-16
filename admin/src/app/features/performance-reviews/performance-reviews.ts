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
import {
  PerformanceReviewService,
  PerformanceReview,
  PERFORMANCE_REVIEW_STATUS_LABELS,
} from '../../core/services/performance-review.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-performance-reviews',
  imports: [
    FormsModule, MatTableModule, MatChipsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  templateUrl: './performance-reviews.html',
  styleUrl: './performance-reviews.scss',
})
export class PerformanceReviews {
  private readonly reviewService = inject(PerformanceReviewService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['employee', 'period', 'selfRating', 'managerRating', 'status', 'actions'];
  readonly reviews = signal<PerformanceReview[]>([]);
  readonly isLoading = signal(true);
  readonly statusLabels = PERFORMANCE_REVIEW_STATUS_LABELS;
  readonly employees = signal<EmployeeListItem[]>([]);

  newEmployeeId = '';
  newPeriod = '';

  managerRatingDraft: Record<string, number> = {};
  managerCommentsDraft: Record<string, string> = {};

  constructor() {
    this.loadEmployees();
    this.load();
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee picker is a convenience; failing to load it must never block the page.
    }
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.reviewService.getList({ pageSize: 50 });
      this.reviews.set(result.items);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load performance reviews.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async create(): Promise<void> {
    if (!this.newEmployeeId || !this.newPeriod.trim()) return;
    try {
      await this.reviewService.create(this.newEmployeeId, this.newPeriod.trim());
      this.newEmployeeId = '';
      this.newPeriod = '';
      await this.load();
      this.toast.success('Performance review cycle started.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to start review.');
    }
  }

  async submitManagerReview(review: PerformanceReview): Promise<void> {
    const rating = this.managerRatingDraft[review.id];
    if (!rating) return;
    try {
      await this.reviewService.submitManagerReview(review.id, rating, this.managerCommentsDraft[review.id]);
      await this.load();
      this.toast.success('Manager review submitted.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to submit manager review.');
    }
  }

  statusChipClass(status: number): string {
    switch (status) {
      case 2:
        return 'status-chip--success'; // Completed
      default:
        return 'status-chip--warning'; // Pending self/manager review
    }
  }
}
