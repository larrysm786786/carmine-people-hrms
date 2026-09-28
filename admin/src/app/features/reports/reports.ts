import { errorMessage } from '../../core/utils/error-message';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { ReportService, ReportType, ReportQuery } from '../../core/services/report.service';
import { OrganizationService, Department } from '../../core/services/organization.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ATTENDANCE_STATUS_LABELS } from '../../core/services/attendance.service';
import { LEAVE_STATUS_LABELS } from '../../core/services/leave.service';
import { ToastService } from '../../core/services/toast.service';

interface ReportTab {
  type: ReportType;
  label: string;
  columns: string[];
  hasStatus: boolean;
  statusLabels?: string[];
}

const REPORT_TABS: ReportTab[] = [
  { type: 'attendance', label: 'Attendance', columns: ['employeeCode', 'employeeName', 'department', 'date', 'status', 'workingMinutes', 'checkInAt', 'checkOutAt'], hasStatus: true, statusLabels: ATTENDANCE_STATUS_LABELS },
  { type: 'leave', label: 'Leave', columns: ['employeeCode', 'employeeName', 'leaveType', 'startDate', 'endDate', 'daysCount', 'status'], hasStatus: true, statusLabels: LEAVE_STATUS_LABELS },
  { type: 'employee', label: 'Employee', columns: ['employeeCode', 'fullName', 'email', 'department', 'designation', 'location', 'status', 'dateOfJoining'], hasStatus: false },
  { type: 'late', label: 'Late', columns: ['employeeCode', 'employeeName', 'date', 'checkInAt'], hasStatus: false },
  { type: 'working-hours', label: 'Working Hours', columns: ['employeeCode', 'employeeName', 'date', 'hours'], hasStatus: false },
];

@Component({
  selector: 'app-reports',
  imports: [FormsModule, MatTabsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatTableModule, MatCardModule],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
})
export class Reports {
  private readonly reportService = inject(ReportService);
  private readonly organizationService = inject(OrganizationService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly tabs = REPORT_TABS;
  readonly selectedTabIndex = signal(0);
  readonly selectedTab = computed(() => this.tabs[this.selectedTabIndex()]);

  readonly departments = signal<Department[]>([]);
  readonly employees = signal<EmployeeListItem[]>([]);
  readonly rows = signal<Record<string, unknown>[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasRun = signal(false);

  from = '';
  to = '';
  departmentId = '';
  employeeId = '';
  status: number | '' = '';

  constructor() {
    this.loadFilterOptions();
  }

  private async loadFilterOptions(): Promise<void> {
    try {
      const [departments, employeesPage] = await Promise.all([
        this.organizationService.getDepartments(),
        this.employeeService.getList({ page: 1, pageSize: 200 }),
      ]);
      this.departments.set(departments);
      this.employees.set(employeesPage.items);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load filter options.');
    }
  }

  onTabChange(index: number): void {
    this.selectedTabIndex.set(index);
    this.status = '';
    this.rows.set([]);
    this.hasRun.set(false);
  }

  private buildQuery(): ReportQuery {
    return {
      from: this.from || undefined,
      to: this.to || undefined,
      departmentId: this.departmentId || undefined,
      employeeId: this.employeeId || undefined,
      status: this.status === '' ? undefined : this.status,
    };
  }

  async runReport(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const data = await this.reportService.getReport<Record<string, unknown>>(this.selectedTab().type, this.buildQuery());
      this.rows.set(data);
      this.hasRun.set(true);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load report.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async exportReport(format: 'csv' | 'xlsx'): Promise<void> {
    try {
      await this.reportService.exportReport(this.selectedTab().type, this.buildQuery(), format);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to export report.');
    }
  }

  statusLabel(value: unknown): string {
    return String(value ?? '');
  }
}
