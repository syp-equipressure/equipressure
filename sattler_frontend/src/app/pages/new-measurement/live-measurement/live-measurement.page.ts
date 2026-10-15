import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../../shared/page-header/page-header';
import { MeasurementSessionService } from '../../../services/measurement-session.service';
import { Gait, Side } from '../../../models/measurement.model';
import { formatDuration } from '../../../utils/time-format';

@Component({
  selector: 'app-live-measurement-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './live-measurement.page.html',
  styleUrl: './live-measurement.page.scss',
})
export class LiveMeasurementPage implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sessionService = inject(MeasurementSessionService);
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
