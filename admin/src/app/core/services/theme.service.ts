import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'carmine-people-theme';

// Applied via [data-theme] on <html> (styles.scss's :root[data-theme='dark'] block), not
// prefers-color-scheme — this is a user-chosen app setting, not something that should silently
// follow the OS. Persisted in localStorage so it survives reloads; defaults to light to match
// the mobile app's always-light design until the user opts into dark here.
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(this.readStored());

  constructor() {
    this.apply(this.mode());
  }

  private readStored(): ThemeMode {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'dark' ? 'dark' : 'light';
    } catch {
      return 'light';
    }
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    this.apply(mode);
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // Private browsing / blocked storage — theme just won't persist across reloads.
    }
  }

  toggle(): void {
    this.setMode(this.mode() === 'dark' ? 'light' : 'dark');
  }

  private apply(mode: ThemeMode): void {
    document.documentElement.setAttribute('data-theme', mode);
  }
}
