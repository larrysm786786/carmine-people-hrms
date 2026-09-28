import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem, EMPLOYMENT_STATUS_LABELS } from '../../core/models/employee.model';
import { DocumentService, EmployeeDocument, DOCUMENT_TYPE_LABELS } from '../../core/services/document.service';
import { OrganizationService, Department, Location, Shift } from '../../core/services/organization.service';
import { EmployeeFormDialog } from './employee-form-dialog';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-employees',
  imports: [
    FormsModule,
    MatTableModule,
    MatSortModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatChipsModule,
    MatButtonModule,
    MatPaginatorModule,
    MatSelectModule,
    MatTooltipModule,
    MatCardModule,
  ],
  templateUrl: './employees.html',
  styleUrl: './employees.scss',
})
export class Employees {
  private readonly employeeService = inject(EmployeeService);
  private readonly documentService = inject(DocumentService);
  private readonly organizationService = inject(OrganizationService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['employeeCode', 'name', 'department', 'designation', 'location', 'role', 'status', 'shift', 'edit', 'documents', 'deactivate'];
  readonly items = signal<EmployeeListItem[]>([]);
  readonly total = signal(0);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = EMPLOYMENT_STATUS_LABELS;
  readonly documentTypeLabels = DOCUMENT_TYPE_LABELS;

  readonly departments = signal<Department[]>([]);
  readonly locations = signal<Location[]>([]);
  readonly shifts = signal<Shift[]>([]);

  readonly expandedEmployeeId = signal<string | null>(null);
  readonly documents = signal<EmployeeDocument[]>([]);
  uploadDocumentType = 0;

  readonly shiftAssignEmployeeId = signal<string | null>(null);
  shiftAssignShiftId = '';

  search = '';
  filterDepartmentId = '';
  filterLocationId = '';
  filterStatus: number | '' = '';
  page = 1;
  pageSize = 20;
  sortBy = '';
  sortDirection: 'asc' | 'desc' | '' = '';

  constructor() {
    this.load();
    this.loadFilterOptions();
  }

  private async loadFilterOptions(): Promise<void> {
    try {
      const [departments, locations, shifts] = await Promise.all([
        this.organizationService.getDepartments(),
        this.organizationService.getLocations(),
        this.organizationService.getShifts(),
      ]);
      this.departments.set(departments);
      this.locations.set(locations);
      this.shifts.set(shifts);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load filter options.');
    }
  }

  async toggleDocuments(employeeId: string): Promise<void> {
    if (this.expandedEmployeeId() === employeeId) {
      this.expandedEmployeeId.set(null);
      return;
    }
    this.expandedEmployeeId.set(employeeId);
    this.documents.set(await this.documentService.getForEmployee(employeeId));
  }

  async uploadDocument(employeeId: string, fileInput: HTMLInputElement): Promise<void> {
    const file = fileInput.files?.[0];
    if (!file) return;
    // Cleared up front so a failed upload can be retried with the same file.
    fileInput.value = '';
    try {
      await this.documentService.upload(employeeId, this.uploadDocumentType, file);
      this.documents.set(await this.documentService.getForEmployee(employeeId));
      this.toast.success('Document uploaded.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to upload document.');
    }
  }

  async downloadDocument(id: string, fileName: string): Promise<void> {
    try {
      await this.documentService.download(id, fileName);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to download document.');
    }
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await this.employeeService.getList({
        search: this.search,
        departmentId: this.filterDepartmentId || undefined,
        locationId: this.filterLocationId || undefined,
        status: this.filterStatus === '' ? undefined : this.filterStatus,
        page: this.page,
        pageSize: this.pageSize,
        sortBy: this.sortBy || undefined,
        sortDirection: this.sortDirection || undefined,
      });
      this.items.set(result.items);
      this.total.set(result.total);
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load employees.');
    } finally {
      this.isLoading.set(false);
    }
  }

  onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  onSearchChange(): void {
    this.page = 1;
    this.load();
  }

  onFilterChange(): void {
    this.page = 1;
    this.load();
  }

  onSortChange(sort: Sort): void {
    this.sortBy = sort.direction ? sort.active : '';
    this.sortDirection = sort.direction === 'asc' || sort.direction === 'desc' ? sort.direction : '';
    this.load();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(EmployeeFormDialog, { data: { employeeId: null } });
    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.load();
        this.toast.success('Employee created.');
      }
    });
  }

  openEditDialog(employeeId: string): void {
    const ref = this.dialog.open(EmployeeFormDialog, { data: { employeeId } });
    ref.afterClosed().subscribe((result) => {
      if (result) {
        this.load();
        this.toast.success('Employee updated.');
      }
    });
  }

  deactivate(employee: EmployeeListItem): void {
    const ref = this.dialog.open(ConfirmDialog, {
      data: {
        title: 'Deactivate employee',
        message: `Deactivate ${employee.firstName} ${employee.lastName}? They will no longer be able to sign in.`,
        confirmLabel: 'Deactivate',
        danger: true,
      },
    });
    ref.afterClosed().subscribe(async (confirmed) => {
      if (!confirmed) return;
      try {
        const detail = await this.employeeService.getById(employee.id);
        await this.employeeService.update(employee.id, {
          firstName: detail.firstName,
          lastName: detail.lastName,
          phoneNumber: detail.phoneNumber,
          departmentId: detail.departmentId,
          designationId: detail.designationId,
          locationId: detail.locationId,
          managerId: detail.managerId,
          roleId: detail.roleId,
          dateOfBirth: detail.dateOfBirth,
          employmentStatus: 1, // Inactive
        });
        this.toast.success('Employee deactivated.');
        this.load();
      } catch (err) {
        this.toast.error(errorMessage(err) ?? 'Unable to deactivate employee.');
      }
    });
  }

  toggleShiftAssign(employeeId: string): void {
    this.shiftAssignEmployeeId.set(this.shiftAssignEmployeeId() === employeeId ? null : employeeId);
    this.shiftAssignShiftId = '';
  }

  async assignShift(employeeId: string): Promise<void> {
    if (!this.shiftAssignShiftId) return;
    try {
      await this.organizationService.assignShift(employeeId, this.shiftAssignShiftId, new Date().toISOString());
      this.toast.success('Shift assigned.');
      this.shiftAssignEmployeeId.set(null);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to assign shift.');
    }
  }
}
