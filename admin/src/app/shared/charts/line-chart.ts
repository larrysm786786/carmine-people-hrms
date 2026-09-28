import { Component, computed, input } from '@angular/core';

export interface LineSeries {
  label: string;
  color: string;
  values: number[];
}

// Dependency-free SVG multi-series line chart, matching DonutChart's in-house approach
// (no charting library — ngx-charts doesn't support Angular 21's peer range yet).
@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.html',
  styleUrl: './line-chart.scss',
})
export class LineChart {
  readonly series = input.required<LineSeries[]>();
  readonly labels = input.required<string[]>();
  readonly width = input(600);
  readonly height = input(220);

  private readonly padding = 32;

  protected readonly maxValue = computed(() => {
    const allValues = this.series().flatMap((s) => s.values);
    return Math.max(1, ...allValues);
  });

  protected readonly gridLines = computed(() => {
    const max = this.maxValue();
    const steps = 4;
    return Array.from({ length: steps + 1 }, (_, i) => Math.round((max / steps) * i));
  });

  protected readonly linePaths = computed(() => {
    const w = this.width() - this.padding * 2;
    const h = this.height() - this.padding * 2;
    const max = this.maxValue();
    const labelCount = this.labels().length || 1;

    return this.series().map((s) => {
      const points = s.values.map((value, i) => {
        const x = this.padding + (i / Math.max(1, labelCount - 1)) * w;
        const y = this.padding + h - (value / max) * h;
        return { x, y, value };
      });
      const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
      return { ...s, path, points };
    });
  });

  protected readonly xAxisY = computed(() => this.height() - this.padding);
}
