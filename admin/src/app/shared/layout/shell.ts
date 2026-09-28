import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { BrandingService } from '../../core/services/branding.service';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatSidenavModule, MatListModule, MatToolbarModule, MatIconModule, MatButtonModule, MatMenuModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly authService = inject(AuthService);
  protected readonly brandingService = inject(BrandingService);
  private readonly router = inject(Router);
  private readonly breakpointObserver = inject(BreakpointObserver);

  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe(Breakpoints.Handset).pipe(map((result) => result.matches)),
    { initialValue: false },
  );
  protected readonly sidenavOpened = signal(true);

  toggleSidenav(): void {
    this.sidenavOpened.set(!this.sidenavOpened());
  }

  readonly navItems = [
    { label: 'Dashboard', icon: 'dashboard', route: '/' },
    { label: 'Employees', icon: 'group', route: '/employees' },
    { label: 'Attendance', icon: 'event_available', route: '/attendance' },
    { label: 'Leave', icon: 'beach_access', route: '/leave' },
    { label: 'Organization', icon: 'apartment', route: '/organization' },
    { label: 'Holidays', icon: 'celebration', route: '/holidays' },
    { label: 'Announcements', icon: 'campaign', route: '/announcements' },
    { label: 'Reports', icon: 'summarize', route: '/reports' },
    { label: 'Audit log', icon: 'fact_check', route: '/audit' },
    { label: 'AI Insights', icon: 'smart_toy', route: '/ai-insights' },
    { label: 'Roles', icon: 'admin_panel_settings', route: '/roles' },
    { label: 'Settings', icon: 'settings', route: '/settings' },
  ];

  async logout(): Promise<void> {
    await this.authService.logout();
    this.router.navigate(['/login']);
  }
}
