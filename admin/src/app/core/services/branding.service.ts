import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEnvelope } from '../models/api-envelope.model';
import { API_BASE_URL } from './api-config';

export interface Branding {
  companyName: string;
  appName: string;
  logoUrl: string | null;
  colorGray: string;
  colorPrimary: string;
  colorBlack: string;
  colorWhite: string;
  gradientCss: string;
  timeZoneId: string;
  freeLatesBeforeHalfDay: number;
}

// A small set of common IANA time zones for the settings dropdown — the backend accepts
// any valid IANA/Windows ID via TimeZoneInfo.FindSystemTimeZoneById, this list is just a
// convenience shortlist rather than an exhaustive picker.
export const COMMON_TIME_ZONES = [
  'UTC',
  'Asia/Kolkata',
  'Asia/Dubai',
  'Asia/Singapore',
  'Europe/London',
  'Europe/Berlin',
  'America/New_York',
  'America/Chicago',
  'America/Los_Angeles',
  'Australia/Sydney',
];

const DEFAULT_BRANDING: Branding = {
  companyName: 'Carmine People',
  appName: 'Carmine People',
  logoUrl: null,
  colorGray: '#f7f7f7',
  colorPrimary: '#cd0040',
  colorBlack: '#000000',
  colorWhite: '#ffffff',
  gradientCss: 'linear-gradient(90deg, #cd0040 0%, #3a1420 52%, #000000 100%)',
  timeZoneId: 'UTC',
  freeLatesBeforeHalfDay: 2,
};

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly http = inject(HttpClient);
  readonly branding = signal<Branding>(DEFAULT_BRANDING);

  async load(): Promise<void> {
    try {
      const response = await firstValueFrom(this.http.get<ApiEnvelope<Branding>>(`${API_BASE_URL}/branding`));
      if (response.success) {
        this.branding.set(response.data);
        this.applyToDocument(response.data);
      }
    } catch {
      // Backend unreachable or not yet seeded: keep the compiled-in defaults.
      this.applyToDocument(DEFAULT_BRANDING);
    }
  }

  async update(branding: Branding): Promise<void> {
    const response = await firstValueFrom(this.http.put<ApiEnvelope<Branding>>(`${API_BASE_URL}/branding`, branding));
    if (!response.success) throw new Error(response.error.message);
    this.branding.set(response.data);
    this.applyToDocument(response.data);
  }

  // Brand colors (carmine/gray/white/black) are fixed in styles.scss, matching the mobile
  // app's design system exactly — they are intentionally NOT applied from Branding here, so
  // this only updates the document title. (colorPrimary/colorGray/colorBlack/colorWhite/
  // gradientCss still round-trip through the API/DB for backward compatibility, but the UI
  // no longer exposes them for editing and the app no longer applies them at runtime.)
  private applyToDocument(b: Branding): void {
    document.title = b.appName;
  }
}
