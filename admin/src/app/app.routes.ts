import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    loadComponent: () => import('./shared/layout/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard) },
      { path: 'employees', loadComponent: () => import('./features/employees/employees').then((m) => m.Employees) },
      { path: 'attendance', loadComponent: () => import('./features/attendance/attendance').then((m) => m.Attendance) },
      { path: 'leave', loadComponent: () => import('./features/leave/leave').then((m) => m.Leave) },
      { path: 'organization', loadComponent: () => import('./features/organization/organization').then((m) => m.Organization) },
      { path: 'holidays', loadComponent: () => import('./features/holidays/holidays').then((m) => m.Holidays) },
      { path: 'assets', loadComponent: () => import('./features/assets/assets').then((m) => m.Assets) },
      { path: 'expenses', loadComponent: () => import('./features/expenses/expenses').then((m) => m.Expenses) },
      { path: 'onboarding', loadComponent: () => import('./features/onboarding/onboarding').then((m) => m.Onboarding) },
      { path: 'pulse-surveys', loadComponent: () => import('./features/pulse-surveys/pulse-surveys').then((m) => m.PulseSurveys) },
      { path: 'signature-documents', loadComponent: () => import('./features/signature-documents/signature-documents').then((m) => m.SignatureDocuments) },
      { path: 'performance-reviews', loadComponent: () => import('./features/performance-reviews/performance-reviews').then((m) => m.PerformanceReviews) },
      { path: 'recruitment', loadComponent: () => import('./features/recruitment/recruitment').then((m) => m.Recruitment) },
      { path: 'announcements', loadComponent: () => import('./features/announcements/announcements').then((m) => m.Announcements) },
      { path: 'reports', loadComponent: () => import('./features/reports/reports').then((m) => m.Reports) },
      { path: 'audit', loadComponent: () => import('./features/audit/audit').then((m) => m.Audit) },
      { path: 'ai-insights', loadComponent: () => import('./features/ai-insights/ai-insights').then((m) => m.AiInsights) },
      { path: 'roles', loadComponent: () => import('./features/roles/roles').then((m) => m.Roles) },
      { path: 'settings', loadComponent: () => import('./features/settings/settings').then((m) => m.Settings) },
    ],
  },
  { path: '**', redirectTo: '' },
];
