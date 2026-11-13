import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { BrandingService } from './core/services/branding.service';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly brandingService = inject(BrandingService);
  // Injected (not just imported) so the constructor-time document.documentElement attribute
  // write happens as early as possible on every boot, not only once some later component
  // happens to reference the service — avoids a flash of the wrong theme.
  private readonly themeService = inject(ThemeService);

  constructor() {
    this.authService.restoreSession();
    this.brandingService.load();
  }
}
