import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { LeaveService, LeaveApplication, LeaveType, LEAVE_STATUS_LABELS } from '../../core/services/leave.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-leave',
  imports: [FormsModule, MatTableModule, MatChipsModule, MatButtonModule, MatIconModule, MatTabsModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, MatSelectModule],
  templateUrl: './leave.html',
  styleUrl: './leave.scss',
})
export class Leave {
  private readonly leaveService = inject(LeaveService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['employee', 'leaveType', 'dates', 'days', 'reason', 'status', 'actions'];
  readonly items = signal<LeaveApplication[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = LEAVE_STATUS_LABELS;
  readonly actingId = signal<string | null>(null);
  readonly employees = signal<EmployeeListItem[]>([]);
  employeeId = '';

  readonly types = signal<LeaveType[]>([]);
  newType = { name: '', isPaid: true, requiresAttachment: false, defaultAnnualDays: 12 };
  editingTypeId: string | null = null;
  editType = { name: '', isPaid: true, requiresAttachment: false, defaultAnnualDays: 0 };

  readonly isAllocating = signal(false);
  allocateYear = new Date().getFullYear();

  constructor() {
    this.loadEmployees();
    this.load();
    this.loadTypes();
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee-name filter is a convenience; failing to load the list must
      // never block the leave-requests table itself from loading.
    }
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.leaveService.getList({ pageSize: 50, employeeId: this.employeeId || undefined });
      this.items.set(result.items);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load leave requests.');
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
      await this.leaveService.approve(id);
      await this.load();
      this.toast.success('Leave request approved.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to approve leave request.');
    } finally {
      this.actingId.set(null);
    }
  }

  async reject(id: string): Promise<void> {
    this.actingId.set(id);
    try {
      await this.leaveService.reject(id);
      await this.load();
      this.toast.success('Leave request rejected.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to reject leave request.');
    } finally {
      this.actingId.set(null);
    }
  }

  // Semantic mapping instead of every status rendering in the same neutral chip color:
  // Approved reads success, Rejected/Cancelled danger, anything still in flight warning.
  statusChipClass(status: number): string {
    switch (status) {
      case 2:
        return 'status-chip--success'; // Approved
      case 3:
      case 4:
        return 'status-chip--danger'; // Rejected / Cancelled
      default:
        return 'status-chip--warning'; // Pending / Manager Approved
    }
  }

  async loadTypes(): Promise<void> {
    try {
      this.types.set(await this.leaveService.getTypes());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load leave types.');
    }
  }

  async addType(): Promise<void> {
    if (!this.newType.name.trim()) return;
    try {
      await this.leaveService.createType({ ...this.newType, name: this.newType.name.trim() });
      this.newType = { name: '', isPaid: true, requiresAttachment: false, defaultAnnualDays: 12 };
      await this.loadTypes();
      this.toast.success('Leave type created.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to create leave type.');
    }
  }

  startEdit(type: LeaveType): void {
    this.editingTypeId = type.id;
    this.editType = { name: type.name, isPaid: type.isPaid, requiresAttachment: type.requiresAttachment, defaultAnnualDays: type.defaultAnnualDays };
  }

  cancelEdit(): void {
    this.editingTypeId = null;
  }

  async saveEdit(): Promise<void> {
    if (!this.editingTypeId || !this.editType.name.trim()) return;
    try {
      await this.leaveService.updateType(this.editingTypeId, { ...this.editType, name: this.editType.name.trim() });
      this.editingTypeId = null;
      await this.loadTypes();
      this.toast.success('Leave type updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update leave type.');
    }
  }

  async allocateDefaultBalances(): Promise<void> {
    this.isAllocating.set(true);
    try {
      const created = await this.leaveService.allocateDefaultBalances(this.allocateYear);
      this.toast.success(
        created > 0
          ? `Allocated ${created} leave balance(s) for ${this.allocateYear}.`
          : `All employees already have their ${this.allocateYear} leave balances.`,
      );
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to allocate leave balances.');
    } finally {
      this.isAllocating.set(false);
    }
  }
}
