import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { PressureHeatmapComponent, HeatmapMode } from '../../shared/pressure-heatmap/pressure-heatmap';
import { VideoTileComponent } from '../../shared/video-tile/video-tile';
import { Gait, Side, Measurement, MeasurementSegmentResult } from '../../models/measurement.model';
import { fullName } from '../../models/customer.model';
import { formatDuration } from '../../utils/time-format';

type TabKey = 'messungen' | 'notizen';
type PressureStatistic = 'average' | 'maximum' | 'pro';

@Component({
  selector: 'app-measurements-page',
  standalone: true,
  imports: [PageHeaderComponent, PressureHeatmapComponent, VideoTileComponent],
  templateUrl: './measurements.page.html',
  styleUrl: './measurements.page.scss',
})
export class MeasurementsPage implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly data = inject(DataService);

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly horse = computed(() => this.data.getHorse(this.params().get('horseId') ?? ''));
  readonly customer = computed(() => {
    const customerId = this.params().get('customerId');
    const horse = this.horse();
    return customerId ? this.data.getCustomer(customerId) : horse ? this.data.getCustomer(horse.ownerId) : undefined;
  });
  readonly measurements = computed(() => {
    const h = this.horse();
    return h ? this.data.getMeasurementsOf(h.id) : [];
  });

  readonly customerName = computed(() => {
    const c = this.customer();
    return c ? fullName(c) : '';
  });

  // ─── UI state ──────────────────────────────────────────────────────────────
  readonly tab = signal<TabKey>('messungen');
  readonly selectedId = signal<string | null>(null);

  readonly selected = computed<Measurement | null>(() => {
    const id = this.selectedId();
    const list = this.measurements();
    if (id) return list.find(m => m.id === id) ?? list[0] ?? null;
    return list[0] ?? null;
  });

  readonly activeGait = signal<Gait | ''>('');
  readonly activeSide = signal<Side | ''>('');
  readonly pressureStatistic = signal<PressureStatistic>('average');
  readonly phase = signal(0);
  readonly isPlaying = signal(false);
  private readonly animationTimer = window.setInterval(
    () => {
      if (this.pressureStatistic() !== 'pro' || !this.isPlaying()) return;
      const nextPhase = Math.min(this.phase() + 0.25, this.playbackDuration());
      this.phase.set(nextPhase);
      if (nextPhase >= this.playbackDuration()) this.isPlaying.set(false);
    },
    250,
  );

  readonly gaitOptions = computed<Gait[]>(() => {
    const measurement = this.selected();
    if (!measurement) return [];
    const gaits = measurement.detail.segments?.map(segment => segment.gait) ?? measurement.detail.gaits;
    return unique(gaits);
  });
  readonly sideOptions = computed<Side[]>(() => {
    const segments = this.selected()?.detail.segments ?? [];
    return unique(
      segments
        .filter(segment => !this.activeGait() || segment.gait === this.activeGait())
        .map(segment => segment.side),
    );
  });
  readonly matchingSegments = computed<MeasurementSegmentResult[]>(() => {
    const segments = this.selected()?.detail.segments ?? [];
    return segments.filter(segment =>
      (!this.activeGait() || segment.gait === this.activeGait()) &&
      (!this.activeSide() || segment.side === this.activeSide()),
    );
  });
  readonly playbackDuration = computed(() =>
    this.matchingSegments().reduce((total, segment) => total + segment.durationSeconds, 0),
  );
  readonly playbackTime = computed(() => formatDuration(Math.floor(this.phase())));
  readonly isProAvailable = computed(() =>
    !!this.activeGait() && !!this.activeSide() && this.matchingSegments().length > 0,
  );
  readonly filteredGrid = computed(() => {
    const measurement = this.selected();
    if (!measurement) return [];

    const segments = this.matchingSegments();
    if (segments.length) {
      const grids = segments.map(segment => segment.pressureGrid);
      return this.pressureStatistic() === 'maximum' ? maximumGrid(grids) : averageGrids(grids);
    }

    if (measurement.detail.segments?.length) return [];
    const gait = this.activeGait();
    const matchesLegacyGait = !gait || measurement.detail.gaits.includes(gait);
    return matchesLegacyGait && !this.activeSide() ? measurement.detail.pressureGrid : [];
  });
  readonly pressureGrid = computed(() => {
    const grid = this.filteredGrid();
    return this.pressureStatistic() === 'pro' &&
      this.activeGait() &&
      this.activeSide()
      ? animatePressureGrid(grid, this.phase())
      : grid;
  });

  readonly statisticMaximum = computed(() => Math.max(0, ...this.filteredGrid().flat()));
  readonly statisticAverage = computed(() => gridMean(this.filteredGrid()));
  readonly heatmapMode = signal<HeatmapMode>('heatmap');

  ngOnDestroy(): void {
    window.clearInterval(this.animationTimer);
  }

  selectMeasurement(id: string) {
    this.selectedId.set(id);
    this.activeGait.set('');
    this.activeSide.set('');
    this.resetPlayback();
  }

  updateNotes(id: string, event: Event): void {
    if (event.target instanceof HTMLTextAreaElement) {
      this.data.updateMeasurementNotes(id, event.target.value);
    }
  }

  setTab(t: TabKey) {
    this.tab.set(t);
  }

  selectGait(value: string): void {
    const gait = isGait(value) ? value : '';
    this.activeGait.set(gait);
    const side = this.activeSide();
    if (side && !this.sideOptions().includes(side)) {
      this.activeSide.set('');
    }
    this.resetPlayback();
    this.resetProModeWithoutSelection();
  }

  selectSide(value: string): void {
    this.activeSide.set(isSide(value) ? value : '');
    this.resetPlayback();
    this.resetProModeWithoutSelection();
  }

  selectPressureStatistic(value: string): void {
    if (value === 'pro' && !this.isProAvailable()) return;
    if (value === 'average' || value === 'maximum' || value === 'pro') {
      this.pressureStatistic.set(value);
      this.resetPlayback();
      if (value === 'pro') this.isPlaying.set(true);
    }
  }

  togglePlayback(): void {
    if (!this.isProAvailable()) return;
    if (this.phase() >= this.playbackDuration()) this.phase.set(0);
    this.isPlaying.update(playing => !playing);
  }

  stopPlayback(): void {
    this.resetPlayback();
  }

  seekPlayback(event: Event): void {
    if (!(event.target instanceof HTMLInputElement)) return;
    this.phase.set(Math.min(Number(event.target.value), this.playbackDuration()));
    if (this.phase() >= this.playbackDuration()) this.isPlaying.set(false);
  }

  skipPlayback(seconds: number): void {
    const nextPhase = Math.max(0, Math.min(this.phase() + seconds, this.playbackDuration()));
    this.phase.set(nextPhase);
    this.isPlaying.set(nextPhase < this.playbackDuration());
  }

  toggleHeatmapMode() {
    this.heatmapMode.update(m => (m === 'heatmap' ? 'grid' : 'heatmap'));
  }

  resetView() {
    this.activeGait.set('');
    this.activeSide.set('');
    this.pressureStatistic.set('average');
    this.heatmapMode.set('heatmap');
    this.resetPlayback();
  }

  readonly isProSelectionMissing = computed(() =>
    this.pressureStatistic() === 'pro' && (!this.activeGait() || !this.activeSide()),
  );

  private resetProModeWithoutSelection(): void {
    if (this.pressureStatistic() === 'pro' && (!this.activeGait() || !this.activeSide())) {
      this.pressureStatistic.set('average');
    }
  }

  private resetPlayback(): void {
    this.phase.set(0);
    this.isPlaying.set(false);
  }

  newMeasurement() {
    const h = this.horse();
    if (!h) return;
    this.router.navigate(['/new-measurement'], {
      queryParams: { ownerId: h.ownerId, horseId: h.id },
    });
  }

  readonly formatDuration = formatDuration;
}

