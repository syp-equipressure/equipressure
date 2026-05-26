import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { PressureHeatmapComponent, HeatmapMode } from '../../shared/pressure-heatmap/pressure-heatmap';
import { VideoTileComponent } from '../../shared/video-tile/video-tile';
import { Gait, Side, Measurement } from '../../models/measurement.model';
import { fullName } from '../../models/customer.model';

type TabKey = 'messungen' | 'notizen';

@Component({
  selector: 'app-measurements-page',
  standalone: true,
  imports: [PageHeaderComponent, PressureHeatmapComponent, VideoTileComponent],
  templateUrl: './measurements.page.html',
  styleUrl: './measurements.page.scss',
})
export class MeasurementsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = inject(DataService);

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly customer = computed(() => this.data.getCustomer(this.params().get('customerId') ?? ''));
  readonly horse    = computed(() => this.data.getHorse(this.params().get('horseId') ?? ''));
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

  readonly activeGait = signal<Gait>('Trab');
  readonly activeSide = signal<Side>('Links');
  readonly heatmapMode = signal<HeatmapMode>('heatmap');

  selectMeasurement(id: string) {
    this.selectedId.set(id);
  }

  setTab(t: TabKey) {
    this.tab.set(t);
  }

  toggleGait(g: Gait) {
    this.activeGait.set(g);
  }

  toggleSide(s: Side) {
    this.activeSide.set(s);
  }

  toggleHeatmapMode() {
    this.heatmapMode.update(m => (m === 'heatmap' ? 'grid' : 'heatmap'));
  }
}
