import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { OnboardingService, OnboardingTaskTemplate, OnboardingTask } from '../../core/services/onboarding.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-onboarding',
  imports: [
    FormsModule, MatTableModule, MatTabsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatCheckboxModule,
  ],
  templateUrl: './onboarding.html',
  styleUrl: './onboarding.scss',
})
export class Onboarding {
  private readonly onboardingService = inject(OnboardingService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly templates = signal<OnboardingTaskTemplate[]>([]);
  readonly templateColumns = ['title', 'description', 'sortOrder', 'actions'];
  newTemplate = { title: '', description: '', sortOrder: 0 };
  editingTemplateId: string | null = null;
  editTemplate = { title: '', description: '', sortOrder: 0 };

  readonly employees = signal<EmployeeListItem[]>([]);
  selectedEmployeeId = '';
  readonly tasks = signal<OnboardingTask[]>([]);
  readonly taskColumns = ['title', 'description', 'completed', 'actions'];
  readonly isLoadingTasks = signal(false);
  readonly isApplying = signal(false);
  newTask = { title: '', description: '' };

  constructor() {
    this.loadTemplates();
    this.loadEmployees();
  }

  async loadTemplates(): Promise<void> {
    try {
      this.templates.set(await this.onboardingService.getTemplates());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load checklist templates.');
    }
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee picker is a convenience; failing to load it must never block the page.
    }
  }

  async addTemplate(): Promise<void> {
    if (!this.newTemplate.title.trim()) return;
    try {
      await this.onboardingService.createTemplate({
        title: this.newTemplate.title.trim(),
        description: this.newTemplate.description.trim() || null,
        sortOrder: this.newTemplate.sortOrder,
      });
      this.newTemplate = { title: '', description: '', sortOrder: 0 };
      await this.loadTemplates();
      this.toast.success('Checklist template added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add template.');
    }
  }

  startEditTemplate(template: OnboardingTaskTemplate): void {
    this.editingTemplateId = template.id;
    this.editTemplate = { title: template.title, description: template.description ?? '', sortOrder: template.sortOrder };
  }

  cancelEditTemplate(): void {
    this.editingTemplateId = null;
  }

  async saveEditTemplate(): Promise<void> {
    if (!this.editingTemplateId || !this.editTemplate.title.trim()) return;
    try {
      await this.onboardingService.updateTemplate(this.editingTemplateId, {
        title: this.editTemplate.title.trim(),
        description: this.editTemplate.description.trim() || null,
        sortOrder: this.editTemplate.sortOrder,
      });
      this.editingTemplateId = null;
      await this.loadTemplates();
      this.toast.success('Checklist template updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update template.');
    }
  }

  async removeTemplate(id: string): Promise<void> {
    try {
      await this.onboardingService.deleteTemplate(id);
      await this.loadTemplates();
      this.toast.success('Checklist template removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to remove template.');
    }
  }

  async onEmployeeChange(): Promise<void> {
    if (!this.selectedEmployeeId) {
      this.tasks.set([]);
      return;
    }
    this.isLoadingTasks.set(true);
    try {
      this.tasks.set(await this.onboardingService.getTasksForEmployee(this.selectedEmployeeId));
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load checklist.');
    } finally {
      this.isLoadingTasks.set(false);
    }
  }

  async applyTemplates(): Promise<void> {
    if (!this.selectedEmployeeId) return;
    this.isApplying.set(true);
    try {
      const added = await this.onboardingService.applyTemplates(this.selectedEmployeeId);
      await this.onEmployeeChange();
      this.toast.success(added > 0 ? `Added ${added} checklist item(s).` : 'All templates are already on this checklist.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to apply templates.');
    } finally {
      this.isApplying.set(false);
    }
  }

  async addTask(): Promise<void> {
    if (!this.selectedEmployeeId || !this.newTask.title.trim()) return;
    try {
      await this.onboardingService.addTask(this.selectedEmployeeId, {
        title: this.newTask.title.trim(),
        description: this.newTask.description.trim() || null,
        sortOrder: this.tasks().length,
      });
      this.newTask = { title: '', description: '' };
      await this.onEmployeeChange();
      this.toast.success('Checklist item added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add checklist item.');
    }
  }

  async toggleComplete(task: OnboardingTask): Promise<void> {
    try {
      if (task.isCompleted) {
        await this.onboardingService.reopen(task.id);
      } else {
        await this.onboardingService.complete(task.id);
      }
      await this.onEmployeeChange();
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update checklist item.');
    }
  }

  async removeTask(id: string): Promise<void> {
    try {
      await this.onboardingService.deleteTask(id);
      await this.onEmployeeChange();
      this.toast.success('Checklist item removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to remove checklist item.');
    }
  }
}
