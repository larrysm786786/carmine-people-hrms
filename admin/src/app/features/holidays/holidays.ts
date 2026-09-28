import { errorMessage } from '../../core/utils/error-message';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatCardModule } from '@angular/material/card';
import { HolidayService, Holiday } from '../../core/services/holiday.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-holidays',
  imports: [FormsModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, MatCardModule],
  templateUrl: './holidays.html',
  styleUrl: './holidays.scss',
})
export class Holidays {
  private readonly holidayService = inject(HolidayService);
  private readonly toast = inject(ToastService);

  readonly holidays = signal<Holiday[]>([]);
  readonly displayedColumns = ['name', 'date', 'isOptional', 'actions'];

  newName = '';
  newDescription = '';
  newDate = '';
  newIsOptional = false;

  constructor() {
    this.load();
  }

  async load(): Promise<void> {
    try {
      this.holidays.set(await this.holidayService.getList());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load holidays.');
    }
  }

  async add(): Promise<void> {
    if (!this.newName.trim() || !this.newDate) return;
    try {
      await this.holidayService.create(this.newName.trim(), this.newDescription.trim() || null, this.newDate, this.newIsOptional);
      this.newName = '';
      this.newDescription = '';
      this.newDate = '';
      this.newIsOptional = false;
      await this.load();
      this.toast.success('Holiday added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add holiday.');
    }
  }

  async remove(id: string): Promise<void> {
    try {
      await this.holidayService.delete(id);
      await this.load();
      this.toast.success('Holiday removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to remove holiday.');
    }
  }
}
