import { Gait, Measurement, MeasurementSegmentResult, Side } from '../models/measurement.model';
import { formatDuration } from '../utils/time-format';

/** The backend stores pressure values in mbar, the app shows N/cm² (100 mbar = 1 N/cm²). */
const MBAR_PER_NCM2 = 100;
const GRID_SIZE = 20;

export interface BackendGroupDetail {
  id: number;
  date: string; // yyyy-mm-dd
  name: string;
  notes: string | null;
  horseName: string;
  riderFirstName: string;
  riderLastName: string;
  riderHeight: number;
  riderWeight: number;
  saddleName: string;
}

export interface BackendMeasurement {
  id: number;
  pace: string;
  hand: string;
  description: string | null;
}

export interface BackendMeasurementData {
  id: number;
  data: string;
  timestamp: string;
}

export interface BackendMeasurementWithData {
  measurement: BackendMeasurement;
  data: BackendMeasurementData[];
}

/** Turns a measurement group of the backend into a measurement of the app. */
export function toMeasurement(
  horseId: string,
  group: BackendGroupDetail,
  measurements: BackendMeasurementWithData[],
): Measurement {
  const segments = measurements.map(toSegment);
  const allFrames = segments.flatMap(segment => segment.frames ?? []);
  const pressureGrid = averageGrids(allFrames);
  const totalSeconds = segments.reduce((total, segment) => total + segment.durationSeconds, 0);
  const duration = `${formatDuration(totalSeconds)} min`;

  return {
    id: String(group.id),
    horseId,
    date: toGermanDate(group.date),
    deviceName: group.name,
    durationLabel: duration,
    symmetryPct: symmetry(pressureGrid),
    notes: group.notes ?? '',
    detail: {
      maxNcm2: Math.max(0, ...allFrames.flat(2)),
      meanNcm2: gridMean(pressureGrid),
      riderName: `${group.riderFirstName} ${group.riderLastName}`.trim(),
      riderHeightM: Number(group.riderHeight) / 100,
      riderWeightKg: Number(group.riderWeight),
      durationFull: duration,
      saddleName: group.saddleName,
      gaits: [...new Set(segments.map(segment => segment.gait))],
      pressureGrid,
      segments,
      videos: {},
    },
  };
}

/** A measurement without data (the backend cannot store recorded data yet) has an empty grid. */
function toSegment({ measurement, data }: BackendMeasurementWithData): MeasurementSegmentResult {
  const frames = data
    .map(entry => parseFrame(entry.data))
    .filter((frame): frame is number[][] => frame !== null);

  const timestamps = data.map(entry => new Date(entry.timestamp).getTime()).filter(Number.isFinite);
  const durationSeconds = timestamps.length >= 2
    ? Math.round((Math.max(...timestamps) - Math.min(...timestamps)) / 1000)
    : 0;

  return {
    gait: measurement.pace as Gait,
    side: measurement.hand as Side,
    durationSeconds,
    pressureGrid: averageGrids(frames),
    frames: frames.length ? frames : undefined,
  };
}

/** True if none of the measurements of a group has recorded data. */
export function hasNoRecordedData(measurement: Measurement): boolean {
  return !(measurement.detail.segments ?? []).some(segment => segment.frames?.length);
}

/** A data entry is a JSON array with 20x20 values in mbar, row by row. */
function parseFrame(data: string): number[][] | null {
  try {
    const values: unknown = JSON.parse(data);
    if (!Array.isArray(values) || values.length !== GRID_SIZE * GRID_SIZE) return null;
    const numbers = values.map(value => Number(value) / MBAR_PER_NCM2);
    return Array.from({ length: GRID_SIZE }, (_, row) =>
      numbers.slice(row * GRID_SIZE, (row + 1) * GRID_SIZE),
    );
  } catch {
    return null;
  }
}

/** How evenly the pressure is spread between the left and the right panel (100 % = equal). */
function symmetry(grid: number[][]): number {
  let left = 0;
  let right = 0;
  for (const row of grid) {
    const half = row.length / 2;
    row.forEach((value, col) => (col < half ? (left += value) : (right += value)));
  }
  const total = left + right;
  return total > 0 ? Math.round(100 - (Math.abs(left - right) / total) * 100) : 100;
}

export function averageGrids(grids: number[][][]): number[][] {
  if (!grids.length) return [];
  return grids[0].map((row, y) =>
    row.map((_, x) => grids.reduce((total, grid) => total + (grid[y]?.[x] ?? 0), 0) / grids.length),
  );
}

function gridMean(grid: number[][]): number {
  const values = grid.flat();
  return values.length ? values.reduce((total, value) => total + value, 0) / values.length : 0;
}

function toGermanDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}
