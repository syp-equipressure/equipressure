import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

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
}

function extractPeople(response: unknown): BackendPerson[] {
  const records = Array.isArray(response)
    ? response
    : isRecord(response)
      ? Object.values(response).find(Array.isArray)
      : undefined;

  if (!Array.isArray(records)) {
    throw new Error('Das Backend hat keine gültige Personenliste zurückgegeben.');
  }

  return records.map((record, index) => mapPerson(record, index));
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
