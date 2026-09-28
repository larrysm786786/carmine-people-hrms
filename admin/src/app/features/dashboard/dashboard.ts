import { errorMessage } from '../../core/utils/error-message';
import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { Dashboard as DashboardData, DashboardService } from '../../core/services/dashboard.service';
import { AttendanceAnomaly, AttendanceAnomalyService } from '../../core/services/attendance-anomaly.service';
import { LeavePrediction, LeavePredictionService } from '../../core/services/leave-prediction.service';
import { DonutChart, DonutSegment } from '../../shared/charts/donut-chart';
import { LineChart, LineSeries } from '../../shared/charts/line-chart';
import { BarChart, BarDatum } from '../../shared/charts/bar-chart';

interface KpiCard {
  label: string;
  value: number;
  icon: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, MatCardModule, MatIconModule, MatTableModule, MatChipsModule, DonutChart, LineChart, BarChart],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly dashboardService = inject(DashboardService);
  private readonly attendanceAnomalyService = inject(AttendanceAnomalyService);
  private readonly leavePredictionService = inject(LeavePredictionService);

  readonly data = signal<DashboardData | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly anomalies = signal<AttendanceAnomaly[]>([]);
  readonly anomaliesLoading = signal(true);

  readonly leavePredictions = signal<LeavePrediction[]>([]);
  readonly leavePredictionsLoading = signal(true);
  readonly leavePredictionMonthName = computed(() => {
    const predictions = this.leavePredictions();
    if (predictions.length === 0) return '';
    return new Date(2000, predictions[0].targetMonth - 1, 1).toLocaleDateString(undefined, { month: 'long' });
  });

  readonly kpis = computed<KpiCard[]>(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { label: 'Total employees', value: d.kpis.totalEmployees, icon: 'group' },
      { label: 'Present today', value: d.kpis.presentToday, icon: 'check_circle' },
      { label: 'Absent', value: d.kpis.absentToday, icon: 'cancel' },
      { label: 'On leave', value: d.kpis.onLeaveToday, icon: 'beach_access' },
      { label: 'Late', value: d.kpis.lateToday, icon: 'schedule' },
      { label: 'Work from home', value: d.kpis.wfhToday, icon: 'home_work' },
    ];
  });

  // Only the 4 brand colors are permitted anywhere in the app (Gray #f7f7f7, Red #cd0040,
  // Black #000000, White #ffffff) — 5 chart segments/series are differentiated using those
  // 4 hues plus opacity variants of red/black, rather than introducing off-brand hex values.
  readonly attendanceDonut = computed<DonutSegment[]>(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { label: 'Present', value: d.kpis.presentToday, color: '#cd0040' },
      { label: 'Absent', value: d.kpis.absentToday, color: '#000000' },
      { label: 'Late', value: d.kpis.lateToday, color: 'rgba(205, 0, 64, 0.5)' },
      { label: 'WFH', value: d.kpis.wfhToday, color: '#f7f7f7' },
      { label: 'On leave', value: d.kpis.onLeaveToday, color: 'rgba(0, 0, 0, 0.4)' },
    ];
  });

  readonly attendanceTrendLabels = computed(() => (this.data()?.attendanceTrend ?? []).map((p) => this.shortDate(p.date)));
  readonly attendanceTrendSeries = computed<LineSeries[]>(() => {
    const points = this.data()?.attendanceTrend ?? [];
    return [
      { label: 'Present', color: '#cd0040', values: points.map((p) => p.present) },
      { label: 'Absent', color: '#000000', values: points.map((p) => p.absent) },
      { label: 'Late', color: 'rgba(205, 0, 64, 0.5)', values: points.map((p) => p.late) },
    ];
  });

  readonly departmentBars = computed<BarDatum[]>(() =>
    (this.data()?.departmentAttendance ?? []).map((d) => ({
      label: d.departmentName,
      value: d.present,
      secondaryValue: d.total,
      color: '#cd0040',
    })),
  );

  readonly leaveTrendLabels = computed(() => (this.data()?.leaveTrend ?? []).map((p) => p.month));
  readonly leaveTrendBars = computed<BarDatum[]>(() =>
    (this.data()?.leaveTrend ?? []).map((p) => ({ label: p.month, value: p.applications, color: '#cd0040' })),
  );

  readonly workingHoursLabels = computed(() => (this.data()?.workingHours ?? []).map((p) => this.shortDate(p.date)));
  readonly workingHoursSeries = computed<LineSeries[]>(() => {
    const points = this.data()?.workingHours ?? [];
    return [{ label: 'Avg hours', color: '#cd0040', values: points.map((p) => p.averageHours) }];
  });

  readonly recentAttendanceColumns = ['employeeName', 'date', 'status', 'checkInAt', 'checkOutAt'];
  readonly pendingApprovalColumns = ['type', 'employeeName', 'summary', 'requestedAt'];
  readonly recentEmployeeColumns = ['fullName', 'department', 'designation', 'dateOfJoining'];
  readonly recentAnnouncementColumns = ['title', 'publishAt'];

  constructor() {
    this.load();
    this.loadAnomalies();
    this.loadLeavePredictions();
  }

  private async load(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      this.data.set(await this.dashboardService.get());
    } catch (err) {
      this.errorMessage.set(errorMessage(err) ?? 'Unable to load dashboard.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadAnomalies(): Promise<void> {
    this.anomaliesLoading.set(true);
    try {
      this.anomalies.set(await this.attendanceAnomalyService.getAnomalies());
    } catch {
      // Best-effort widget — a failed anomaly fetch shouldn't block the rest of the
      // dashboard from rendering; it simply shows nothing in that card.
      this.anomalies.set([]);
    } finally {
      this.anomaliesLoading.set(false);
    }
  }

  private async loadLeavePredictions(): Promise<void> {
    this.leavePredictionsLoading.set(true);
    try {
      this.leavePredictions.set(await this.leavePredictionService.getPredictions());
    } catch {
      this.leavePredictions.set([]);
    } finally {
      this.leavePredictionsLoading.set(false);
    }
  }

  private shortDate(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}
