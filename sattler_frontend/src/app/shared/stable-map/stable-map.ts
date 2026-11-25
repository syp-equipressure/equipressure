import {
  AfterViewInit,
  Component,
  DOCUMENT,
  ElementRef,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import * as L from 'leaflet';
import { fullName } from '../../models/customer.model';
import { LatLng, Stable, StableService } from '../../services/stable.service';
import { stableIcon } from './stable-pin';

/** Austria, shown while the stables are being located */
const DEFAULT_CENTER: L.LatLngExpression = [47.7, 13.6];
const DEFAULT_ZOOM = 7;
const HOME_ZOOM = 14;

const STREET_TILES = {
  url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
  maxZoom: 19,
};
/** Same satellite images as in the Reiter app */
const SATELLITE_TILES = {
  url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
  attribution: 'Bilder &copy; Esri, Maxar, Earthstar Geographics',
  maxZoom: 19,
};

/**
 * Map of the stables like the "Sattler finden" map of the Reiter app: a pin per stable (number = customers there),
 * a card with the customers when a pin is clicked, a house for the own address, satellite view, home and overview
 * buttons and a fullscreen mode.
 *
 * Fullscreen lays [fullscreenTarget] (or only the map) over the whole window with CSS (class "is-fullscreen", Esc
 * closes it), because the Fullscreen API does not work for elements on iPhones and in embedded browsers.
 */
@Component({
  selector: 'app-stable-map',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './stable-map.html',
  styleUrl: './stable-map.scss',
  encapsulation: ViewEncapsulation.None,
})
export class StableMapComponent implements AfterViewInit, OnDestroy {
  private readonly stableService = inject(StableService);
  private readonly document = inject(DOCUMENT);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly stables = input.required<Stable[]>();
  readonly selectedKey = input<string | null>(null);
  /** Address of the saddler, shown with a house */
  readonly homeAddress = input<string | null>(null);
  /** Element that is shown in fullscreen, by default only the map */
  readonly fullscreenTarget = input<HTMLElement | null>(null);
  /** Key of the clicked stable, null when the card is closed */
  readonly selected = output<string | null>();
  /** The stables that could not be found on the map (e.g. unknown address) */
  readonly unlocated = output<string[]>();

  readonly locating = signal(false);
  readonly isFullscreen = signal(false);
  readonly isSatellite = signal(false);
  readonly homePosition = signal<LatLng | null>(null);
  readonly selectedStable = computed(
    () => this.stables().find(stable => stable.key === this.selectedKey()) ?? null,
  );
  readonly fullName = fullName;

  private readonly mapElement = viewChild.required<ElementRef<HTMLDivElement>>('mapElement');
  private readonly positions = signal<Map<string, LatLng>>(new Map());
  private map: L.Map | null = null;
  private tileLayer: L.TileLayer | null = null;
  private markers = new Map<string, L.Marker>();
  private homeMarker: L.Marker | null = null;
  private resizeObserver: ResizeObserver | null = null;
  private fittedFor = '';
  private lastSelectedKey: string | null = null;
  private destroyed = false;
  private readonly onKeydown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape') return;
    if (this.selectedKey()) this.selected.emit(null);
    else if (this.isFullscreen()) this.setFullscreen(false);
  };

  constructor() {
    // locate all stables, one after another (Nominatim allows one request per second)
    effect(() => {
      const stables = this.stables();
      const home = this.homeAddress();
      this.locateAll(stables, home);
    });

    effect(() => {
      const positions = this.positions();
      const home = this.homePosition();
      const selectedKey = this.selectedKey();
      this.renderMarkers(positions, home, selectedKey);
    });

    effect(() => {
      const satellite = this.isSatellite();
      this.setTiles(satellite);
    });
  }

  ngAfterViewInit(): void {
    this.map = L.map(this.mapElement().nativeElement, {
      center: DEFAULT_CENTER,
      zoom: DEFAULT_ZOOM,
      scrollWheelZoom: true,
      zoomControl: false,
    });
    L.control.zoom({ position: 'topleft', zoomInTitle: 'Hineinzoomen', zoomOutTitle: 'Herauszoomen' }).addTo(this.map);
    // a click on the map (not on a pin) closes the card, like in the Reiter app
    this.map.on('click', () => {
      if (this.selectedKey()) this.selected.emit(null);
    });
    this.setTiles(this.isSatellite());
    // Leaflet measures the container only once, so tell it when the layout changes its size
    this.resizeObserver = new ResizeObserver(() => {
      this.map?.invalidateSize();
      this.fittedFor = '';
      this.lastSelectedKey = null;
      this.renderMarkers(this.positions(), this.homePosition(), this.selectedKey());
    });
    this.resizeObserver.observe(this.mapElement().nativeElement);
    this.document.addEventListener('keydown', this.onKeydown);
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.document.removeEventListener('keydown', this.onKeydown);
    if (this.isFullscreen()) this.setFullscreen(false);
    this.resizeObserver?.disconnect();
    this.map?.remove();
    this.map = null;
  }

  toggleFullscreen(): void {
    this.setFullscreen(!this.isFullscreen());
  }

  toggleSatellite(): void {
    this.isSatellite.update(value => !value);
  }

  /** "Meine Sattlerei": zooms to the own address and closes the card */
  goHome(): void {
    const home = this.homePosition();
    if (!home || !this.map) return;
    this.selected.emit(null);
    this.map.setView([home.lat, home.lng], HOME_ZOOM, { animate: true });
  }

  /** "Übersicht": all stables and the own address */
  showOverview(): void {
    this.selected.emit(null);
    this.fittedFor = '';
    this.fitAll(this.positions(), this.homePosition());
  }

  closeCard(): void {
    this.selected.emit(null);
  }

  horseNames(stable: Stable, customerId: string): string {
    const entry = stable.customers.find(item => item.customer.id === customerId);
    return entry ? entry.horses.map(horse => horse.name).join(', ') : '';
  }

  plural(count: number, one: string, many: string): string {
    return `${count} ${count === 1 ? one : many}`;
  }

  private setFullscreen(on: boolean): void {
    this.isFullscreen.set(on);
    this.fullscreenElement().classList.toggle('is-fullscreen', on);
    // no scrolling of the page behind the map
    this.document.body.classList.toggle('stable-map-fullscreen-open', on);
  }

  private fullscreenElement(): HTMLElement {
    return this.fullscreenTarget() ?? this.host.nativeElement;
  }

  private setTiles(satellite: boolean): void {
    if (!this.map) return;
    const tiles = satellite ? SATELLITE_TILES : STREET_TILES;
    this.tileLayer?.remove();
    this.tileLayer = L.tileLayer(tiles.url, { maxZoom: tiles.maxZoom, attribution: tiles.attribution })
      .addTo(this.map);
  }

  private async locateAll(stables: Stable[], home: string | null): Promise<void> {
    this.locating.set(true);
    if (home) {
      const position = await this.stableService.locate(home);
      if (this.destroyed) return;
      this.homePosition.set(position);
    } else {
      this.homePosition.set(null);
    }
    const missing: string[] = [];
    for (const stable of stables) {
      const position = await this.stableService.locate(stable.address);
      if (this.destroyed) return;
      if (position) {
        this.positions.update(positions => new Map(positions).set(stable.key, position));
      } else {
        missing.push(stable.key);
      }
    }
    this.locating.set(false);
    this.unlocated.emit(missing);
  }

  private renderMarkers(positions: Map<string, LatLng>, home: LatLng | null, selectedKey: string | null): void {
    const map = this.map;
    if (!map) return;
    const stables = this.stables().filter(stable => positions.has(stable.key));

    this.renderHomeMarker(map, home);

    for (const [key, marker] of this.markers) {
      if (!stables.some(stable => stable.key === key)) {
        marker.remove();
        this.markers.delete(key);
      }
    }

    for (const stable of stables) {
      const position = positions.get(stable.key)!;
      const isSelected = stable.key === selectedKey;
      const icon = stableIcon(stable.customers.length, isSelected);
      let marker = this.markers.get(stable.key);
      if (!marker) {
        marker = L.marker([position.lat, position.lng], { icon, keyboard: true, title: stable.name })
          .on('click', () => this.selected.emit(stable.key))
          .addTo(map);
        this.markers.set(stable.key, marker);
      } else {
        marker.setIcon(icon);
      }
      marker.setZIndexOffset(isSelected ? 1000 : 0);
    }

    const selectedPosition = selectedKey ? positions.get(selectedKey) : undefined;
    if (selectedPosition) {
      if (selectedKey !== this.lastSelectedKey) {
        this.lastSelectedKey = selectedKey;
        this.focusAboveCard(map, selectedPosition);
      }
      return;
    }
    this.lastSelectedKey = null;
    this.fitAll(positions, home);
  }

  /** Centers the pin a bit above the middle, so the card at the bottom does not cover it. */
  private focusAboveCard(map: L.Map, position: LatLng): void {
    const zoom = Math.max(map.getZoom(), 12);
    const cardOffset = Math.min(map.getSize().y * 0.22, 130);
    const point = map.project([position.lat, position.lng], zoom).add([0, cardOffset]);
    map.setView(map.unproject(point, zoom), zoom, { animate: true });
  }

  /** Zooms so that all stables and the own address are visible, again whenever a new one was found. */
  private fitAll(positions: Map<string, LatLng>, home: LatLng | null): void {
    const map = this.map;
    if (!map) return;
    const points = this.stables()
      .map(stable => positions.get(stable.key))
      .filter((position): position is LatLng => position !== undefined);
    if (home) points.push(home);
    const fitKey = points.map(point => `${point.lat},${point.lng}`).join('|');
    if (!points.length || fitKey === this.fittedFor) return;
    this.fittedFor = fitKey;
    map.fitBounds(L.latLngBounds(points.map(point => [point.lat, point.lng] as L.LatLngTuple)), {
      padding: [64, 64],
      maxZoom: 13,
    });
  }

  private renderHomeMarker(map: L.Map, position: LatLng | null): void {
    if (!position) {
      this.homeMarker?.remove();
      this.homeMarker = null;
      return;
    }
    if (!this.homeMarker) {
      this.homeMarker = L.marker([position.lat, position.lng], {
        icon: L.divIcon({
          className: 'stable-pin-wrapper',
          html: '<span class="stable-pin is-home"><span class="material-icons">home</span></span>',
          iconSize: [36, 44],
          iconAnchor: [18, 44],
        }),
        keyboard: false,
        title: 'Meine Sattlerei',
        // behind the stables, so a stable next to the workshop can still be clicked
        zIndexOffset: -500,
      })
        .on('click', () => this.goHome())
        .addTo(map);
    } else {
      this.homeMarker.setLatLng([position.lat, position.lng]);
    }
  }
}
