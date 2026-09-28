import { Component, computed, input } from '@angular/core';

export interface BarDatum {
  label: string;
  value: number;
  secondaryValue?: number; // e.g. total headcount, shown as a lighter background bar
  color: string;
}

// Dependency-free SVG bar chart, matching DonutChart/LineChart's in-house approach.
@Component({
  selector: 'app-bar-chart',
  templateUrl: './bar-chart.html',
  styleUrl: './bar-chart.scss',
})
export class BarChart {
  readonly data = input.required<BarDatum[]>();
  readonly width = input(600);
  readonly height = input(220);

  private readonly padding = 32;

  protected readonly maxValue = computed(() => {
    const values = this.data().flatMap((d) => [d.value, d.secondaryValue ?? 0]);
    return Math.max(1, ...values);
  });

  protected readonly bars = computed(() => {
    const w = this.width() - this.padding * 2;
    const h = this.height() - this.padding * 2;
    const count = this.data().length || 1;
    const slot = w / count;
    const barWidth = Math.min(40, slot * 0.5);
    const max = this.maxValue();

    return this.data().map((d, i) => {
      const x = this.padding + slot * i + (slot - barWidth) / 2;
      const barHeight = (d.value / max) * h;
      const secondaryHeight = d.secondaryValue !== undefined ? (d.secondaryValue / max) * h : 0;
      return {
        ...d,
        x,
        barWidth,
        y: this.padding + h - barHeight,
        barHeight,
        secondaryY: this.padding + h - secondaryHeight,
        secondaryHeight,
        labelX: x + barWidth / 2,
      };
    });
  });

  protected readonly xAxisY = computed(() => this.height() - this.padding);
}
