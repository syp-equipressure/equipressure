import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../../../shared/page-header/page-header';
import { DataService } from '../../../services/data.service';
import { MeasurementSessionService } from '../../../services/measurement-session.service';
import { formatDuration } from '../../../utils/time-format';
import { Gait, Side } from '../../../models/measurement.model';
import { MeasurementSegment } from '../../../models/measurement-session.model';
import { fullName } from '../../../models/customer.model';

@Component({
  selector: 'app-measurement-session-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './measurement-session.page.html',
  styleUrl: './measurement-session.page.scss',
})
export class MeasurementSessionPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly data = inject(DataService);
  private readonly sessionService = inject(MeasurementSessionService);
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  readonly session = this.sessionService.session;
  readonly owner = computed(() => {
    const session = this.session();
    return session ? this.data.getCustomer(session.ownerId) : undefined;
  });
  readonly pageTitle = computed(() => {
    const owner = this.owner();
    return owner ? `Messung für ${fullName(owner)}` : 'Neue Messung';
  });
  readonly horse = computed(() => {
    const session = this.session();
    return session ? this.data.getHorse(session.horseId) : undefined;
  });
  readonly saddle = computed(() => {
    const session = this.session();
    return session
      ? this.data.getSaddlesOfHorse(session.horseId).find(item => item.id === session.saddleId)
      : undefined;
  });
  readonly totalSeconds = computed(() =>
    this.session()?.segments.reduce(
      (total, segment) => total + (segment.completed ? segment.durationSeconds : 0),
      0,
    ) ?? 0,
  );
  readonly completedCount = computed(() =>
    this.session()?.segments.filter(segment => segment.completed).length ?? 0,
  );

  constructor() {
    const params = this.queryParams();
    const ownerId = params.get('ownerId');
    const horseId = params.get('horseId');
    const saddleId = params.get('saddleId');
    const current = this.session();
    const differentSession = current && (
      current.ownerId !== ownerId || current.horseId !== horseId || current.saddleId !== saddleId
    );

    if (ownerId && horseId && saddleId && (!current || differentSession)) {
      if (!this.sessionService.start(ownerId, horseId, saddleId)) {
        void this.router.navigate(['/new-measurement']);
      }
    } else if (!current) {
      void this.router.navigate(['/new-measurement']);
    }
  }

  updateNotes(event: Event): void {
    if (event.target instanceof HTMLTextAreaElement) {
      this.sessionService.setNotes(event.target.value);
    }
  }

  updateSegment(segment: MeasurementSegment, field: 'gait' | 'side', value: string): void {
    const gait = field === 'gait' && isGait(value) ? value : segment.gait;
    const side = field === 'side' && isSide(value) ? value : segment.side;
    this.sessionService.updateSegment(segment.id, gait, side);
  }

  startSegment(segment: MeasurementSegment): void {
    if (segment.completed) return;
    this.sessionService.updateSegment(segment.id, segment.gait, segment.side);
    void this.router.navigate(['/new-measurement/live'], {
      queryParams: { segmentId: segment.id },
    });
  }

  addSegment(): void {
    this.sessionService.addSegment();
  }

  removeSegment(segmentId: string): void {
    this.sessionService.removeSegment(segmentId);
  }

  backToSelection(): void {
    const session = this.session();
    if (!session) return;
    this.sessionService.clear();
    void this.router.navigate(['/new-measurement'], {
      queryParams: {
        ownerId: session.ownerId,
        horseId: session.horseId,
        saddleId: session.saddleId,
      },
    });
  }

  finish(): void {
    const session = this.session();
    if (!session || this.completedCount() === 0) return;
    const measurementId = this.sessionService.complete();
    if (measurementId) {
      void this.router.navigate(['/customers', session.ownerId, 'horses', session.horseId], {
        queryParams: { measurementId },
      });
    }
  }

  readonly formatDuration = formatDuration;
}

function isGait(value: string): value is Gait {
  return value === 'Schritt' || value === 'Trab' || value === 'Galopp';
}

function isSide(value: string): value is Side {
  return value === 'Links' || value === 'Rechts';
}
