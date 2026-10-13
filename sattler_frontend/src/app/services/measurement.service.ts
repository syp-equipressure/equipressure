import { Injectable } from '@angular/core';
import { Measurement } from '../models/measurement.model';

@Injectable({ providedIn: 'root' })
export class MeasurementService {
  getRecentMeasurements(): Measurement[] {
    // TODO: Das Backend benötigt einen Endpunkt für Messungen.
    return [];
  }

  getMeasurementsForHorse(_horseId: string): Measurement[] {
    // TODO: Das Backend benötigt einen Endpunkt für Messungen eines Pferdes.
    return [];
  }
}
