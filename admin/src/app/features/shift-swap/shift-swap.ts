import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { ShiftSwapService, ShiftSwapRequest, SHIFT_SWAP_STATUS_LABELS } from '../../core/services/shift-swap.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-shift-swap',
  imports: [FormsModule, MatTableModule, MatChipsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatSelectModule],
  templateUrl: './shift-swap.html',
  styleUrl: './shift-swap.scss',
})
export class ShiftSwap {
  private readonly shiftSwapService = inject(ShiftSwapService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['requester', 'counterpart', 'dates', 'reason', 'status', 'actions'];
  readonly items = signal<ShiftSwapRequest[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = SHIFT_SWAP_STATUS_LABELS;
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
      const result = await this.shiftSwapService.getList({ pageSize: 50, employeeId: this.employeeId || undefined });
      this.items.set(result.items);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load shift swap requests.');
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
      await this.shiftSwapService.approve(id);
      await this.load();
      this.toast.success('Shift swap approved.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to approve shift swap.');
    } finally {
      this.actingId.set(null);
    }
  }

  async reject(id: string): Promise<void> {
    this.actingId.set(id);
    try {
      await this.shiftSwapService.reject(id);
      await this.load();
      this.toast.success('Shift swap rejected.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to reject shift swap.');
    } finally {
      this.actingId.set(null);
    }
  }

  // Only a fully mutual (employee-accepted) request reaching Pending is something a
  // manager/HR can act on; RequestedAcceptance is still waiting on the counterpart employee.
  statusChipClass(status: number): string {
    switch (status) {
      case 2:
        return 'status-chip--success'; // Approved
      case 3:
      case 4:
        return 'status-chip--danger'; // Rejected / Cancelled
      default:
        return 'status-chip--warning'; // RequestedAcceptance / Pending
    }
  }
}
