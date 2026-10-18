import { Gait, Side } from './measurement.model';

export interface MeasurementSession {
  ownerId: string;
  horseId: string;
  saddleId: string;
  notes: string;
  segments: MeasurementSegment[];
}

export interface MeasurementSegment {
  id: string;
  gait: Gait;
  side: Side;
  durationSeconds: number;
  completed: boolean;
}
