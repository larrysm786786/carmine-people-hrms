import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { OvertimeService, OvertimeRequest, OVERTIME_STATUS_LABELS } from '../../core/services/overtime.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-overtime',
  imports: [FormsModule, MatTableModule, MatChipsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatSelectModule],
  templateUrl: './overtime.html',
  styleUrl: './overtime.scss',
})
export class Overtime {
  private readonly overtimeService = inject(OvertimeService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['employee', 'date', 'hours', 'reason', 'status', 'actions'];
  readonly items = signal<OvertimeRequest[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = OVERTIME_STATUS_LABELS;
  readonly actingId = signal<string | null>(null);
  readonly employees = signal<EmployeeListItem[]>([]);

  employeeId = '';

  constructor() {
    this.loadEmployees();
    this.load();
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee-name filter is a convenience; failing to load it must never block the table.
    }
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.overtimeService.getList({ pageSize: 50, employeeId: this.employeeId || undefined });
      this.items.set(result.items);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load overtime requests.');
    } finally {
      this.isLoading.set(false);
    }
  }

  onFilterChange(): void {
    this.load();
  }

  async approve(id: string): Promise<void> {
    this.actingId.set(id);
    try {
      await this.overtimeService.approve(id);
      await this.load();
      this.toast.success('Overtime request approved.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to approve overtime request.');
    } finally {
      this.actingId.set(null);
    }
  }

  async reject(id: string): Promise<void> {
    this.actingId.set(id);
    try {
      await this.overtimeService.reject(id);
      await this.load();
      this.toast.success('Overtime request rejected.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to reject overtime request.');
    } finally {
      this.actingId.set(null);
    }
  }

  statusChipClass(status: number): string {
    switch (status) {
      case 1:
        return 'status-chip--success'; // Approved
      case 2:
      case 3:
        return 'status-chip--danger'; // Rejected / Cancelled
      default:
        return 'status-chip--warning'; // Pending
    }
  }
}
