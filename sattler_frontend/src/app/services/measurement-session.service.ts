import { Injectable, inject, signal } from '@angular/core';
import { MeasurementSession, MeasurementSegment } from '../models/measurement-session.model';
import { Gait, Side } from '../models/measurement.model';
import { DataService } from './data.service';

@Injectable({ providedIn: 'root' })
export class MeasurementSessionService {
  private readonly data = inject(DataService);
  private readonly _session = signal<MeasurementSession | null>(null);

  readonly session = this._session.asReadonly();

  clear(): void {
    this._session.set(null);
  }

  start(ownerId: string, horseId: string, saddleId: string): boolean {
    const horse = this.data.getHorse(horseId);
    const owner = this.data.getCustomer(ownerId);
    const saddle = this.data.getSaddlesOfHorse(horseId).find(item => item.id === saddleId);
    if (!horse || horse.ownerId !== ownerId || !owner || !saddle) {
      this._session.set(null);
      return false;
    }

    this._session.set({
      ownerId,
      horseId,
      saddleId,
      notes: '',
      segments: [createSegment(1)],
    });
    return true;
  }

  setNotes(notes: string): void {
    this._session.update(session => session ? { ...session, notes } : null);
  }

  addSegment(): void {
    this._session.update(session =>
      session
        ? { ...session, segments: [...session.segments, createSegment(session.segments.length + 1)] }
        : null,
    );
  }

  updateSegment(segmentId: string, gait: Gait, side: Side): void {
    this._session.update(session => session ? {
      ...session,
      segments: session.segments.map(segment =>
        segment.id === segmentId && !segment.completed ? { ...segment, gait, side } : segment,
      ),
    } : null);
  }

  completeSegment(segmentId: string, gait: Gait, side: Side, durationSeconds: number): void {
    if (durationSeconds <= 0) return;
    this._session.update(session => session ? {
      ...session,
      segments: session.segments.map(segment =>
        segment.id === segmentId
          ? { ...segment, gait, side, durationSeconds, completed: true }
          : segment,
      ),
    } : null);
  }

  removeSegment(segmentId: string): void {
    this._session.update(session =>
      session
        ? { ...session, segments: session.segments.filter(segment => segment.id !== segmentId) }
        : null,
    );
  }

  complete(): string | null {
    const session = this._session();
    if (!session || !session.segments.some(segment => segment.completed)) return null;

    const measurement = this.data.addSessionMeasurement(session);
    this._session.set(null);
    return measurement.id;
  }
}

function createSegment(index: number): MeasurementSegment {
  return {
    id: `segment-${Date.now()}-${index}`,
    gait: 'Schritt',
    side: 'Links',
    durationSeconds: 0,
    completed: false,
  };
}
