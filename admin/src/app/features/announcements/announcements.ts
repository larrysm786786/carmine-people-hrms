import { errorMessage } from '../../core/utils/error-message';
import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { AnnouncementService, Announcement, AUDIENCE_TYPE_LABELS } from '../../core/services/announcement.service';
import { OrganizationService, Department, Location } from '../../core/services/organization.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-announcements',
  imports: [CommonModule, FormsModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule],
  templateUrl: './announcements.html',
  styleUrl: './announcements.scss',
})
export class Announcements {
  private readonly announcementService = inject(AnnouncementService);
  private readonly organizationService = inject(OrganizationService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly announcements = signal<Announcement[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly locations = signal<Location[]>([]);
  readonly employees = signal<EmployeeListItem[]>([]);
  readonly audienceTypeLabels = AUDIENCE_TYPE_LABELS;
  readonly isPublishing = signal(false);

  newTitle = '';
  newBody = '';
  newAudienceType = 0;
  newAudienceIds: string[] = [];
  newPublishAt = new Date().toISOString().slice(0, 16);
  newExpiresAt = '';
  imageFile: File | null = null;
  attachmentFile: File | null = null;

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    try {
      const [announcements, departments, locations, employeesPage] = await Promise.all([
        this.announcementService.getFeed(),
        this.organizationService.getDepartments(),
        this.organizationService.getLocations(),
        this.employeeService.getList({ page: 1, pageSize: 200 }),
      ]);
      this.announcements.set(announcements);
      this.departments.set(departments);
      this.locations.set(locations);
      this.employees.set(employeesPage.items);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load announcements.');
    }
  }

  onAudienceTypeChange(): void {
    this.newAudienceIds = [];
  }

  // The input is cleared after reading so picking the same file again (e.g. after
  // removing it) still fires a change event.
  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.imageFile = input.files?.[0] ?? null;
    input.value = '';
  }

  onAttachmentSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.attachmentFile = input.files?.[0] ?? null;
    input.value = '';
  }

  async publish(): Promise<void> {
    if (!this.newTitle.trim() || !this.newBody.trim()) return;
    this.isPublishing.set(true);
    try {
      const imageDocumentId = this.imageFile ? await this.announcementService.uploadMedia(this.imageFile) : null;
      const attachmentDocumentId = this.attachmentFile ? await this.announcementService.uploadMedia(this.attachmentFile) : null;

      await this.announcementService.create({
        title: this.newTitle.trim(),
        body: this.newBody.trim(),
        audienceType: this.newAudienceType,
        audienceIds: this.newAudienceType === 0 ? null : this.newAudienceIds,
        publishAt: new Date(this.newPublishAt).toISOString(),
        expiresAt: this.newExpiresAt ? new Date(this.newExpiresAt).toISOString() : null,
        imageDocumentId,
        attachmentDocumentId,
      });

      this.newTitle = '';
      this.newBody = '';
      this.newAudienceType = 0;
      this.newAudienceIds = [];
      this.newPublishAt = new Date().toISOString().slice(0, 16);
      this.newExpiresAt = '';
      this.imageFile = null;
      this.attachmentFile = null;
      await this.load();
      this.toast.success('Announcement published.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to publish announcement.');
    } finally {
      this.isPublishing.set(false);
    }
  }
}
