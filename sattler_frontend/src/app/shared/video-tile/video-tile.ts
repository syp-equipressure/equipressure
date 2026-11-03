import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-video-tile',
  standalone: true,
  templateUrl: './video-tile.html',
  styleUrl: './video-tile.scss',
})
export class VideoTileComponent {
  @Input({ required: true }) src!: string;
  @Input({ required: true }) label!: string;
  @Input() showExpand = false;
}
