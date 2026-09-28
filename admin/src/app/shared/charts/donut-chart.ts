import { Component, computed, input } from '@angular/core';

export interface DonutSegment {
  label: string;
  value: number;
  color: string;
}

// A dependency-free SVG donut chart. Built in-house instead of pulling in a
// charting library (ngx-charts doesn't yet support Angular 21's peer range),
// so this stays on stable ground across framework upgrades.
@Component({
  selector: 'app-donut-chart',
  templateUrl: './donut-chart.html',
  styleUrl: './donut-chart.scss',
})
export class DonutChart {
  readonly segments = input.required<DonutSegment[]>();
  readonly size = input(160);

  private readonly radius = 15.915; // circumference works out to exactly 100 for percentage-based stroke-dasharray

  protected readonly total = computed(() => this.segments().reduce((sum, s) => sum + s.value, 0) || 1);

  protected readonly arcs = computed(() => {
    let cumulative = 0;
    return this.segments().map((segment) => {
      const percent = (segment.value / this.total()) * 100;
      const dashArray = `${percent} ${100 - percent}`;
      const dashOffset = 25 - cumulative; // start at 12 o'clock, go clockwise
      cumulative += percent;
      return { ...segment, percent, dashArray, dashOffset };
    });
  });

  protected readonly r = this.radius;
}
