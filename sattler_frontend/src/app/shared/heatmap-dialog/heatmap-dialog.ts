import { Component, EventEmitter, Input, Output } from '@angular/core';
import { HeatmapMode, PressureHeatmapComponent } from '../pressure-heatmap/pressure-heatmap';

/** Shows a pressure grid in a large view above the page. Extra controls can be projected below the heatmap. */
@Component({
  selector: 'app-heatmap-dialog',
  standalone: true,
  imports: [PressureHeatmapComponent],
  templateUrl: './heatmap-dialog.html',
  styleUrl: './heatmap-dialog.scss',
})
export class HeatmapDialogComponent {
  @Input({ required: true }) grid: number[][] = [];
  @Input() mode: HeatmapMode = 'heatmap';
  @Input() max = 10;
  @Input() title = 'Druckverteilung';
  @Input() subtitle = '';
  @Output() closed = new EventEmitter<void>();
}
