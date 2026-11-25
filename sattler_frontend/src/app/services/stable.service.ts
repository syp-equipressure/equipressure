import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { Customer } from '../models/customer.model';
import { Horse } from '../models/horse.model';
import { BackendApiService } from './backend-api.service';
import { BackendPersonContextService } from './backend-person-context.service';
import { DataService } from './data.service';

/** A customer together with the horses he or she has (owner or rider). */
export interface CustomerHorses {
  customer: Customer;
  horses: Horse[];
}

/** The horses of one customer that are kept in a stable. */
export interface StableCustomer {
  customer: Customer;
  horses: Horse[];
}

/**
 * A stable = all horses of the customers with the same address. The backend has no own entity for stables,
 * the address of a horse is where it is kept.
 */
export interface Stable {
  /** Normalized address, the same for all horses of the stable */
  key: string;
  /** e.g. "Reiterhof Sonnenhügel 5", or "4020 Linz" if there is no street */
  name: string;
  /** e.g. "4050 Traun", empty if there is no street */
  place: string;
  address: string;
  /** Only the PLZ is known, so the position on the map is just the middle of the area */
  onlyPostalCode: boolean;
  customers: StableCustomer[];
  horseCount: number;
}

export interface LatLng {
  lat: number;
  lng: number;
}

const GEOCODE_CACHE_KEY = 'equipressure-sattler-geocode-v1';
/** Nominatim allows at most one request per second */
const GEOCODE_DELAY_MS = 1100;

@Injectable({ providedIn: 'root' })
export class StableService {
  private readonly document = inject(DOCUMENT);
  private readonly api = inject(BackendApiService);
  private readonly data = inject(DataService);
  private readonly personContext = inject(BackendPersonContextService);

  private readonly geocodeCache = new Map<string, LatLng | null>(this.readGeocodeCache());
  private geocodeQueue: Promise<unknown> = Promise.resolve();

  /** Loads the customers of the saddler with their horses (backend or local demo data). */
  loadCustomerHorses(): Observable<CustomerHorses[]> {
    const personId = this.personContext.personId();
    if (!personId) {
      return of(
        this.data.getCustomers()
          .filter(customer => !customer.isMe)
          .map(customer => ({ customer, horses: this.data.getHorsesOf(customer.id) })),
      );
    }
    return this.api.getContacts(personId).pipe(
      switchMap(customers => customers.length
        ? forkJoin(customers.map(customer => this.api.getHorses(customer.id).pipe(
            catchError(() => of([] as Horse[])),
            map(horses => ({ customer, horses })),
          )))
        : of([] as CustomerHorses[])),
    );
  }

  loadStables(): Observable<Stable[]> {
    return this.loadCustomerHorses().pipe(map(groupStables));
  }

  /** Address of the logged-in saddler (the workshop), shown with a house on the map. */
  loadOwnAddress(): Observable<string | null> {
    const personId = this.personContext.personId();
    if (!personId) return of(this.data.getSattler().address ?? null);
    return this.api.getPerson(personId).pipe(
      map(person => person.address ?? null),
      catchError(() => of(null)),
    );
  }

  /**
   * Position of an address on the map. Uses OpenStreetMap (Nominatim) and keeps the result in the local storage,
   * so every address is only looked up once. If the street is unknown to OpenStreetMap the middle of the PLZ is used.
   */
  locate(address: string): Promise<LatLng | null> {
    const key = stableKey(address);
    if (this.geocodeCache.has(key)) {
      return Promise.resolve(this.geocodeCache.get(key) ?? null);
    }
    const result = this.geocodeQueue.then(async () => {
      if (this.geocodeCache.has(key)) return this.geocodeCache.get(key) ?? null;
      const parts = splitStableAddress(address);
      let position: LatLng | null = null;
      try {
        if (parts.street) {
          position = await this.searchNominatim({ street: parts.street, postalcode: parts.postalCode, city: parts.city });
          if (!position) await delay(GEOCODE_DELAY_MS);
        }
        if (!position && (parts.postalCode || parts.city)) {
          position = await this.searchNominatim({ postalcode: parts.postalCode, city: parts.city });
        }
      } catch {
        // offline or blocked: try again next time, do not cache
        await delay(GEOCODE_DELAY_MS);
        return null;
      }
      this.geocodeCache.set(key, position);
      this.writeGeocodeCache();
      await delay(GEOCODE_DELAY_MS);
      return position;
    });
    this.geocodeQueue = result.catch(() => null);
    return result;
  }

