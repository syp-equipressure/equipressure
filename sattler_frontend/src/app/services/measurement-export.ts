import { Measurement } from '../models/measurement.model';

/**
 * Downloads all recorded frames of a measurement as CSV for Excel (semicolon separated, decimal comma).
 * Every row is one frame of the sensor mat: gait, hand, frame number, second within the measurement and the
 * values of all sensors in N/cm² (columns Z01S01 … Z20S20 = row 1 column 1 … row 20 column 20, row 1 is the front).
 */
export function downloadMeasurementCsv(measurement: Measurement, horseName: string): void {
  const segments = measurement.detail.segments ?? [];
  const firstGrid = segments.flatMap(segment => segment.frames ?? [segment.pressureGrid]).find(grid => grid.length);
  const rows = firstGrid?.length ?? 0;
  const cols = firstGrid?.[0]?.length ?? 0;

  const sensorColumns: string[] = [];
  for (let row = 1; row <= rows; row++) {
    for (let col = 1; col <= cols; col++) {
      sensorColumns.push(`Z${pad(row)}S${pad(col)}`);
    }
  }

  const lines = [['Gangart', 'Hand', 'Aufnahme', 'Sekunde', ...sensorColumns].join(';')];
  for (const segment of segments) {
    const frames = segment.frames ?? (segment.pressureGrid.length ? [segment.pressureGrid] : []);
    frames.forEach((frame, index) => {
      const second = frames.length > 1 ? (index * segment.durationSeconds) / (frames.length - 1) : 0;
      lines.push([
        segment.gait,
        segment.side,
        String(index + 1),
        formatNumber(second, 1),
        ...frame.flat().map(value => formatNumber(value, 2)),
      ].join(';'));
    });
  }

  // BOM, damit Excel die Datei als UTF-8 erkennt (Umlaute)
  const blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `Messdaten_${horseName}_${measurement.date}.csv`.replace(/\s+/g, '_');
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

function formatNumber(value: number, digits: number): string {
  return value.toFixed(digits).replace('.', ',');
}
