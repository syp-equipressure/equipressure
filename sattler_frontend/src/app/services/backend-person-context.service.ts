import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

const PERSON_ID_KEY = 'equipressure.backendPersonId';

@Injectable({ providedIn: 'root' })
export class BackendPersonContextService {
  private readonly document = inject(DOCUMENT);
  readonly personId = signal(this.readPersonId());

  savePersonId(value: string): boolean {
    const normalized = value.trim();
    if (normalized && !/^[1-9]\d*$/.test(normalized)) return false;
    this.document.defaultView?.localStorage.setItem(PERSON_ID_KEY, normalized);
    this.personId.set(normalized);
    return true;
  }

  private readPersonId(): string {
    const id = this.document.defaultView?.localStorage.getItem(PERSON_ID_KEY) ?? '';
    return /^[1-9]\d*$/.test(id) ? id : '';
  }
}