  private async searchNominatim(query: Record<string, string>): Promise<LatLng | null> {
    const params = new URLSearchParams({ format: 'jsonv2', limit: '1', countrycodes: 'at,de', ...query });
    for (const [key, value] of [...params.entries()]) if (!value) params.delete(key);
    const response = await fetch(`https://nominatim.openstreetmap.org/search?${params}`, {
      headers: { 'Accept-Language': 'de' },
    });
    if (!response.ok) throw new Error(`Nominatim ${response.status}`);
    const results = (await response.json()) as Array<{ lat: string; lon: string }>;
    const first = results[0];
    return first ? { lat: Number(first.lat), lng: Number(first.lon) } : null;
  }

  private readGeocodeCache(): Array<[string, LatLng | null]> {
    try {
      const raw = this.document.defaultView?.localStorage.getItem(GEOCODE_CACHE_KEY);
      const parsed = raw ? JSON.parse(raw) : {};
      return Object.entries(parsed) as Array<[string, LatLng | null]>;
    } catch {
      return [];
    }
  }

  private writeGeocodeCache(): void {
    this.document.defaultView?.localStorage.setItem(
      GEOCODE_CACHE_KEY,
      JSON.stringify(Object.fromEntries(this.geocodeCache)),
    );
  }
}

/** Groups the horses of all customers by their address, stables with the most customers first. */
export function groupStables(entries: readonly CustomerHorses[]): Stable[] {
  const stables = new Map<string, Stable>();
  for (const { customer, horses } of entries) {
    for (const horse of horses) {
      const key = stableKey(horse.address);
      if (!key) continue;
      let stable = stables.get(key);
      if (!stable) {
        const parts = splitStableAddress(horse.address!);
        const place = [parts.postalCode, parts.city].filter(Boolean).join(' ');
        stable = {
          key,
          name: parts.street || place,
          place: parts.street ? place : '',
          address: horse.address!.trim(),
          onlyPostalCode: !parts.street,
          customers: [],
          horseCount: 0,
        };
        stables.set(key, stable);
      }
      let stableCustomer = stable.customers.find(entry => entry.customer.id === customer.id);
      if (!stableCustomer) {
        stableCustomer = { customer, horses: [] };
        stable.customers.push(stableCustomer);
      }
      if (!stableCustomer.horses.some(existing => existing.id === horse.id)) {
        stableCustomer.horses.push(horse);
      }
    }
  }
  for (const stable of stables.values()) {
    // a horse with several riders is counted only once
    stable.horseCount = new Set(stable.customers.flatMap(entry => entry.horses.map(horse => horse.id))).size;
  }
  return [...stables.values()].sort((left, right) =>
    right.customers.length - left.customers.length || left.name.localeCompare(right.name, 'de'));
}

/** Same address → same key, no matter how it is written (case, spaces). */
export function stableKey(address: string | undefined): string {
  return (address ?? '').trim().replace(/\s+/g, ' ').toLocaleLowerCase('de');
}

/** "Reiterhof Sonnenhügel 5, 4050 Traun" → street, PLZ and city ("4020 Linz" has no street). */
function splitStableAddress(address: string): { street: string; postalCode: string; city: string } {
  const value = address.trim();
  const withStreet = /^(.*?),\s*(\d{4,5})\s+(.+)$/.exec(value);
  if (withStreet) return { street: withStreet[1].trim(), postalCode: withStreet[2], city: withStreet[3].trim() };
  const withoutStreet = /^(\d{4,5})\s+(.+)$/.exec(value);
  if (withoutStreet) return { street: '', postalCode: withoutStreet[1], city: withoutStreet[2].trim() };
  return { street: value, postalCode: '', city: '' };
}

function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}
