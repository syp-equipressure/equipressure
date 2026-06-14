import { Component, Input, ViewChild, ElementRef, AfterViewInit, SimpleChanges, OnChanges, ChangeDetectionStrategy } from '@angular/core';

export type HeatmapMode = 'heatmap' | 'grid';

@Component({
  selector: 'app-pressure-heatmap',
  standalone: true,
  templateUrl: './pressure-heatmap.html',
  styleUrl: './pressure-heatmap.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PressureHeatmapComponent implements AfterViewInit, OnChanges {
  @Input({ required: true }) grid: number[][] = [];
  @Input() mode: HeatmapMode = 'heatmap';
  @Input() max = 10;

  @ViewChild('canvas', { static: false }) canvasRef?: ElementRef<HTMLCanvasElement>;

  ngAfterViewInit(): void {
    this.renderCanvas();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['grid'] || changes['mode'] || changes['max']) {
      requestAnimationFrame(() => this.renderCanvas());
    }
  }

  get colCount(): number {
    return this.grid[0]?.length ?? 0;
  }

  private renderCanvas(): void {
    if (this.mode !== 'heatmap' || !this.canvasRef?.nativeElement) return;

    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (!this.grid || this.grid.length === 0) return;

    // Get canvas size from parent
    const rect = canvas.parentElement?.getBoundingClientRect();
    if (!rect) return;
    
    canvas.width = rect.width;
    canvas.height = rect.height;

    const imgData = ctx.createImageData(canvas.width, canvas.height);
    const data = imgData.data;

    const rows = this.grid.length;
    const cols = this.grid[0].length;

    // Bilinear interpolation
    for (let py = 0; py < canvas.height; py++) {
      for (let px = 0; px < canvas.width; px++) {
        const gx = (px / canvas.width) * (cols - 1);
        const gy = (py / canvas.height) * (rows - 1);

        const x0 = Math.floor(gx);
        const x1 = Math.min(x0 + 1, cols - 1);
        const y0 = Math.floor(gy);
        const y1 = Math.min(y0 + 1, rows - 1);

        const fx = gx - x0;
        const fy = gy - y0;

        const v00 = this.grid[y0]?.[x0] ?? 0;
        const v10 = this.grid[y0]?.[x1] ?? 0;
        const v01 = this.grid[y1]?.[x0] ?? 0;
        const v11 = this.grid[y1]?.[x1] ?? 0;

        const vx0 = v00 * (1 - fx) + v10 * fx;
        const vx1 = v01 * (1 - fx) + v11 * fx;
        const value = vx0 * (1 - fy) + vx1 * fy;

        const color = this.valueToRgba(value);
        const idx = (py * canvas.width + px) * 4;
        data[idx] = color.r;
        data[idx + 1] = color.g;
        data[idx + 2] = color.b;
        data[idx + 3] = color.a;
      }
    }

    ctx.putImageData(imgData, 0, 0);
  }

  private valueToRgba(value: number): { r: number; g: number; b: number; a: number } {
    if (value <= 0.05) {
      return { r: 10, g: 16, b: 48, a: 255 };
    }

    const t = Math.min(1, value / this.max);
    const hue = (1 - t) * 240;
    const saturation = 95;
    const lightness = 50;

    return this.hslToRgba(hue, saturation, lightness, 255);
  }

  private hslToRgba(h: number, s: number, l: number, a: number): { r: number; g: number; b: number; a: number } {
    s /= 100;
    l /= 100;

    const c = (1 - Math.abs(2 * l - 1)) * s;
    const x = c * (1 - Math.abs((h / 60) % 2 - 1));
    const m = l - c / 2;

    let r = 0, g = 0, b = 0;

    if (h >= 0 && h < 60) {
      r = c; g = x; b = 0;
    } else if (h >= 60 && h < 120) {
      r = x; g = c; b = 0;
    } else if (h >= 120 && h < 180) {
      r = 0; g = c; b = x;
    } else if (h >= 180 && h < 240) {
      r = 0; g = x; b = c;
    } else if (h >= 240 && h < 300) {
      r = x; g = 0; b = c;
    } else if (h >= 300 && h < 360) {
      r = c; g = 0; b = x;
    }

    return {
      r: Math.round((r + m) * 255),
      g: Math.round((g + m) * 255),
      b: Math.round((b + m) * 255),
      a,
    };
  }

  colorFor(value: number): string {
    if (value <= 0.05) return 'transparent';
    const t = Math.min(1, value / this.max);
    const hue = (1 - t) * 240;
    return `hsl(${hue}, 95%, 50%)`;
  }

  cellBg(value: number): string {
    if (value <= 0.05) return '#FFFFFF';
    const t = Math.min(1, value / this.max);
    const hue = (1 - t) * 240;
    const lightness = 85 - t * 35;
    return `hsl(${hue}, 90%, ${lightness}%)`;
  }

  cellBorder(value: number): string {
    if (value <= 0.05) return '#E8E8E8';
    const t = Math.min(1, value / this.max);
    const hue = (1 - t) * 240;
    return `hsl(${hue}, 60%, 55%)`;
  }
}
