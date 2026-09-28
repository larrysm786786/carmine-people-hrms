import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';
import { EmployeeService } from '../../core/services/employee.service';
import { OrganizationService, Department, Designation, Location } from '../../core/services/organization.service';
import { PayrollService } from '../../core/services/payroll.service';
import { RoleService, Role } from '../../core/services/role.service';
import { EmployeeDetail, EmployeeListItem, EMPLOYMENT_STATUS_LABELS } from '../../core/models/employee.model';

export interface EmployeeFormDialogData {
  employeeId: string | null; // null = create mode
}

@Component({
  selector: 'app-employee-form-dialog',
  imports: [
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './employee-form-dialog.html',
  styleUrl: './employee-form-dialog.scss',
})
export class EmployeeFormDialog {
  private readonly employeeService = inject(EmployeeService);
  private readonly organizationService = inject(OrganizationService);
  private readonly payrollService = inject(PayrollService);
  private readonly roleService = inject(RoleService);
  private readonly authService = inject(AuthService);
  private readonly dialogRef = inject(MatDialogRef<EmployeeFormDialog>);
  protected readonly data = inject<EmployeeFormDialogData>(MAT_DIALOG_DATA);

  readonly isEditMode = this.data.employeeId !== null;
  // Only HR/SuperAdmin (payroll.manage) may view or set an employee's monthly salary —
  // everyone else who can open this dialog (e.g. a manager with employee.manage) never sees it.
  readonly canManageSalary = this.authService.hasPermission('payroll.manage');
  readonly statusLabels = EMPLOYMENT_STATUS_LABELS;
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loadErrorMessage = signal<string | null>(null);

  readonly departments = signal<Department[]>([]);
  readonly designations = signal<Designation[]>([]);
  readonly locations = signal<Location[]>([]);
  readonly roles = signal<Role[]>([]);
  readonly managers = signal<EmployeeListItem[]>([]);

  employeeCode = '';
  firstName = '';
  lastName = '';
  email = '';
  password = '';
  phoneNumber = '';
  departmentId = '';
  designationId = '';
  locationId = '';
  managerId = '';
  roleId = '';
  dateOfJoining = '';
  dateOfBirth = '';
  employmentStatus = 0;
  monthlySalary: number | null = null;

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    this.loadErrorMessage.set(null);
    try {
      const [departments, locations, roles, managersPage] = await Promise.all([
        this.organizationService.getDepartments(),
        this.organizationService.getLocations(),
        this.roleService.getRoles(),
        this.employeeService.getList({ page: 1, pageSize: 200 }),
      ]);
      this.departments.set(departments);
      this.locations.set(locations);
      this.roles.set(roles);
      this.managers.set(managersPage.items.filter((e) => e.id !== this.data.employeeId));

      if (this.data.employeeId) {
        const detail = await this.employeeService.getById(this.data.employeeId);
        this.employeeCode = detail.employeeCode;
        this.firstName = detail.firstName;
        this.lastName = detail.lastName;
        this.email = detail.email;
        this.phoneNumber = detail.phoneNumber ?? '';
        this.departmentId = detail.departmentId;
        this.designationId = detail.designationId;
        this.locationId = detail.locationId;
        this.managerId = detail.managerId ?? '';
        this.roleId = detail.roleId;
        this.dateOfJoining = detail.dateOfJoining.slice(0, 10);
        this.dateOfBirth = detail.dateOfBirth?.slice(0, 10) ?? '';
        this.employmentStatus = detail.employmentStatus;
        if (this.canManageSalary) {
          this.monthlySalary = await this.payrollService.getMonthlySalary(this.data.employeeId);
        }
        await this.loadDesignations();
      } else {
        this.dateOfJoining = new Date().toISOString().slice(0, 10);
      }
    } catch (err) {
      this.loadErrorMessage.set(errorMessage(err) ?? 'Unable to load employee data.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onDepartmentChange(): Promise<void> {
    this.designationId = '';
    await this.loadDesignations();
  }

  async loadDesignations(): Promise<void> {
    this.designations.set(this.departmentId ? await this.organizationService.getDesignations(this.departmentId) : []);
  }

  async save(): Promise<void> {
    this.errorMessage.set(null);
    this.isSaving.set(true);
    try {
      if (this.isEditMode) {
        const detail: EmployeeDetail = await this.employeeService.update(this.data.employeeId!, {
          firstName: this.firstName,
          lastName: this.lastName,
          phoneNumber: this.phoneNumber || null,
          departmentId: this.departmentId,
          designationId: this.designationId,
          locationId: this.locationId,
          managerId: this.managerId || null,
          roleId: this.roleId,
          dateOfBirth: this.dateOfBirth || null,
          employmentStatus: this.employmentStatus,
        });
        if (this.canManageSalary && this.monthlySalary !== null) {
          await this.payrollService.setMonthlySalary(this.data.employeeId!, this.monthlySalary);
        }
        this.dialogRef.close(detail);
      } else {
        const detail = await this.employeeService.create({
          employeeCode: this.employeeCode,
          firstName: this.firstName,
          lastName: this.lastName,
          email: this.email,
          password: this.password,
          phoneNumber: this.phoneNumber || null,
          departmentId: this.departmentId,
          designationId: this.designationId,
          locationId: this.locationId,
          managerId: this.managerId || null,
          roleId: this.roleId,
          dateOfJoining: this.dateOfJoining,
          dateOfBirth: this.dateOfBirth || null,
        });
        this.dialogRef.close(detail);
      }
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to save employee.');
    } finally {
      this.isSaving.set(false);
    }
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
