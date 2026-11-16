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
import {
  PayrollService,
  PayrollComplianceSettings,
  TdsSlab,
  ComplianceCalculation,
} from '../../core/services/payroll.service';
import { ToastService } from '../../core/services/toast.service';

const EMPTY_SETTINGS: PayrollComplianceSettings = {
  isPfEnabled: false,
  pfEmployeeRatePercent: 0,
  pfEmployerRatePercent: 0,
  pfWageCeiling: null,
  isEsiEnabled: false,
  esiEmployeeRatePercent: 0,
  esiEmployerRatePercent: 0,
  esiEligibilityThreshold: null,
  isTdsEnabled: false,
};

@Component({
  selector: 'app-payroll-compliance',
  imports: [FormsModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, MatCardModule],
  templateUrl: './payroll-compliance.html',
  styleUrl: './payroll-compliance.scss',
})
export class PayrollCompliance {
  private readonly payrollService = inject(PayrollService);
  private readonly toast = inject(ToastService);

  settings: PayrollComplianceSettings = { ...EMPTY_SETTINGS };
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);

  readonly tdsSlabs = signal<TdsSlab[]>([]);
  newSlab = { upToAnnualIncome: null as number | null, ratePercent: 0, sortOrder: 0 };

  calculatorGross: number | null = null;
  readonly calculatorResult = signal<ComplianceCalculation | null>(null);
  readonly isCalculating = signal(false);

  constructor() {
    this.load();
    this.loadTdsSlabs();
  }

  async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.settings = await this.payrollService.getComplianceSettings();
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load payroll compliance settings.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async save(): Promise<void> {
    this.isSaving.set(true);
    try {
      await this.payrollService.updateComplianceSettings(this.settings);
      this.toast.success('Payroll compliance settings saved.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to save settings.');
    } finally {
      this.isSaving.set(false);
    }
  }

  async loadTdsSlabs(): Promise<void> {
    try {
      this.tdsSlabs.set(await this.payrollService.getTdsSlabs());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load TDS slabs.');
    }
  }

  async addTdsSlab(): Promise<void> {
    try {
      await this.payrollService.addTdsSlab({
        upToAnnualIncome: this.newSlab.upToAnnualIncome,
        ratePercent: this.newSlab.ratePercent,
        sortOrder: this.tdsSlabs().length,
      });
      this.newSlab = { upToAnnualIncome: null, ratePercent: 0, sortOrder: 0 };
      await this.loadTdsSlabs();
      this.toast.success('TDS slab added.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to add TDS slab.');
    }
  }

  async removeTdsSlab(id: string): Promise<void> {
    try {
      await this.payrollService.deleteTdsSlab(id);
      await this.loadTdsSlabs();
      this.toast.success('TDS slab removed.');
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to remove TDS slab.');
    }
  }

  async calculate(): Promise<void> {
    if (this.calculatorGross === null || this.calculatorGross < 0) return;
    this.isCalculating.set(true);
    try {
      this.calculatorResult.set(await this.payrollService.calculateCompliance(this.calculatorGross));
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to calculate.');
    } finally {
      this.isCalculating.set(false);
    }
  }
}
