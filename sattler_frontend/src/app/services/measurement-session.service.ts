import { Injectable, inject, signal } from '@angular/core';
import { MeasurementSession, MeasurementSegment } from '../models/measurement-session.model';
import { Gait, Side } from '../models/measurement.model';
import { DataService } from './data.service';
import { BackendApiService } from './backend-api.service';
import { BackendPersonContextService } from './backend-person-context.service';
import { Observable, concat, map, of, switchMap, throwError, toArray } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MeasurementSessionService {
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);
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

  moveSegment(fromIndex: number, toIndex: number): void {
    this._session.update(session => {
      if (
        !session ||
        fromIndex < 0 ||
        toIndex < 0 ||
        fromIndex >= session.segments.length ||
        toIndex >= session.segments.length ||
        fromIndex === toIndex
      ) {
        return session;
      }

      const segments = [...session.segments];
      const [segment] = segments.splice(fromIndex, 1);
      segments.splice(toIndex, 0, segment);
      return { ...session, segments };
    });
  }

  /**
   * Saves the session and returns the id of the new measurement. Horses from the backend are saved as a
   * measurement group with one measurement per completed segment, other horses only locally.
   */
  complete(): Observable<string> {
    const session = this._session();
    if (!session || !session.segments.some(segment => segment.completed)) {
      return throwError(() => new Error('Bitte führe mindestens eine Teilmessung durch.'));
    }

    const personId = this.personContext.personId();
    const isBackendSession = !!personId &&
      [session.ownerId, session.horseId, session.saddleId].every(id => /^[1-9]\d*$/.test(id));
    if (!isBackendSession) {
      const measurement = this.data.addSessionMeasurement(session);
      this._session.set(null);
      return of(measurement.id);
    }

    // Gangart + Hand sind pro Messgruppe eindeutig, bei Wiederholungen zählt die letzte Teilmessung
    const segments = [...new Map(
      session.segments
        .filter(segment => segment.completed)
        .map(segment => [`${segment.gait}|${segment.side}`, segment] as const),
    ).values()];
    const date = new Date();

    return this.api.getDevices(personId).pipe(
      switchMap(devices => {
        const device = devices[0];
        if (!device) throw new Error('Für deine Person ist kein Messgerät hinterlegt.');
        return this.api.createMeasurementGroup({
          name: session.notes.trim() || `Messung vom ${date.toLocaleDateString('de-AT')}`,
          date,
          notes: session.notes.trim() || null,
          personId: session.ownerId,
          horseId: session.horseId,
          saddleId: session.saddleId,
          deviceId: device.id,
        });
      }),
      // nacheinander anlegen, damit die Reihenfolge der Teilmessungen erhalten bleibt
      switchMap(groupId => concat(
        ...segments.map(segment => this.api.createMeasurement(groupId, segment.gait, segment.side)),
      ).pipe(toArray(), map(() => groupId))),
      map(groupId => {
        this.data.addSessionMeasurement({ ...session, segments }, groupId);
        this._session.set(null);
        return groupId;
      }),
    );
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
