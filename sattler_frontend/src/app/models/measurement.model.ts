export type Gait = 'Schritt' | 'Trab' | 'Galopp';
export type Side = 'Links' | 'Rechts';

export interface Measurement {
  id: string;
  horseId: string;
  date: string; // dd.mm.yyyy
  deviceName: string; // e.g. "Prestige X-D2"
  durationLabel: string; // e.g. "01:22 min"
  symmetryPct: number; // 0-100
  // detail
  detail: MeasurementDetail;
}

export interface MeasurementDetail {
  maxNcm2: number;
  meanNcm2: number;
  riderName: string;
  riderHeightM: number;
  riderWeightKg: number;
  durationFull: string; // e.g. "09:22 min"
  saddleName: string;
  gaits: Gait[];
  /** A simple 18x18 grid of pressure values used for both views. */
  pressureGrid: number[][];
  videos: {
    main?: string;
    sideRight?: string;
    sideLeft?: string;
  };
}
