import { Component, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { PressureHeatmapComponent, HeatmapMode } from '../../shared/pressure-heatmap/pressure-heatmap';
import { VideoTileComponent } from '../../shared/video-tile/video-tile';
import { Gait, Side, Measurement, MeasurementSegmentResult } from '../../models/measurement.model';
import { fullName } from '../../models/customer.model';
import { formatDuration } from '../../utils/time-format';
import { AddHorse } from '../horses/add-horse/add-horse';
import { Horse as HorseModel } from '../../models/horse.model';
import { BackendApiService } from '../../services/backend-api.service';
import { printMeasurementReport } from '../../services/measurement-report';
import { HeatmapDialogComponent } from '../../shared/heatmap-dialog/heatmap-dialog';

type TabKey = 'profil' | 'messungen' | 'notizen';
type PressureStatistic = 'average' | 'maximum' | 'pro';

@Component({
  selector: 'app-measurements-page',
  standalone: true,
  imports: [PageHeaderComponent, PressureHeatmapComponent, VideoTileComponent, AddHorse, HeatmapDialogComponent],
  templateUrl: './measurements.page.html',
  styleUrl: './measurements.page.scss',
  host: { '(document:keydown.escape)': 'closeExpandedHeatmap()' },
})
export class MeasurementsPage implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly measurementsLoading = signal(false);
  readonly measurementsError = signal('');

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
  readonly tab = signal<TabKey>('profil');
  readonly selectedId = signal<string | null>(null);
  readonly showHorseForm = signal(false);

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

  constructor() {
    const measurementId = this.route.snapshot.queryParamMap.get('measurementId');
    if (measurementId) {
      this.tab.set('messungen');
      this.selectedId.set(measurementId);
    }

    // Pferde aus dem Backend haben numerische IDs, ihre Messungen kommen ebenfalls aus dem Backend
    effect(onCleanup => {
      const horseId = this.params().get('horseId') ?? '';
      this.measurementsError.set('');
      if (!isBackendId(horseId)) {
        this.measurementsLoading.set(false);
        return;
      }
      this.measurementsLoading.set(true);
      const subscription = this.api.getMeasurementsOfHorse(horseId).subscribe({
        next: measurements => {
          this.data.setMeasurementsOf(horseId, measurements);
          this.measurementsLoading.set(false);
        },
        error: error => {
          this.measurementsError.set(
            error instanceof Error ? error.message : 'Messungen konnten nicht geladen werden.',
          );
          this.measurementsLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

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
      // Durchschnitt und Maximum über alle Aufnahmen, sonst über die Teilmessungen
      const grids = segments.flatMap(segment => segment.frames ?? [segment.pressureGrid]);
      return this.pressureStatistic() === 'maximum' ? maximumGrid(grids) : averageGrids(grids);
    }

    if (measurement.detail.segments?.length) return [];
    const gait = this.activeGait();
    const matchesLegacyGait = !gait || measurement.detail.gaits.includes(gait);
    return matchesLegacyGait && !this.activeSide() ? measurement.detail.pressureGrid : [];
  });
  readonly pressureGrid = computed(() => {
    const grid = this.filteredGrid();
    if (this.pressureStatistic() !== 'pro' || !this.activeGait() || !this.activeSide()) return grid;
    // mit aufgenommenen Frames die echte Messung abspielen, sonst eine Bewegung andeuten
    return frameAt(this.matchingSegments(), this.phase()) ?? animatePressureGrid(grid, this.phase());
  });

  readonly statisticMaximum = computed(() => Math.max(0, ...this.filteredGrid().flat()));
  readonly statisticAverage = computed(() => gridMean(this.filteredGrid()));
  readonly heatmapMode = signal<HeatmapMode>('heatmap');

  ngOnDestroy(): void {
    window.clearInterval(this.animationTimer);
    window.clearTimeout(this.shareFeedbackTimer);
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

  editHorse(): void {
    if (this.customer()?.isMe) this.showHorseForm.set(true);
  }

  saveHorse(horse: HorseModel): void {
    if (!this.customer()?.isMe) return;
    this.data.updateHorse(horse);
    this.showHorseForm.set(false);
  }

  deleteHorse(): void {
    const horse = this.horse();
    if (!horse || !this.customer()?.isMe) return;
    if (!window.confirm(`Möchtest du ${horse.name} und alle zugehörigen Messungen und Sättel wirklich löschen?`)) return;
    this.data.removeHorse(horse.id);
    void this.router.navigate(['/my-horses']);
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

  deleteMeasurement(id: string, event: Event): void {
    event.stopPropagation();
    const measurement = this.data.getMeasurement(id);
    if (!measurement) return;
    if (!window.confirm(`Möchtest du die Messung vom ${measurement.date} wirklich löschen?`)) return;
    if (!isBackendId(id)) {
      this.removeMeasurementLocally(id);
      return;
    }
    this.api.deleteMeasurementGroup(id).subscribe({
      next: () => this.removeMeasurementLocally(id),
      error: error => this.measurementsError.set(
        error instanceof Error ? error.message : 'Die Messung konnte nicht gelöscht werden.',
      ),
    });
  }

  private removeMeasurementLocally(id: string): void {
    this.data.removeMeasurement(id);
    if (this.selectedId() === id) {
      const remaining = this.measurements();
      this.selectedId.set(remaining[0]?.id ?? null);
    }
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

  // ─── Großansicht, Bericht, Teilen, Bearbeiten ──────────────────────────────

  readonly heatmapExpanded = signal(false);
  readonly shareFeedback = signal('');
  private shareFeedbackTimer?: number;

  /** Beschreibt die aktuelle Ansicht, z.B. "Trab · Links · Maximum". */
  readonly viewLabel = computed(() => [
    this.activeGait() || 'Alle Gangarten',
    this.activeSide() || 'Alle Hände',
    { average: 'Durchschnitt', maximum: 'Maximum', pro: 'Profi' }[this.pressureStatistic()],
  ].join(' · '));

  expandHeatmap(): void {
    this.heatmapExpanded.set(true);
  }

  closeExpandedHeatmap(): void {
    this.heatmapExpanded.set(false);
  }

  /** Öffnet den Druckdialog mit einem Bericht der Messung, dort kann er als PDF gespeichert werden. */
  downloadReport(measurement: Measurement): void {
    printMeasurementReport({
      measurement,
      horseName: this.horse()?.name ?? '',
      customerName: this.customerName(),
      grid: this.filteredGrid(),
      viewLabel: this.viewLabel(),
      maxNcm2: this.statisticMaximum(),
      meanNcm2: this.statisticAverage(),
    });
  }

  /** Teilt einen Link auf die Messung, ohne Web Share API wird der Link kopiert. */
  async shareMeasurement(measurement: Measurement): Promise<void> {
    const url = new URL(window.location.href);
    url.searchParams.set('measurementId', measurement.id);
    const title = `Satteldruckmessung ${this.horse()?.name ?? ''} vom ${measurement.date}`.trim();

    if (navigator.share) {
      try {
        await navigator.share({ title, text: title, url: url.toString() });
        return;
      } catch (error) {
        // abgebrochen: nichts tun, sonst auf Kopieren ausweichen
        if (error instanceof DOMException && error.name === 'AbortError') return;
      }
    }

    try {
      await navigator.clipboard.writeText(url.toString());
      this.showShareFeedback('Link zur Messung wurde kopiert.');
    } catch {
      this.showShareFeedback('Der Link konnte nicht kopiert werden.');
    }
  }

  private showShareFeedback(message: string): void {
    this.shareFeedback.set(message);
    window.clearTimeout(this.shareFeedbackTimer);
    this.shareFeedbackTimer = window.setTimeout(() => this.shareFeedback.set(''), 3000);
  }

  /** Bearbeiten heißt Notizen bearbeiten: wechselt in den Notizen-Tab und setzt den Cursor ins Feld. */
  editMeasurement(measurement: Measurement): void {
    this.setTab('notizen');
    window.setTimeout(() => {
      const field = document.getElementById(`notes-${measurement.id}`);
      if (field instanceof HTMLTextAreaElement) {
        field.scrollIntoView({ behavior: 'smooth', block: 'center' });
        field.focus();
      }
    });
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

  startMeasurement() {
    const horse = this.horse();
    if (!horse) return;
    void this.router.navigate(['/new-measurement'], {
      queryParams: { ownerId: horse.ownerId, horseId: horse.id },
    });
  }

  readonly formatDuration = formatDuration;
}

function isBackendId(id: string): boolean {
  return /^[1-9]\d*$/.test(id);
}

/** Pressure grid at [phase] seconds: the segments play one after another, frames are interpolated. */
function frameAt(segments: MeasurementSegmentResult[], phase: number): number[][] | null {
  let remaining = phase;
  for (const [index, segment] of segments.entries()) {
    const frames = segment.frames;
    if (!frames?.length) return null;
    const isLast = index === segments.length - 1;
    if (remaining > segment.durationSeconds && !isLast) {
      remaining -= segment.durationSeconds;
      continue;
    }
    if (frames.length === 1 || segment.durationSeconds <= 0) return frames[0];
    const position = Math.min(1, remaining / segment.durationSeconds) * (frames.length - 1);
    const lower = Math.floor(position);
    const upper = Math.min(lower + 1, frames.length - 1);
    const t = position - lower;
    return frames[lower].map((row, y) => row.map((value, x) => value * (1 - t) + frames[upper][y][x] * t));
  }
  return null;
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
