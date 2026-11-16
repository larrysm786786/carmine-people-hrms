import { errorMessage } from '../../core/utils/error-message';
import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ExpenseService, Expense, EXPENSE_STATUS_LABELS } from '../../core/services/expense.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-expenses',
  imports: [FormsModule, DecimalPipe, MatTableModule, MatChipsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './expenses.html',
  styleUrl: './expenses.scss',
})
export class Expenses {
  private readonly expenseService = inject(ExpenseService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['employee', 'category', 'amount', 'date', 'description', 'status', 'actions'];
  readonly items = signal<Expense[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = EXPENSE_STATUS_LABELS;
  readonly actingId = signal<string | null>(null);
  readonly employees = signal<EmployeeListItem[]>([]);

  employeeId = '';
  statusFilter: number | '' = '';

  constructor() {
    this.loadEmployees();
    this.load();
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee-name filter is a convenience; failing to load the list must
      // never block the expense claims table itself from loading.
    }
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.expenseService.getList({
        pageSize: 50,
        employeeId: this.employeeId || undefined,
        status: this.statusFilter === '' ? undefined : this.statusFilter,
      });
      this.items.set(result.items);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load expense claims.');
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
      await this.expenseService.approve(id);
      await this.load();
      this.toast.success('Expense claim approved.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to approve expense claim.');
    } finally {
      this.actingId.set(null);
    }
  }

  async reject(id: string): Promise<void> {
    this.actingId.set(id);
    try {
      await this.expenseService.reject(id);
      await this.load();
      this.toast.success('Expense claim rejected.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to reject expense claim.');
    } finally {
      this.actingId.set(null);
    }
  }

  async reimburse(id: string): Promise<void> {
    this.actingId.set(id);
    try {
      await this.expenseService.reimburse(id);
      await this.load();
      this.toast.success('Expense claim marked as reimbursed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to mark expense claim as reimbursed.');
    } finally {
      this.actingId.set(null);
    }
  }

  // Semantic mapping: Approved/Reimbursed read success, Rejected reads danger,
  // Pending (still awaiting a decision) reads warning.
  statusChipClass(status: number): string {
    switch (status) {
      case 1:
      case 3:
        return 'status-chip--success'; // Approved / Reimbursed
      case 2:
        return 'status-chip--danger'; // Rejected
      default:
        return 'status-chip--warning'; // Pending
    }
  }
}
