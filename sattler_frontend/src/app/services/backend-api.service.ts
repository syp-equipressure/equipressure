import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface BackendAddress {
  id: number;
  addressName: string | null;
  cityName: string;
  plz: string;
}

export interface BackendPerson {
  id: number;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  height: number;
  weight: number;
  email: string | null;
  websiteLink: string | null;
  description: string | null;
  address: BackendAddress;
  roles: string[];
}

export interface BackendContact {
  id: number;
  firstName: string;
  lastName: string;
  addressName: string | null;
  cityName: string;
  plz: string;
  websiteLink: string | null;
  description: string | null;
}

export interface BackendHorse {
  id: number;
  name: string;
  dateOfBirth: string;
  weight: number;
  height: number;
  gender: 'Female' | 'Male';
  breedNames: string[];
  address: BackendAddress;
}

export interface BackendSaddle {
  id: number;
  name: string;
  horseId: number;
  categoryId: number;
  categoryName: string;
}

export interface BackendAddressRequest {
  addressName: string | null;
  cityName: string;
  plz: string;
}

@Injectable({ providedIn: 'root' })
export class BackendApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5200/api';

  readonly currentPersonId = 1;

  getPerson(id: number): Promise<BackendPerson> {
    return firstValueFrom(this.http.get<BackendPerson>(`${this.baseUrl}/persons/${id}`));
  }

  getContacts(id: number): Promise<{ contacts: BackendContact[] }> {
    return firstValueFrom(
      this.http.get<{ contacts: BackendContact[] }>(`${this.baseUrl}/persons/${id}/contacts`),
    );
  }

  getHorses(id: number): Promise<{ horses: BackendHorse[] }> {
    return firstValueFrom(
      this.http.get<{ horses: BackendHorse[] }>(`${this.baseUrl}/persons/${id}/horses`),
    );
  }

  getSaddles(horseId: number): Promise<{ saddles: BackendSaddle[] }> {
    return firstValueFrom(
      this.http.get<{ saddles: BackendSaddle[] }>(`${this.baseUrl}/horses/${horseId}/saddles`),
    );
  }

  createPerson(request: {
    firstName: string;
    lastName: string;
    height: number;
    weight: number;
    dateOfBirth: string;
    email: string | null;
    websiteLink: string | null;
    description: string | null;
    address: BackendAddressRequest;
    role: 'Equestrian';
  }): Promise<BackendPerson> {
    return firstValueFrom(this.http.post<BackendPerson>(`${this.baseUrl}/persons`, request));
  }

  addContact(personId: number, contactId: number): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.baseUrl}/persons/${personId}/contacts/${contactId}`, null),
    );
  }

  updatePerson(
    id: number,
    request: {
      firstName?: string;
      lastName?: string;
      email?: string;
      websiteLink?: string | null;
      description?: string | null;
      address?: BackendAddressRequest;
    },
  ): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`${this.baseUrl}/persons/${id}`, request));
  }

  createHorse(request: {
    name: string;
    dateOfBirth: string;
    weight: number;
    height: number;
    gender: BackendHorse['gender'];
    address: BackendAddressRequest;
    breedIds: number[];
    ownerId: number;
  }): Promise<BackendHorse> {
    return firstValueFrom(this.http.post<BackendHorse>(`${this.baseUrl}/horses`, request));
  }
}
