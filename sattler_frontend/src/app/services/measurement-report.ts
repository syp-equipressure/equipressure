import { Measurement, MeasurementSegmentResult } from '../models/measurement.model';
import { heatmapToDataUrl } from '../shared/pressure-heatmap/pressure-heatmap';
import { formatDuration } from '../utils/time-format';

export interface MeasurementReport {
  measurement: Measurement;
  horseName: string;
  customerName: string;
  /** The pressure grid currently shown (respects the selected gait, hand and statistic). */
  grid: number[][];
  /** Describes the current view, e.g. "Trab · Links · Maximum". */
  viewLabel: string;
  maxNcm2: number;
  meanNcm2: number;
}

/**
 * Opens the print dialog with a report of the measurement. In the dialog the report can be saved as PDF,
 * the document title is used as file name. An invisible iframe is used, so no popup blocker interferes.
 */
export function printMeasurementReport(report: MeasurementReport): void {
  const iframe = document.createElement('iframe');
  iframe.setAttribute('aria-hidden', 'true');
  iframe.style.position = 'fixed';
  iframe.style.width = '0';
  iframe.style.height = '0';
  iframe.style.border = '0';
  document.body.appendChild(iframe);

  const doc = iframe.contentDocument;
  const win = iframe.contentWindow;
  if (!doc || !win) {
    iframe.remove();
    return;
  }

  doc.open();
  doc.write(buildReportHtml(report));
  doc.close();

  // the heatmap is a data URL, wait until it is decoded before printing
  const image = doc.querySelector('img');
  const print = () => {
    win.focus();
    win.print();
    // remove the iframe after the dialog was closed
    setTimeout(() => iframe.remove(), 1000);
  };
  if (image && !image.complete) image.addEventListener('load', print, { once: true });
  else print();
}

function buildReportHtml(report: MeasurementReport): string {
  const { measurement: m } = report;
  const segments = m.detail.segments ?? [];
  const scaleMax = Math.max(report.maxNcm2, 1);
  const heatmap = report.grid.length ? heatmapToDataUrl(report.grid, scaleMax, 480) : '';
  const title = `Satteldruckmessung_${report.horseName}_${m.date}`.replace(/\s+/g, '_');

  const rows = segments.map(segment => {
    const values = (segment.frames ?? [segment.pressureGrid]).flat(2);
    const max = values.length ? Math.max(...values) : 0;
    const mean = values.length ? values.reduce((total, value) => total + value, 0) / values.length : 0;
    return `<tr>
        <td>${escapeHtml(segment.gait)}</td>
        <td>${escapeHtml(segment.side)}</td>
        <td>${formatDuration(segment.durationSeconds)} min</td>
        <td>${formatNumber(mean)}</td>
        <td>${formatNumber(max)}</td>
        <td>${segmentFrameCount(segment)}</td>
      </tr>`;
  }).join('');

  return `<!doctype html>
<html lang="de">
<head>
<meta charset="utf-8">
<title>${escapeHtml(title)}</title>
<style>
  @page { size: A4; margin: 16mm; }
  * { box-sizing: border-box; }
  body { font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; color: #1f2430; margin: 0; font-size: 12px; }
  header { border-bottom: 2px solid #6b4c9a; padding-bottom: 10px; margin-bottom: 16px; }
  h1 { margin: 0 0 4px; font-size: 22px; color: #6b4c9a; }
  h2 { font-size: 14px; margin: 20px 0 8px; color: #6b4c9a; }
  .subtitle { color: #5b6270; font-size: 13px; }
  .facts { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px 16px; }
  .fact span { display: block; font-size: 10px; letter-spacing: .06em; text-transform: uppercase; color: #8993a0; }
  .fact strong { font-size: 13px; }
  .heatmap { display: flex; gap: 16px; align-items: stretch; }
  .heatmap img { width: 300px; height: 300px; border-radius: 8px; border: 1px solid #d8dbe3; }
  .scale { display: flex; flex-direction: column; justify-content: space-between; font-size: 11px; color: #5b6270; }
  .scale-bar { width: 14px; flex: 1; border-radius: 4px; margin: 4px 0;
    background: linear-gradient(to bottom, hsl(0,95%,50%), hsl(60,95%,50%), hsl(120,95%,50%), hsl(180,95%,50%), hsl(240,95%,50%)); }
  .scale-wrap { display: flex; gap: 6px; }
  .view { font-size: 11px; color: #5b6270; margin-top: 6px; }
  table { width: 100%; border-collapse: collapse; }
  th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid #e6e8ee; }
  th { font-size: 10px; letter-spacing: .06em; text-transform: uppercase; color: #8993a0; }
  .notes { white-space: pre-wrap; background: #f6f4fa; border-radius: 8px; padding: 10px 12px; }
  footer { margin-top: 24px; font-size: 10px; color: #8993a0; }
</style>
</head>
<body>
  <header>
    <h1>Satteldruckmessung</h1>
    <div class="subtitle">${escapeHtml(report.horseName)} · Messung vom ${escapeHtml(m.date)}${m.deviceName ? ` · ${escapeHtml(m.deviceName)}` : ''}</div>
  </header>

  <section class="facts">
    ${fact('Kunde', report.customerName || '–')}
    ${fact('Reiter:in', `${m.detail.riderName} · ${formatNumber(m.detail.riderHeightM)} m · ${m.detail.riderWeightKg} kg`)}
    ${fact('Sattel', m.detail.saddleName || '–')}
    ${fact('Dauer', m.detail.durationFull)}
    ${fact('Gangarten', m.detail.gaits.join(', ') || '–')}
    ${fact('Symmetrie', `${m.symmetryPct} %`)}
  </section>

  <h2>Druckverteilung</h2>
  ${heatmap ? `<div class="heatmap">
    <div>
      <img src="${heatmap}" alt="Heatmap der Druckverteilung">
      <div class="view">Ansicht: ${escapeHtml(report.viewLabel)}</div>
    </div>
    <div class="scale-wrap">
      <div class="scale-bar"></div>
      <div class="scale"><span>${formatNumber(scaleMax)} N/cm²</span><span>0 N/cm²</span></div>
    </div>
    <section class="facts" style="grid-template-columns: 1fr; align-content: start;">
      ${fact('Maximaler Druck', `${formatNumber(report.maxNcm2)} N/cm²`)}
      ${fact('Mittlerer Druck', `${formatNumber(report.meanNcm2)} N/cm²`)}
    </section>
  </div>` : '<p>Für diese Messung sind keine Druckdaten vorhanden.</p>'}

  ${segments.length ? `<h2>Teilmessungen</h2>
  <table>
    <thead><tr><th>Gangart</th><th>Hand</th><th>Dauer</th><th>Ø Druck (N/cm²)</th><th>Max (N/cm²)</th><th>Aufnahmen</th></tr></thead>
    <tbody>${rows}</tbody>
  </table>` : ''}

  <h2>Notizen</h2>
  <div class="notes">${escapeHtml(m.notes || 'Keine Notizen vorhanden.')}</div>

  <footer>Erstellt am ${new Date().toLocaleString('de-AT')} mit EquiPressure</footer>
</body>
</html>`;
}

function fact(label: string, value: string): string {
  return `<div class="fact"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`;
}

function segmentFrameCount(segment: MeasurementSegmentResult): number {
  return segment.frames?.length ?? (segment.pressureGrid.length ? 1 : 0);
}

function formatNumber(value: number): string {
  return value.toFixed(2).replace('.', ',');
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
