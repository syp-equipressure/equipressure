import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import { Customer } from '../models/customer.model';
import { Horse } from '../models/horse.model';
import { PressureDevice } from '../models/device.model';
import { Measurement } from '../models/measurement.model';
import { Saddle } from '../models/saddle.model';
import {
  BackendGroupDetail,
  BackendMeasurement,
  BackendMeasurementData,
  toMeasurement,
} from './measurement-mapper';

export const BACKEND_BASE_URL = 'http://localhost:5200/api';

export interface SaddleCategory {
  id: number;
  name: string;
}

/** Device categories of the development data, the backend has no endpoint to list them. */
export const DEVELOPMENT_DEVICE_CATEGORIES = [
  { name: 'Basic', id: 1 },
  { name: 'Standard', id: 2 },
  { name: 'Family', id: 3 },
  { name: 'Stable', id: 4 },
  { name: 'Professional', id: 5 },
] as const;

export interface MeasurementSummary {
  id: string;
  horseId: string;
  /** yyyy-mm-dd */
  date: string;
  riderName: string;
  saddleName: string;
}

export interface NewMeasurementGroup {
  name: string;
  date: Date;
  notes: string | null;
  personId: string;
  horseId: string;
  saddleId: string;
  deviceId: string;
}

export const DEVELOPMENT_HORSE_BREEDS = [
  { name: 'Hannoveraner', id: 1 },
  { name: 'Holsteiner', id: 2 },
  { name: 'Westfale', id: 3 },
  { name: 'Trakehner', id: 4 },
  { name: 'Haflinger', id: 5 },
  { name: 'Noriker', id: 6 },
  { name: 'Lipizzaner', id: 7 },
  { name: 'Quarter Horse', id: 8 },
] as const;

