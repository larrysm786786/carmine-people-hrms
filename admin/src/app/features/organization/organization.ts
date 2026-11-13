import { errorMessage } from '../../core/utils/error-message';
import { dateToIso, isoToDate } from '../../core/utils/date-format';
import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  OrganizationService,
  Department,
  Designation,
  Location,
  Shift,
  EmployeeShift,
  SHIFT_TYPE_LABELS,
} from '../../core/services/organization.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-organization',
  imports: [
    CommonModule,
    FormsModule,
    MatTabsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
  ],
  templateUrl: './organization.html',
  styleUrl: './organization.scss',
})
export class Organization {
  private readonly organizationService = inject(OrganizationService);
  private readonly toast = inject(ToastService);

  readonly departments = signal<Department[]>([]);
  readonly designations = signal<Designation[]>([]);
  readonly locations = signal<Location[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly employeeShifts = signal<EmployeeShift[]>([]);
  readonly employees = signal<EmployeeListItem[]>([]);
  readonly shiftTypeLabels = SHIFT_TYPE_LABELS;
  readonly isLoading = signal(false);

  newDepartment = { name: '', code: '', description: '', managerId: '', isActive: true };
  editingDepartmentId: string | null = null;
  editDepartment = { name: '', code: '', description: '', managerId: '', isActive: true };

  newDesignation = { title: '', description: '', departmentId: '', isActive: true };
  editingDesignationId: string | null = null;
  editDesignation = { title: '', description: '', departmentId: '', isActive: true };

  newLocation = { name: '', address: '', latitude: 0, longitude: 0, geofenceRadiusMeters: 100, isActive: true, officeWifiSsid: '' as string | null };
  editingLocationId: string | null = null;
  editLocation = { name: '', address: '', latitude: 0, longitude: 0, geofenceRadiusMeters: 100, isActive: true, officeWifiSsid: '' as string | null };

  newShift = { name: '', type: 0, startTime: '09:00:00', endTime: '18:00:00', gracePeriodMinutes: 10, breakMinutes: 30, requiredWorkMinutes: 540 };
  editingShiftId: string | null = null;
  editShift = { name: '', type: 0, startTime: '', endTime: '', gracePeriodMinutes: 0, breakMinutes: 0, requiredWorkMinutes: 0 };

  assignEmployeeId = '';
  assignShiftId = '';
  assignEffectiveFrom = new Date().toISOString().slice(0, 10);

  get assignEffectiveFromDate(): Date | null { return isoToDate(this.assignEffectiveFrom); }
  set assignEffectiveFromDate(value: Date | null) { this.assignEffectiveFrom = dateToIso(value); }

  private readonly employeeService = inject(EmployeeService);

  constructor() {
    this.loadAll();
  }

  async loadAll(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [departments, designations, locations, shifts, employeeShifts, employeesPage] = await Promise.all([
        this.organizationService.getDepartments(),
        this.organizationService.getDesignations(),
        this.organizationService.getLocations(),
        this.organizationService.getShifts(),
        this.organizationService.getEmployeeShifts(),
        this.employeeService.getList({ page: 1, pageSize: 200 }),
      ]);
      this.departments.set(departments);
      this.designations.set(designations);
      this.locations.set(locations);
      this.shifts.set(shifts);
      this.employeeShifts.set(employeeShifts);
      this.employees.set(employeesPage.items);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load organization data.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async addDepartment(): Promise<void> {
    if (!this.newDepartment.name.trim()) return;
    try {
      await this.organizationService.createDepartment({
        name: this.newDepartment.name.trim(),
        code: this.newDepartment.code.trim() || null,
        description: this.newDepartment.description.trim() || null,
        managerId: this.newDepartment.managerId || null,
        isActive: this.newDepartment.isActive,
      });
      this.newDepartment = { name: '', code: '', description: '', managerId: '', isActive: true };
      this.departments.set(await this.organizationService.getDepartments());
      this.toast.success('Department added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add department.');
    }
  }

  startEditDepartment(department: Department): void {
    this.editingDepartmentId = department.id;
    this.editDepartment = {
      name: department.name,
      code: department.code ?? '',
      description: department.description ?? '',
      managerId: department.managerId ?? '',
      isActive: department.isActive,
    };
  }

  cancelEditDepartment(): void {
    this.editingDepartmentId = null;
  }

  async saveEditDepartment(): Promise<void> {
    if (!this.editingDepartmentId) return;
    try {
      await this.organizationService.updateDepartment(this.editingDepartmentId, {
        name: this.editDepartment.name,
        code: this.editDepartment.code.trim() || null,
        description: this.editDepartment.description.trim() || null,
        managerId: this.editDepartment.managerId || null,
        isActive: this.editDepartment.isActive,
      });
      this.departments.set(await this.organizationService.getDepartments());
      this.editingDepartmentId = null;
      this.toast.success('Department updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update department.');
    }
  }

  async removeDepartment(id: string): Promise<void> {
    try {
      await this.organizationService.deleteDepartment(id);
      this.departments.set(await this.organizationService.getDepartments());
      this.toast.success('Department removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to delete department.');
    }
  }

  async addDesignation(): Promise<void> {
    if (!this.newDesignation.title.trim() || !this.newDesignation.departmentId) return;
    try {
      await this.organizationService.createDesignation({
        title: this.newDesignation.title.trim(),
        description: this.newDesignation.description.trim() || null,
        departmentId: this.newDesignation.departmentId,
        isActive: this.newDesignation.isActive,
      });
      this.newDesignation = { title: '', description: '', departmentId: '', isActive: true };
      this.designations.set(await this.organizationService.getDesignations());
      this.toast.success('Designation added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add designation.');
    }
  }

  startEditDesignation(designation: Designation): void {
    this.editingDesignationId = designation.id;
    this.editDesignation = {
      title: designation.title,
      description: designation.description ?? '',
      departmentId: designation.departmentId,
      isActive: designation.isActive,
    };
  }

  cancelEditDesignation(): void {
    this.editingDesignationId = null;
  }

  async saveEditDesignation(): Promise<void> {
    if (!this.editingDesignationId) return;
    try {
      await this.organizationService.updateDesignation(this.editingDesignationId, {
        title: this.editDesignation.title,
        description: this.editDesignation.description.trim() || null,
        departmentId: this.editDesignation.departmentId,
        isActive: this.editDesignation.isActive,
      });
      this.designations.set(await this.organizationService.getDesignations());
      this.editingDesignationId = null;
      this.toast.success('Designation updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update designation.');
    }
  }

  async removeDesignation(id: string): Promise<void> {
    try {
      await this.organizationService.deleteDesignation(id);
      this.designations.set(await this.organizationService.getDesignations());
      this.toast.success('Designation removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to delete designation.');
    }
  }

  async addLocation(): Promise<void> {
    if (!this.newLocation.name.trim()) return;
    try {
      await this.organizationService.createLocation({ ...this.newLocation, officeWifiSsid: this.newLocation.officeWifiSsid?.trim() || null });
      this.newLocation = { name: '', address: '', latitude: 0, longitude: 0, geofenceRadiusMeters: 100, isActive: true, officeWifiSsid: '' };
      this.locations.set(await this.organizationService.getLocations());
      this.toast.success('Location added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add location.');
    }
  }

  startEditLocation(location: Location): void {
    this.editingLocationId = location.id;
    this.editLocation = {
      name: location.name,
      address: location.address ?? '',
      latitude: location.latitude,
      longitude: location.longitude,
      geofenceRadiusMeters: location.geofenceRadiusMeters,
      isActive: location.isActive,
      officeWifiSsid: location.officeWifiSsid ?? '',
    };
  }

  cancelEditLocation(): void {
    this.editingLocationId = null;
  }

  async saveEditLocation(): Promise<void> {
    if (!this.editingLocationId) return;
    try {
      await this.organizationService.updateLocation(this.editingLocationId, { ...this.editLocation, officeWifiSsid: this.editLocation.officeWifiSsid?.trim() || null });
      this.locations.set(await this.organizationService.getLocations());
      this.editingLocationId = null;
      this.toast.success('Location updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update location.');
    }
  }

  async removeLocation(id: string): Promise<void> {
    try {
      await this.organizationService.deleteLocation(id);
      this.locations.set(await this.organizationService.getLocations());
      this.toast.success('Location removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to delete location.');
    }
  }

  async addShift(): Promise<void> {
    if (!this.newShift.name.trim()) return;
    try {
      await this.organizationService.createShift({ ...this.newShift });
      this.newShift = { name: '', type: 0, startTime: '09:00:00', endTime: '18:00:00', gracePeriodMinutes: 10, breakMinutes: 30, requiredWorkMinutes: 540 };
      this.shifts.set(await this.organizationService.getShifts());
      this.toast.success('Shift added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add shift.');
    }
  }

  startEditShift(shift: Shift): void {
    this.editingShiftId = shift.id;
    this.editShift = {
      name: shift.name,
      type: shift.type,
      startTime: shift.startTime,
      endTime: shift.endTime,
      gracePeriodMinutes: shift.gracePeriodMinutes,
      breakMinutes: shift.breakMinutes,
      requiredWorkMinutes: shift.requiredWorkMinutes,
    };
  }

  cancelEditShift(): void {
    this.editingShiftId = null;
  }

  async saveEditShift(): Promise<void> {
    if (!this.editingShiftId) return;
    try {
      await this.organizationService.updateShift(this.editingShiftId, { ...this.editShift });
      this.shifts.set(await this.organizationService.getShifts());
      this.editingShiftId = null;
      this.toast.success('Shift updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update shift.');
    }
  }

  async assignShift(): Promise<void> {
    if (!this.assignEmployeeId || !this.assignShiftId) return;
    try {
      await this.organizationService.assignShift(this.assignEmployeeId, this.assignShiftId, new Date(this.assignEffectiveFrom).toISOString());
      this.employeeShifts.set(await this.organizationService.getEmployeeShifts());
      this.assignEmployeeId = '';
      this.assignShiftId = '';
      this.toast.success('Shift assigned.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to assign shift.');
    }
  }

  employeeName(employeeId: string): string {
    const e = this.employees().find((x) => x.id === employeeId);
    return e ? `${e.firstName} ${e.lastName}` : employeeId;
  }
}