function isGait(value: string): value is Gait {
  return value === 'Schritt' || value === 'Trab' || value === 'Galopp';
}

function isSide(value: string): value is Side {
  return value === 'Links' || value === 'Rechts';
}

function unique<T>(values: T[]): T[] {
  return [...new Set(values)];
}

function averageGrids(grids: number[][][]): number[][] {
  if (!grids.length) return [];
  return grids[0].map((row, y) =>
    row.map((_, x) => grids.reduce((total, grid) => total + (grid[y]?.[x] ?? 0), 0) / grids.length),
  );
}

function maximumGrid(grids: number[][][]): number[][] {
  if (!grids.length) return [];
  return grids[0].map((row, y) =>
    row.map((_, x) => Math.max(...grids.map(grid => grid[y]?.[x] ?? 0))),
  );
}

function gridMean(grid: number[][]): number {
  const values = grid.flat();
  return values.length ? values.reduce((total, value) => total + value, 0) / values.length : 0;
}

function animatePressureGrid(grid: number[][], phase: number): number[][] {
  const rows = grid.length;
  const cols = grid[0]?.length ?? 0;
  if (!rows || !cols) return grid;

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
  const top = grid[y0][x0] * (1 - tx) + grid[y0][x1] * tx;
  const bottom = grid[y1][x0] * (1 - tx) + grid[y1][x1] * tx;
  return top * (1 - ty) + bottom * ty;
}