@Injectable({ providedIn: 'root' })
export class BackendApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = BACKEND_BASE_URL;

  getContacts(personId: string): Observable<Customer[]> {
    return this.http.get<unknown>(`${this.baseUrl}/persons/${personId}/contacts`).pipe(
      map(response => listFrom(response, 'contacts').map(mapContact)),
      catchError(toApiError),
    );
  }

  getPerson(personId: string): Observable<Customer> {
    return this.http.get<unknown>(`${this.baseUrl}/persons/${personId}`).pipe(
      map(mapPerson),
      catchError(toApiError),
    );
  }

  getHorses(personId: string): Observable<Horse[]> {
    return this.http.get<unknown>(`${this.baseUrl}/persons/${personId}/horses`).pipe(
      map(response => listFrom(response, 'horses').map((horse, index) => mapHorse(horse, personId, index))),
      catchError(toApiError),
    );
  }

  createHorse(ownerId: string, horse: Horse): Observable<Horse> {
    const values = horseRequestValues(horse);
    if (values instanceof Error) return throwError(() => values);

    return this.http.post<unknown>(`${this.baseUrl}/horses`, {
      ...values,
      ownerId: Number(ownerId),
    }).pipe(
      map(response => mapHorse(response, ownerId, 0)),
      catchError(toApiError),
    );
  }

  /** Saves all values of the horse, the image only exists in the frontend and is kept. */
  updateHorse(horse: Horse): Observable<Horse> {
    const values = horseRequestValues(horse);
    if (values instanceof Error) return throwError(() => values);

    return this.http.patch<unknown>(`${this.baseUrl}/horses/${horse.id}`, values).pipe(
      map(response => ({ ...mapHorse(response, horse.ownerId, 0), imageUrl: horse.imageUrl })),
      catchError(toApiError),
    );
  }

  /** Deletes the horse together with its saddles and measurements. */
  deleteHorse(horseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/horses/${horseId}`).pipe(catchError(toApiError));
  }

  getSaddles(horseId: string): Observable<Saddle[]> {
    return this.http.get<unknown>(`${this.baseUrl}/horses/${horseId}/saddles`).pipe(
      map(response => listFrom(response, 'saddles').map(saddle => mapSaddle(saddle, horseId))),
      catchError(toApiError),
    );
  }

  getDevices(personId: string): Observable<PressureDevice[]> {
    return this.http.get<unknown>(`${this.baseUrl}/persons/${personId}/devices`).pipe(
      map(response => listFrom(response, 'devices').map(mapDevice)),
      catchError(toApiError),
    );
  }

  updatePerson(personId: string, changes: Record<string, unknown>): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/persons/${personId}`, changes).pipe(
      catchError(toApiError),
    );
  }

  createPerson(request: Record<string, unknown>): Observable<string> {
    return this.http.post<unknown>(`${this.baseUrl}/persons`, request).pipe(
      map(mapCreatedPersonId),
      catchError(toApiError),
    );
  }

  addContact(personId: string, contactId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/persons/${personId}/contacts/${contactId}`, {}).pipe(
      catchError(toApiError),
    );
  }

  // ── Saddles ────────────────────────────────────────────────────────────────

  getSaddleCategories(): Observable<SaddleCategory[]> {
    return this.http.get<unknown>(`${this.baseUrl}/horses/saddles/categories`).pipe(
      map(response => listFrom(response, 'categories').map(category => {
        if (!isRecord(category)) throw new Error('Das Backend hat eine ungültige Sattelkategorie zurückgegeben.');
        return { id: numeric(category, 'id'), name: required(category, 'name') };
      })),
      catchError(toApiError),
    );
  }

  createSaddle(horseId: string, name: string, categoryId: number): Observable<Saddle> {
    return this.http.post<unknown>(`${this.baseUrl}/horses/${horseId}/saddles`, { name, categoryId }).pipe(
      map(response => mapSaddle(response, horseId)),
      catchError(error => {
        // The backend saves the saddle but fails to build the response (HTTP 500),
        // so load the saddles of the horse again and take the newest one with this name.
        if (error instanceof HttpErrorResponse && error.status === 500) {
          return this.getSaddles(horseId).pipe(map(saddles => {
            const created = saddles
              .filter(saddle => saddle.name === name)
              .sort((a, b) => Number(b.id) - Number(a.id))[0];
            if (!created) throw new Error('Der Sattel konnte nicht gespeichert werden.');
            return created;
          }));
        }
        return toApiError(error);
      }),
    );
  }

  // ── Devices ────────────────────────────────────────────────────────────────

  createDevice(deviceId: string, ownerId: string, categoryId: number): Observable<PressureDevice> {
    return this.http.post<unknown>(`${this.baseUrl}/devices`, {
      deviceId,
      ownerId: Number(ownerId),
      categoryId,
    }).pipe(
      map(mapDevice),
      catchError(toApiError),
    );
  }

  // ── Measurements ───────────────────────────────────────────────────────────

  /** Loads the measurement groups of a horse as short overview (no measurement data), newest first. */
  getMeasurementSummariesOfHorse(horseId: string): Observable<MeasurementSummary[]> {
    return this.http.get<unknown>(`${this.baseUrl}/measurementgroups/horse/${horseId}`).pipe(
      map(response => listFrom(response, 'groups')
        .filter(isRecord)
        .map(group => ({
          id: required(group, 'id'),
          horseId,
          date: required(group, 'date'),
          riderName: [optional(group, 'personFirstName'), optional(group, 'personLastName')].filter(Boolean).join(' '),
          saddleName: optional(group, 'saddleName') ?? '',
        }))
        .sort((a, b) => b.date.localeCompare(a.date))),
      catchError(toApiError),
    );
  }

  /** Name and average pressure (N/cm²) of a measurement group, the backend computes the average. */
  getMeasurementGroupOverview(groupId: string): Observable<{ name: string; meanNcm2: number | null }> {
    const groupUrl = `${this.baseUrl}/measurementgroups/${groupId}`;
    return forkJoin({
      group: this.http.get<BackendGroupDetail>(groupUrl),
      // 404 = keine Messdaten vorhanden
      average: this.http.get<{ value: number }>(`${groupUrl}/measurements/average`).pipe(
        map(response => response.value / 100),
        catchError(() => of(null)),
      ),
    }).pipe(
      map(({ group, average }) => ({ name: group.name, meanNcm2: average })),
      catchError(toApiError),
    );
  }

  /** Loads only the dates (yyyy-mm-dd) of the measurement groups of a horse, without any measurement data. */
  getMeasurementDatesOfHorse(horseId: string): Observable<string[]> {
    return this.http.get<unknown>(`${this.baseUrl}/measurementgroups/horse/${horseId}`).pipe(
      map(response => listFrom(response, 'groups')
        .map(group => isRecord(group) ? optional(group, 'date') : undefined)
        .filter((date): date is string => !!date)),
      catchError(toApiError),
    );
  }

  /**
   * Loads all measurement groups of a horse, newest first. Without [withData] the recorded frames are not loaded
   * (the measurements only have pace and hand), load them later with getMeasurement.
   */
  getMeasurementsOfHorse(horseId: string, withData = true): Observable<Measurement[]> {
    return this.http.get<unknown>(`${this.baseUrl}/measurementgroups/horse/${horseId}`).pipe(
      map(response => listFrom(response, 'groups').map(group => isRecord(group) ? required(group, 'id') : '')),
      switchMap(groupIds => groupIds.length
        ? forkJoin(groupIds.map(groupId => this.getMeasurementGroup(horseId, groupId, withData)))
        : of([] as Measurement[])),
      // groups without measurements count as deleted (see deleteMeasurementGroup)
      map(measurements => measurements
        .filter(measurement => (measurement.detail.segments?.length ?? 0) > 0)
        .sort((a, b) => toSortableDate(b.date).localeCompare(toSortableDate(a.date)))),
      catchError(toApiError),
    );
  }

  /** Loads a measurement group including all recorded frames. */
  getMeasurement(horseId: string, groupId: string): Observable<Measurement> {
    return this.getMeasurementGroup(horseId, groupId, true).pipe(catchError(toApiError));
  }

  private getMeasurementGroup(horseId: string, groupId: string, withData: boolean): Observable<Measurement> {
    const groupUrl = `${this.baseUrl}/measurementgroups/${groupId}`;
    return forkJoin({
      group: this.http.get<BackendGroupDetail>(groupUrl),
      measurements: this.http.get<unknown>(`${groupUrl}/measurements`).pipe(
        map(response => listFrom(response, 'measurements') as BackendMeasurement[]),
        switchMap(measurements => {
          if (!withData) return of(measurements.map(measurement => ({ measurement, data: [] })));
          return measurements.length
            ? forkJoin(measurements.map(measurement =>
                this.http.get<unknown>(`${groupUrl}/measurements/${measurement.id}/measurementData`).pipe(
                  map(response => ({
                    measurement,
                    data: listFrom(response, 'data') as BackendMeasurementData[],
                  })),
                )))
            : of([]);
        }),
      ),
    }).pipe(map(({ group, measurements }) => toMeasurement(horseId, group, measurements)));
  }

  /** Adds a measurement group and returns its id. */
  createMeasurementGroup(group: NewMeasurementGroup): Observable<string> {
    return this.http.post<unknown>(`${this.baseUrl}/measurementgroups`, {
      name: group.name.slice(0, 100),
      date: toIsoLocalDate(group.date),
      notes: group.notes ? group.notes.slice(0, 500) : null,
      personId: Number(group.personId),
      horseId: Number(group.horseId),
      saddleId: Number(group.saddleId),
      deviceId: group.deviceId,
    }).pipe(
      map(response => {
        if (!isRecord(response)) throw new Error('Das Backend hat keine gültige Messgruppe zurückgegeben.');
        return required(response, 'id');
      }),
      catchError(toApiError),
    );
  }

  /** Adds a measurement (pace + hand) to a measurement group. */
  createMeasurement(groupId: string, pace: string, hand: string): Observable<void> {
    return this.http.post<unknown>(`${this.baseUrl}/measurementgroups/${groupId}/measurements`, {
      pace,
      hand,
      description: null,
    }).pipe(
      map(() => undefined),
      catchError(toApiError),
    );
  }

  /**
   * Deletes all measurements of a group. The backend has no endpoint to delete a whole group,
   * the empty group is hidden by getMeasurementsOfHorse.
   */
  deleteMeasurementGroup(groupId: string): Observable<void> {
    const groupUrl = `${this.baseUrl}/measurementgroups/${groupId}`;
    return this.http.get<unknown>(`${groupUrl}/measurements`).pipe(
      map(response => listFrom(response, 'measurements') as BackendMeasurement[]),
      switchMap(measurements => measurements.length
        ? forkJoin(measurements.map(measurement =>
            this.http.delete<void>(`${groupUrl}/measurements/${measurement.id}`)))
        : of([])),
      map(() => undefined),
      catchError(toApiError),
    );
  }
}

function toIsoLocalDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** dd.mm.yyyy → yyyy-mm-dd, so dates can be compared as strings. */
function toSortableDate(value: string): string {
  const match = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value);
  return match ? `${match[3]}-${match[2]}-${match[1]}` : value;
}

function mapContact(value: unknown): Customer {
  if (!isRecord(value)) throw new Error('Das Backend hat einen ungültigen Kontakt zurückgegeben.');
  const id = required(value, 'id');
  const addressName = optional(value, 'addressName', 'address_name');
  const city = optional(value, 'cityName', 'city_name');
  const postalCode = optional(value, 'plz', 'PLZ', 'postalCode');
  const address = [addressName, [postalCode, city].filter(Boolean).join(' ')].filter(Boolean).join(', ');
  return {
    id,
    firstName: required(value, 'firstName', 'first_name'),
    lastName: required(value, 'lastName', 'last_name'),
    email: optional(value, 'email'),
    address: address || undefined,
    heightCm: 0,
    weightKg: 0,
    horseIds: [],
  };
}

function mapPerson(value: unknown): Customer {
  if (!isRecord(value)) throw new Error('Das Backend hat ungültige Personendaten zurückgegeben.');
  const address = value['address'];
  const addressText = isRecord(address)
    ? [optional(address, 'addressName', 'address_name'),
       [optional(address, 'plz', 'PLZ'), optional(address, 'cityName', 'city_name')].filter(Boolean).join(' ')]
        .filter(Boolean)
        .join(', ')
    : undefined;
  return {
    id: required(value, 'id'),
    firstName: required(value, 'firstName', 'first_name'),
    lastName: required(value, 'lastName', 'last_name'),
    email: optional(value, 'email'),
    websiteLink: optional(value, 'websiteLink', 'website_link'),
    description: optional(value, 'description'),
    birthDate: optional(value, 'dateOfBirth', 'date_of_birth'),
    createdAt: optional(value, 'createdAt', 'created_at'),
    address: addressText,
    heightCm: numeric(value, 'height', 'heightCm'),
    weightKg: numeric(value, 'weight', 'weightKg'),
    horseIds: [],
  };
}

function mapCreatedPersonId(value: unknown): string {
  if (typeof value === 'string' || typeof value === 'number') return String(value);
  if (isRecord(value)) {
    const id = value['id'] ?? value['personId'] ?? value['person_id'];
    if (typeof id === 'string' || typeof id === 'number') return String(id);
  }
  throw new Error('Das Backend hat nach dem Anlegen keine gültige Personen-ID zurückgegeben.');
}

function breedIdsForNames(value: string): number[] | null {
  const names = value.split(',').map(name => name.trim().toLocaleLowerCase('de')).filter(Boolean);
  const breedIds: Array<number | undefined> = names.map((name): number | undefined =>
    DEVELOPMENT_HORSE_BREEDS.find(breed => breed.name.toLocaleLowerCase('de') === name)?.id,
  );
  if (names.length === 0 || breedIds.some(id => id === undefined)) return null;
  return [...new Set(breedIds.filter((id): id is number => id !== undefined))];
}

function parseHorseAddress(value: string | undefined): { addressName: string | null; plz: string; cityName: string } | null {
  if (!value) return null;
  const match = /^(.*?),\s*(\d{4,10})\s+(.+)$/.exec(value.trim());
  if (!match) return null;
  return {
    addressName: match[1].trim() || null,
    plz: match[2],
    cityName: match[3].trim(),
  };
}

function toIsoDate(value: string | undefined): string {
  if (!value) return '';
  const germanDate = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value);
  return germanDate ? `${germanDate[3]}-${germanDate[2]}-${germanDate[1]}` : value;
}

/** The values of a horse as the backend expects them when adding or changing it. */
function horseRequestValues(horse: Horse) {
  const breedIds = breedIdsForNames(horse.breed);
  if (!breedIds) {
    const supportedBreeds = DEVELOPMENT_HORSE_BREEDS.map(breed => breed.name).join(', ');
    return new Error(`Bitte eine unterstützte Pferderasse angeben: ${supportedBreeds}.`);
  }
  const address = parseHorseAddress(horse.address);
  if (!address) {
    return new Error('Bitte den Standort als „Straße, PLZ Ort“ angeben.');
  }
  return {
    name: horse.name,
    dateOfBirth: toIsoDate(horse.birthDate),
    weight: horse.weightKg,
    height: horse.heightCm,
    gender: horse.sex === 'Männlich' ? 'Male' : 'Female',
    address,
    breedIds,
  };
}

function mapHorse(value: unknown, ownerId: string, index: number): Horse {
  if (!isRecord(value)) throw new Error(`Pferd ${index + 1} aus dem Backend ist ungültig.`);
  const address = value['address'];
  const addressText = isRecord(address)
    ? [optional(address, 'addressName', 'address_name'),
       [optional(address, 'plz', 'PLZ'), optional(address, 'cityName', 'city_name')].filter(Boolean).join(' ')]
        .filter(Boolean)
        .join(', ')
    : undefined;
  const birthDate = required(value, 'dateOfBirth', 'date_of_birth');
  const breeds = value['breedNames'] ?? value['breed_names'];
  const gender = String(value['gender'] ?? '').toLowerCase();
  return {
    id: required(value, 'id'),
    ownerId,
    name: required(value, 'name'),
    age: ageFromDate(birthDate),
    breed: Array.isArray(breeds) ? breeds.map(String).join(', ') : '–',
    heightCm: numeric(value, 'height', 'heightCm'),
    weightKg: numeric(value, 'weight', 'weightKg'),
    imageUrl: '',
    sex: gender === 'male' ? 'Männlich' : gender === 'female' ? 'Weiblich' : undefined,
    birthDate,
    address: addressText,
  };
}

function mapSaddle(value: unknown, horseId: string): Saddle {
  if (!isRecord(value)) throw new Error('Das Backend hat einen ungültigen Sattel zurückgegeben.');
  return {
    id: required(value, 'id'),
    horseId: optional(value, 'horseId', 'horse_id') ?? horseId,
    name: required(value, 'name'),
    category: required(value, 'categoryName', 'category_name'),
  };
}

function mapDevice(value: unknown): PressureDevice {
  if (!isRecord(value)) throw new Error('Das Backend hat ein ungültiges Messgerät zurückgegeben.');
  const number = required(value, 'id');
  return {
    id: number,
    number,
    name: optional(value, 'categoryName', 'category_name') ?? 'Satteldruckmessgerät',
  };
}

function listFrom(value: unknown, key: string): unknown[] {
  if (Array.isArray(value)) return value;
  if (isRecord(value) && Array.isArray(value[key])) return value[key] as unknown[];
  throw new Error('Das Backend hat keine gültige Liste zurückgegeben.');
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function required(record: Record<string, unknown>, ...keys: string[]): string {
  const value = keys.map(key => record[key]).find(entry => entry !== undefined && entry !== null);
  if (value === undefined) throw new Error(`Backend-Antwort enthält kein ${keys[0]}.`);
  return String(value);
}

function optional(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  const value = keys.map(key => record[key]).find(entry => entry !== undefined && entry !== null);
  return value === undefined ? undefined : String(value);
}

function numeric(record: Record<string, unknown>, ...keys: string[]): number {
  const value = keys.map(key => record[key]).find(entry => entry !== undefined && entry !== null);
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function ageFromDate(value: string): number {
  const birthDate = new Date(value);
  if (Number.isNaN(birthDate.getTime())) return 0;
  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  if (
    today.getMonth() < birthDate.getMonth() ||
    (today.getMonth() === birthDate.getMonth() && today.getDate() < birthDate.getDate())
  ) age--;
  return Math.max(0, age);
}

export function toApiError(error: unknown) {
  if (error instanceof HttpErrorResponse) {
    const body: unknown = error.error;
    if (isRecord(body)) {
      const detail = body['detail'] ?? body['title'];
      if (typeof detail === 'string' && detail) return throwError(() => new Error(detail));
      const errors = body['errors'];
      if (isRecord(errors)) {
        const message = Object.values(errors).flat().find(item => typeof item === 'string');
        if (typeof message === 'string') return throwError(() => new Error(message));
      }
    }
    return throwError(() => new Error(
      error.status === 0
        ? 'Das Backend ist nicht erreichbar. Prüfe, ob der lokale Server läuft.'
        : `Backend-Anfrage fehlgeschlagen (HTTP ${error.status}).`,
    ));
  }
  return throwError(() => error instanceof Error ? error : new Error('Backend-Anfrage fehlgeschlagen.'));
}
