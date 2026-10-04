import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../../shared/page-header/page-header';
import { PressureHeatmapComponent } from '../../../shared/pressure-heatmap/pressure-heatmap';
import { MeasurementSessionService } from '../../../services/measurement-session.service';
import { DataService } from '../../../services/data.service';
import { Gait, Side } from '../../../models/measurement.model';
import { formatDuration } from '../../../utils/time-format';

@Component({
  selector: 'app-live-measurement-page',
  standalone: true,
  imports: [PageHeaderComponent, PressureHeatmapComponent],
  templateUrl: './live-measurement.page.html',
  styleUrl: './live-measurement.page.scss',
})
export class LiveMeasurementPage implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sessionService = inject(MeasurementSessionService);
  private readonly data = inject(DataService);
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });
  private timer?: number;
  private readonly startedAt = Date.now();

  readonly session = this.sessionService.session;
  readonly segment = computed(() => {
    const id = this.queryParams().get('segmentId');
    return this.session()?.segments.find(segment => segment.id === id) ?? null;
  });
  readonly phase = signal(0);
  readonly elapsedSeconds = computed(() => Math.floor(this.phase()));
  readonly gait = signal<Gait>('Schritt');
  readonly side = signal<Side>('Links');
  readonly pressureGrid = computed(() =>
    animatePressureGrid(this.data.getSamplePressureGrid(), this.phase()),
  );

  constructor() {
    if (!this.session() || !this.segment() || this.segment()?.completed) {
      void this.router.navigate(['/new-measurement/session']);
      return;
    }
    this.gait.set(this.segment()!.gait);
    this.side.set(this.segment()!.side);
    this.timer = window.setInterval(() => {
      this.phase.set((Date.now() - this.startedAt) / 1000);
    }, 250);
  }

  ngOnDestroy(): void {
    if (this.timer !== undefined) window.clearInterval(this.timer);
  }

  finishSegment(): void {
    const segment = this.segment();
    if (!segment || this.elapsedSeconds() <= 0) return;
    this.sessionService.completeSegment(
      segment.id,
      this.gait(),
      this.side(),
      this.elapsedSeconds(),
    );
    void this.router.navigate(['/new-measurement/session']);
  }

  readonly formatDuration = formatDuration;
}

function animatePressureGrid(grid: number[][], phase: number): number[][] {
  const rows = grid.length;
  const cols = grid[0]?.length ?? 0;
  if (rows === 0 || cols === 0) return grid;

  const shiftX = Math.sin(phase * 1.3) * 0.55;
  const shiftY = Math.cos(phase * 0.9) * 0.28;
  const pulse = 1 + Math.sin(phase * 3.2) * 0.08;

  return grid.map((row, y) =>
    row.map((_, x) => sampleGrid(grid, x + shiftX, y + shiftY, rows, cols) * pulse),
  );
}

function sampleGrid(grid: number[][], x: number, y: number, rows: number, cols: number): number {
  const boundedX = Math.max(0, Math.min(cols - 1, x));
  const boundedY = Math.max(0, Math.min(rows - 1, y));
  const x0 = Math.floor(boundedX);
  const y0 = Math.floor(boundedY);
  const x1 = Math.min(x0 + 1, cols - 1);
  const y1 = Math.min(y0 + 1, rows - 1);
  const tx = boundedX - x0;
  const ty = boundedY - y0;
  const top = (grid[y0][x0] * (1 - tx)) + (grid[y0][x1] * tx);
  const bottom = (grid[y1][x0] * (1 - tx)) + (grid[y1][x1] * tx);
  return top * (1 - ty) + bottom * ty;
}
