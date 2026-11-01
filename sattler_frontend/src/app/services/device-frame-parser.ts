import { MBAR_PER_NCM2 } from './measurement-mapper';

const GRID_SIZE = 20;
const CELLS = GRID_SIZE * GRID_SIZE;
const MAX_PENDING_LENGTH = 64 * 1024;

/**
 * Builds heatmap frames (20x20 values in N/cm²) from the Bluetooth data of the measurement device.
 * Like in the backend (MeasurementDataBluetoothHandler) every line is a number or a JSON array in mbar.
 * A Bluetooth message only carries a few bytes, so longer lines arrive in several parts.
 */
export class DeviceFrameParser {
  private pending = '';

  /** Takes the next part of the data and returns all frames that are complete with it. */
  add(text: string): number[][][] {
    this.pending += text;

    const lines = this.pending.split('\n');
    this.pending = lines.pop() ?? '';
    if (this.pending.length > MAX_PENDING_LENGTH) this.pending = '';

    return lines
      .map(line => parseLine(line.trim()))
      .filter((frame): frame is number[][] => frame !== null);
  }

  /** Forgets a started line, e.g. after the connection was lost. */
  reset(): void {
    this.pending = '';
  }
}

function parseLine(line: string): number[][] | null {
  if (!line) return null;

  let parsed: unknown;
  try {
    parsed = JSON.parse(line);
  } catch {
    return null;
  }

  const values = Array.isArray(parsed) ? parsed.map(Number) : [Number(parsed)];
  if (values.length === 0 || values.length > CELLS || values.some(value => !Number.isFinite(value))) return null;

  const cells = (values.length === CELLS ? values : toHeatmapCells(values)).map(value => value / MBAR_PER_NCM2);
  return Array.from({ length: GRID_SIZE }, (_, row) => cells.slice(row * GRID_SIZE, (row + 1) * GRID_SIZE));
}

/** Fewer values (e.g. a pressure curve) are stretched to one row and multiplied with themselves, like in the app. */
function toHeatmapCells(values: number[]): number[] {
  const profile = Array.from({ length: GRID_SIZE }, (_, index) => {
    if (values.length === 1) return values[0];
    const position = index * (values.length - 1) / (GRID_SIZE - 1);
    const lower = Math.floor(position);
    const upper = Math.min(lower + 1, values.length - 1);
    return values[lower] * (1 - (position - lower)) + values[upper] * (position - lower);
  });

  const peak = Math.max(...profile);
  if (peak <= 0) return new Array<number>(CELLS).fill(0);
  return Array.from({ length: CELLS }, (_, index) =>
    profile[Math.floor(index / GRID_SIZE)] * profile[index % GRID_SIZE] / peak,
  );
}
