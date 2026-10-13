import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { Horse } from '../models/horse.model';

export interface BackendPerson {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  heightCm: number;
  weightKg: number;
  horseIds: string[];
}

@Injectable({ providedIn: 'root' })
export class PersonApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5200/api';

  getContacts(personId: string): Observable<BackendPerson[]> {
    return this.http
      .get<unknown>(`${this.baseUrl}/persons/${personId}/contacts`)
      .pipe(map(response => extractPeople(response)));
  }

  getFavourites(personId: string): Observable<BackendPerson[]> {
    return this.http
      .get<unknown>(`${this.baseUrl}/persons/${personId}/favourites`)
      .pipe(map(response => extractPeople(response)));
  }

  getHorses(personId: string): Observable<Horse[]> {
    return this.http
      .get<unknown>(`${this.baseUrl}/persons/${personId}/horses`)
      .pipe(
        map(response =>
          extractList(response, ['horses']).map((horse, index) =>
            mapHorse(horse, personId, index),
          ),
        ),
      );
  }
}

function extractPeople(response: unknown): BackendPerson[] {
  return extractList(response, ['persons', 'people']).map((record, index) =>
    mapPerson(record, index),
  );
}

function extractList(response: unknown, keys: string[]): unknown[] {
  if (Array.isArray(response)) return response;
  if (isRecord(response)) {
    for (const key of keys) {
      if (Array.isArray(response[key])) return response[key] as unknown[];
    }
  }
  throw new Error('Das Backend hat keine gültige Liste zurückgegeben.');
}

function mapPerson(value: unknown, index: number): BackendPerson {
  if (!isRecord(value)) {
    throw new Error(`Person an Listenposition ${index + 1} ist ungültig.`);
  }

  const id = firstDefined(value, 'id', 'personId', 'person_id');
  const firstName = firstDefined(value, 'firstName', 'firstname', 'first_name');
  const lastName = firstDefined(value, 'lastName', 'lastname', 'last_name');
  if (id === undefined || firstName === undefined || lastName === undefined) {
    throw new Error(`Person an Listenposition ${index + 1} enthält keine ID oder Namen.`);
  }

  return {
    id: String(id),
    firstName: String(firstName),
    lastName: String(lastName),
    email: optionalString(value, 'email'),
    phoneNumber: optionalString(value, 'phoneNumber', 'phone_number'),
    address: optionalString(value, 'address', 'addressName', 'address_name'),
    heightCm: numericValue(value, 'height', 'heightCm', 'height_cm'),
    weightKg: numericValue(value, 'weight', 'weightKg', 'weight_kg'),
    horseIds: [],
  };
}

function mapHorse(value: unknown, ownerId: string, index: number): Horse {
  if (!isRecord(value)) {
    throw new Error(`Pferd an Listenposition ${index + 1} ist ungültig.`);
  }

  const id = firstDefined(value, 'id', 'horseId', 'horse_id');
  const name = firstDefined(value, 'name');
  if (id === undefined || name === undefined) {
    throw new Error(`Pferd an Listenposition ${index + 1} enthält keine ID oder keinen Namen.`);
  }

  const birthDate = optionalString(value, 'dateOfBirth', 'birthDate', 'date_of_birth');
  return {
    id: String(id),
    ownerId: String(firstDefined(value, 'ownerId', 'owner_id') ?? ownerId),
    name: String(name),
    age: numericValue(value, 'age') || ageFromDate(birthDate),
    breed: formatBreed(firstDefined(value, 'breeds', 'breed')),
    heightCm: numericValue(value, 'height', 'heightCm', 'height_cm'),
    weightKg: numericValue(value, 'weight', 'weightKg', 'weight_kg'),
    imageUrl: optionalString(value, 'imageUrl', 'image_url') ?? '',
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function firstDefined(record: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (record[key] !== undefined && record[key] !== null) {
      return record[key];
    }
  }
  return undefined;
}

function optionalString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  const value = firstDefined(record, ...keys);
  return value === undefined ? undefined : String(value);
}

function numericValue(record: Record<string, unknown>, ...keys: string[]): number {
  const value = firstDefined(record, ...keys);
  const number = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(number) ? number : 0;
}

function formatBreed(value: unknown): string {
  if (typeof value === 'string') return value;
  if (!Array.isArray(value)) return '–';
  return value
    .map(breed =>
      typeof breed === 'string'
        ? breed
        : isRecord(breed)
          ? firstDefined(breed, 'name', 'breedName', 'breed_name')
          : undefined,
    )
    .filter((breed): breed is string => typeof breed === 'string')
    .join(', ') || '–';
}

function ageFromDate(value: string | undefined): number {
  if (!value) return 0;
  const birthDate = new Date(value);
  if (Number.isNaN(birthDate.getTime())) return 0;
  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  if (
    today.getMonth() < birthDate.getMonth() ||
    (today.getMonth() === birthDate.getMonth() && today.getDate() < birthDate.getDate())
  ) {
    age--;
  }
  return Math.max(0, age);
}
