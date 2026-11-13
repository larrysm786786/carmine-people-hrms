import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthService } from '../../core/services/auth.service';
import { NotificationPreferenceService, NotificationPreference } from '../../core/services/notification-preference.service';
import { BrandingService, Branding, COMMON_TIME_ZONES } from '../../core/services/branding.service';
import { ThemeService } from '../../core/services/theme.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';

const NOTIFICATION_CATEGORIES = ['leave', 'attendance', 'announcement'];

@Component({
  selector: 'app-settings',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
  ],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings {
  protected readonly authService = inject(AuthService);
  protected readonly brandingService = inject(BrandingService);
  protected readonly themeService = inject(ThemeService);
  private readonly notificationPreferenceService = inject(NotificationPreferenceService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly categories = NOTIFICATION_CATEGORIES;
  readonly timeZones = COMMON_TIME_ZONES;
  readonly preferences = signal<NotificationPreference[]>([]);
  readonly brandingForm = signal<Branding>(this.brandingService.branding());
  readonly brandingSaving = signal(false);
  readonly brandingSaved = signal(false);

  constructor() {
    this.loadPreferences();
    this.brandingForm.set(this.brandingService.branding());
  }

  get isSuperAdmin(): boolean {
    return this.authService.profile()?.roleName === 'SuperAdmin';
  }

  updateBrandingField<K extends keyof Branding>(field: K, value: Branding[K]): void {
    this.brandingForm.set({ ...this.brandingForm(), [field]: value });
  }

  async saveBranding(): Promise<void> {
    this.brandingSaving.set(true);
    this.brandingSaved.set(false);
    try {
      await this.brandingService.update(this.brandingForm());
      this.brandingSaved.set(true);
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to save branding.');
    } finally {
      this.brandingSaving.set(false);
    }
  }

  async loadPreferences(): Promise<void> {
    try {
      this.preferences.set(await this.notificationPreferenceService.getPreferences());
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to load notification preferences.');
    }
  }

  isEnabled(category: string): boolean {
    const pref = this.preferences().find((p) => p.category === category && p.channel === 1);
    return pref?.isEnabled ?? true;
  }

  async togglePreference(category: string): Promise<void> {
    const nextValue = !this.isEnabled(category);
    try {
      await this.notificationPreferenceService.setPreference(1, category, nextValue);
      await this.loadPreferences();
    } catch (err) {
      this.toast.error(errorMessage(err) ?? 'Unable to update notification preference.');
    }
  }

  async logout(): Promise<void> {
    await this.authService.logout();
    this.router.navigate(['/login']);
  }
}
