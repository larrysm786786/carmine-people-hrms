import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { AssetService, Asset, ASSET_STATUS_LABELS } from '../../core/services/asset.service';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeListItem } from '../../core/models/employee.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-assets',
  imports: [FormsModule, MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatCardModule, MatChipsModule],
  templateUrl: './assets.html',
  styleUrl: './assets.scss',
})
export class Assets {
  private readonly assetService = inject(AssetService);
  private readonly employeeService = inject(EmployeeService);
  private readonly toast = inject(ToastService);

  readonly statusLabels = ASSET_STATUS_LABELS;
  readonly assets = signal<Asset[]>([]);
  readonly employees = signal<EmployeeListItem[]>([]);
  readonly isLoading = signal(true);
  readonly displayedColumns = ['category', 'name', 'serialNumber', 'assignedTo', 'status', 'actions'];

  readonly assignTargetId = signal<string | null>(null);
  assignEmployeeId = '';

  newName = '';
  newCategory = '';
  newSerialNumber = '';
  newNotes = '';

  constructor() {
    this.load();
    this.loadEmployees();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.assets.set(await this.assetService.getList());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load assets.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadEmployees(): Promise<void> {
    try {
      const result = await this.employeeService.getList({ page: 1, pageSize: 500 });
      this.employees.set(result.items);
    } catch {
      // Employee picker is a convenience for the assign action — failing to load it must
      // never block the asset list itself from loading.
    }
  }

  async add(): Promise<void> {
    if (!this.newName.trim() || !this.newCategory.trim()) return;
    try {
      await this.assetService.create({
        name: this.newName.trim(),
        category: this.newCategory.trim(),
        serialNumber: this.newSerialNumber.trim() || null,
        notes: this.newNotes.trim() || null,
      });
      this.newName = '';
      this.newCategory = '';
      this.newSerialNumber = '';
      this.newNotes = '';
      await this.load();
      this.toast.success('Asset added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add asset.');
    }
  }

  toggleAssign(assetId: string): void {
    this.assignTargetId.set(this.assignTargetId() === assetId ? null : assetId);
    this.assignEmployeeId = '';
  }

  async confirmAssign(assetId: string): Promise<void> {
    if (!this.assignEmployeeId) return;
    try {
      await this.assetService.assign(assetId, this.assignEmployeeId);
      this.assignTargetId.set(null);
      await this.load();
      this.toast.success('Asset assigned.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to assign asset.');
    }
  }

  async unassign(assetId: string): Promise<void> {
    try {
      await this.assetService.unassign(assetId);
      await this.load();
      this.toast.success('Asset unassigned.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to unassign asset.');
    }
  }

  async markUnderRepair(asset: Asset): Promise<void> {
    await this.changeStatus(asset, 2);
  }

  async retire(asset: Asset): Promise<void> {
    await this.changeStatus(asset, 3);
  }

  async restore(asset: Asset): Promise<void> {
    await this.changeStatus(asset, 0);
  }

  private async changeStatus(asset: Asset, status: number): Promise<void> {
    try {
      await this.assetService.update(asset.id, {
        name: asset.name,
        category: asset.category,
        serialNumber: asset.serialNumber,
        notes: asset.notes,
        status,
      });
      await this.load();
      this.toast.success('Asset updated.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update asset.');
    }
  }

  async remove(assetId: string): Promise<void> {
    try {
      await this.assetService.delete(assetId);
      await this.load();
      this.toast.success('Asset removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to remove asset.');
    }
  }
}
