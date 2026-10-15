import { Injectable, signal } from '@angular/core';

const PERSON_ID_KEY = 'equipressure.backendPersonId';

@Injectable({ providedIn: 'root' })
export class BackendPersonContextService {
  private readonly storedId = readStoredPersonId();
  readonly personId = signal(this.storedId);

  savePersonId(value: string): boolean {
    const normalized = value.trim();
    if (!normalized) {
      localStorage.removeItem(PERSON_ID_KEY);
      this.personId.set('');
      return true;
    }
    if (!/^[1-9]\d*$/.test(normalized)) {
      return false;
    }

    localStorage.setItem(PERSON_ID_KEY, normalized);
    this.personId.set(normalized);
    return true;
  }
}

function readStoredPersonId(): string {
  if (typeof localStorage === 'undefined') {
    return '';
  }
  const id = localStorage.getItem(PERSON_ID_KEY) ?? '';
  return /^[1-9]\d*$/.test(id) ? id : '';
}
