import { Component, Input } from '@angular/core';

export type HeatmapMode = 'heatmap' | 'grid';

@Component({
  selector: 'app-pressure-heatmap',
  standalone: true,
  templateUrl: './pressure-heatmap.html',
  styleUrl: './pressure-heatmap.scss',
})
export class PressureHeatmapComponent {
  @Input({ required: true }) grid: number[][] = [];
  @Input() mode: HeatmapMode = 'heatmap';
  /** Max value the color scale tops out at — usually 10. */
  @Input() max = 10;

  get colCount(): number {
    return this.grid[0]?.length ?? 0;
  }

  /**
   * Convert a 0..max pressure value to an HSL color matching the
   * jet-like color scale shown in the screenshots (blue → green → yellow → red).
   */
  colorFor(value: number): string {
    if (value <= 0.05) return 'transparent';
    const t = Math.min(1, value / this.max);
    // Hue: 240° (blue) → 0° (red) — invert so high = red.
    const hue = (1 - t) * 240;
    return `hsl(${hue}, 95%, 50%)`;
  }

  /** Tint a cell for the grid view: pale background with the heat color. */
  cellBg(value: number): string {
    if (value <= 0.05) return '#FFFFFF';
    const t = Math.min(1, value / this.max);
    const hue = (1 - t) * 240;
    // Higher saturation for hot spots, lighter for low values.
    const lightness = 85 - t * 35;
    return `hsl(${hue}, 90%, ${lightness}%)`;
  }

  /** Border color used in grid view — slightly darker than the bg. */
  cellBorder(value: number): string {
    if (value <= 0.05) return '#E8E8E8';
    const t = Math.min(1, value / this.max);
    const hue = (1 - t) * 240;
    return `hsl(${hue}, 60%, 55%)`;
  }
}
