import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewEncapsulation,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';
import { LatLng, Stable, StableService } from '../../services/stable.service';
import { stableIcon } from './stable-pin';

const MINI_MAP_ZOOM = 14;

/** Small, fixed map section that only shows where the given stables are (e.g. in the customer profile). */
@Component({
  selector: 'app-stable-mini-map',
  standalone: true,
  template: `
    <div class="stable-mini-map" #mapElement aria-hidden="true"></div>
    @if (state() === 'loading') {
      <span class="stable-mini-map-note">Karte wird geladen …</span>
    } @else if (state() === 'missing') {
      <span class="stable-mini-map-note">
        <span class="material-icons" aria-hidden="true">location_off</span>
        Adresse nicht auf der Karte gefunden
      </span>
    }
  `,
  styleUrl: './stable-mini-map.scss',
  encapsulation: ViewEncapsulation.None,
})
export class StableMiniMapComponent implements AfterViewInit, OnDestroy {
  private readonly stableService = inject(StableService);

  readonly stables = input.required<Stable[]>();
  readonly state = signal<'loading' | 'ready' | 'missing'>('loading');

  private readonly mapElement = viewChild.required<ElementRef<HTMLDivElement>>('mapElement');
  private readonly located = signal<Array<{ stable: Stable; position: LatLng }>>([]);
  private map: L.Map | null = null;
  private markers: L.Marker[] = [];
  private resizeObserver: ResizeObserver | null = null;

  constructor() {
    effect(() => {
      const stables = this.stables();
      this.state.set('loading');
      Promise.all(stables.map(async stable => ({ stable, position: await this.stableService.locate(stable.address) })))
        .then(results => {
          const found = results.filter((entry): entry is { stable: Stable; position: LatLng } => !!entry.position);
          this.located.set(found);
          this.state.set(found.length ? 'ready' : 'missing');
        });
    });

    effect(() => this.show(this.located()));
  }

  ngAfterViewInit(): void {
    // only a picture of the place: no moving, zooming or keyboard focus
    this.map = L.map(this.mapElement().nativeElement, {
      zoomControl: false,
      dragging: false,
      scrollWheelZoom: false,
      doubleClickZoom: false,
      boxZoom: false,
      touchZoom: false,
      keyboard: false,
    });
    this.map.attributionControl.setPrefix(false);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap',
    }).addTo(this.map);
    this.resizeObserver = new ResizeObserver(() => {
      this.map?.invalidateSize();
      this.show(this.located());
    });
    this.resizeObserver.observe(this.mapElement().nativeElement);
    this.show(this.located());
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.map?.remove();
    this.map = null;
  }

  private show(located: Array<{ stable: Stable; position: LatLng }>): void {
    const map = this.map;
    if (!map || !located.length) return;

    this.markers.forEach(marker => marker.remove());
    this.markers = located.map(({ stable, position }) =>
      L.marker([position.lat, position.lng], {
        icon: stableIcon(stable.customers.length, true),
        interactive: false,
        keyboard: false,
      }).addTo(map));

    if (located.length === 1) {
      const { position } = located[0];
      // the pin points down, so center the map a bit below it
      const point = map.project([position.lat, position.lng], MINI_MAP_ZOOM).subtract([0, 18]);
      map.setView(map.unproject(point, MINI_MAP_ZOOM), MINI_MAP_ZOOM, { animate: false });
    } else {
      map.fitBounds(
        L.latLngBounds(located.map(({ position }) => [position.lat, position.lng] as L.LatLngTuple)),
        { paddingTopLeft: [40, 56], paddingBottomRight: [40, 12], maxZoom: MINI_MAP_ZOOM, animate: false },
      );
    }
  }
}
