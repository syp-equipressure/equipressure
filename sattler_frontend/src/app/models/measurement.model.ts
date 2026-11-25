/** The usual gaits, every other gait is a free text ("Sonstiges" in the apps, e.g. Sprung or Bergauf). */
export const STANDARD_GAITS = ['Schritt', 'Trab', 'Galopp'] as const;
export type StandardGait = (typeof STANDARD_GAITS)[number];
/** A standard gait or the name the user typed in for "Sonstiges" */
export type Gait = StandardGait | (string & {});

export function isStandardGait(value: string): value is StandardGait {
  return (STANDARD_GAITS as readonly string[]).includes(value);
}

export type Side = 'Links' | 'Rechts';

export interface Measurement {
  id: string;
  horseId: string;
  date: string; // dd.mm.yyyy
  deviceName: string; // e.g. "Prestige X-D2"
  /** The device the measurement was taken with, a saddler may delete their own measurements */
  deviceId?: string;
  durationLabel: string; // e.g. "01:22 min"
  symmetryPct: number; // 0-100
  notes: string;
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
  segments?: MeasurementSegmentResult[];
  videos: {
    main?: string;
    sideRight?: string;
    sideLeft?: string;
  };
}

export interface MeasurementSegmentResult {
  gait: Gait;
  side: Side;
  durationSeconds: number;
  /** Average of all frames of the segment. */
  pressureGrid: number[][];
  /** The recorded frames of the sensor mat in chronological order (from the backend). */
  frames?: number[][][];
}
