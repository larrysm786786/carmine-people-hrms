import { errorMessage } from '../../core/utils/error-message';
import { dateToIso } from '../../core/utils/date-format';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { AttendanceService, AttendanceRecord, ATTENDANCE_STATUS_LABELS, RegularizationRequest } from '../../core/services/attendance.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-attendance',
  imports: [FormsModule, MatTableModule, MatChipsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatPaginatorModule, MatCardModule, MatDatepickerModule],
  templateUrl: './attendance.html',
  styleUrl: './attendance.scss',
})
export class Attendance {
  private readonly attendanceService = inject(AttendanceService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['employee', 'date', 'checkInAt', 'checkOutAt', 'breakMinutes', 'workingMinutes', 'status'];
  readonly items = signal<AttendanceRecord[]>([]);
  readonly total = signal(0);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = ATTENDANCE_STATUS_LABELS;
  readonly employees = signal<EmployeeListItem[]>([]);

  readonly pendingRegularizations = signal<RegularizationRequest[]>([]);
  readonly actingRegularizationId = signal<string | null>(null);

  fromDate: Date | null = null;
  toDate: Date | null = null;
  status: number | null = null;
  employeeId = '';
  page = 1;
  pageSize = 20;

  constructor() {
    this.loadEmployees();
    this.load();
    this.loadPendingRegularizations();
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee-name filter is a convenience; failing to load the list must
      // never block the attendance table itself from loading.
    }
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.attendanceService.getList({
        from: dateToIso(this.fromDate) || undefined,
        to: dateToIso(this.toDate) || undefined,
        status: this.status ?? undefined,
        employeeId: this.employeeId || undefined,
        page: this.page,
        pageSize: this.pageSize,
      });
      this.items.set(result.items);
      this.total.set(result.total);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load attendance records.');
    } finally {
      this.isLoading.set(false);
    }
  }

  onFilterChange(): void {
    this.page = 1;
    this.load();
  }

  clearFilters(): void {
    this.fromDate = null;
    this.toDate = null;
    this.status = null;
    this.employeeId = '';
    this.onFilterChange();
  }

  onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  formatTime(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  async loadPendingRegularizations(): Promise<void> {
    try {
      this.pendingRegularizations.set(await this.attendanceService.getPendingRegularizations());
    } catch {
      // Non-critical banner on this page; failing to load it must never block the main table.
    }
  }

  async approveRegularization(req: RegularizationRequest): Promise<void> {
    this.actingRegularizationId.set(req.id);
    try {
      await this.attendanceService.approveRegularization(req.id);
      await Promise.all([this.loadPendingRegularizations(), this.load()]);
      this.toast.success('Regularization approved.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to approve regularization.');
    } finally {
      this.actingRegularizationId.set(null);
    }
  }

  async rejectRegularization(req: RegularizationRequest): Promise<void> {
    this.actingRegularizationId.set(req.id);
    try {
      await this.attendanceService.rejectRegularization(req.id);
      await Promise.all([this.loadPendingRegularizations(), this.load()]);
      this.toast.success('Regularization rejected.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to reject regularization.');
    } finally {
      this.actingRegularizationId.set(null);
    }
  }

  statusChipClass(status: number): string {
    switch (status) {
      case 0: // Present
      case 7: // WFH
        return 'status-chip--success';
      case 1: // Absent
        return 'status-chip--danger';
      case 2: // Late
      case 8: // PendingRegularization
        return 'status-chip--warning';
      default:
        return '';
    }
  }
}
