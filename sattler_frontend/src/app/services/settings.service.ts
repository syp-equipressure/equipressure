import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export interface UserPreferences {
  notifications: {
    chatMessages: boolean;
    measurementUpdates: boolean;
    accountSecurity: boolean;
  };
  language: 'de' | 'en';
  dateFormat: 'dd.mm.yyyy' | 'yyyy-mm-dd' | 'mm/dd/yyyy';
  timeFormat: '24h' | '12h';
}

const DEFAULT_PREFERENCES: UserPreferences = {
  notifications: {
    chatMessages: true,
    measurementUpdates: true,
    accountSecurity: true,
  },
  language: 'de',
  dateFormat: 'dd.mm.yyyy',
  timeFormat: '24h',
};

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'equipressure-user-preferences';
  private readonly storedPreferences = signal(this.readPreferences());
  readonly preferences = this.storedPreferences.asReadonly();

  savePreferences(preferences: UserPreferences): void {
    this.document.defaultView?.localStorage.setItem(this.storageKey, JSON.stringify(preferences));
    this.storedPreferences.set(preferences);
  }

  async updatePreferencesOnServer(_preferences: UserPreferences): Promise<void> {
    // TODO: Send preferences to the account settings endpoint when it is available.
    throw new Error('Der Backend-Endpunkt zum Speichern der Einstellungen fehlt.');
  }

  private readPreferences(): UserPreferences {
    const stored = this.document.defaultView?.localStorage.getItem(this.storageKey);
    if (!stored) return structuredClone(DEFAULT_PREFERENCES);

    try {
      const value: unknown = JSON.parse(stored);
      if (isUserPreferences(value)) return value;
    } catch {
      // Ignore malformed cached preferences and start with the defaults.
    }

    return structuredClone(DEFAULT_PREFERENCES);
  }
}

function isUserPreferences(value: unknown): value is UserPreferences {
  if (!value || typeof value !== 'object') return false;
  const preferences = value as Partial<UserPreferences>;
  const notifications = preferences.notifications;
  return (
    !!notifications &&
    typeof notifications.chatMessages === 'boolean' &&
    typeof notifications.measurementUpdates === 'boolean' &&
    typeof notifications.accountSecurity === 'boolean' &&
    (preferences.language === 'de' || preferences.language === 'en') &&
    (preferences.dateFormat === 'dd.mm.yyyy' ||
      preferences.dateFormat === 'yyyy-mm-dd' ||
      preferences.dateFormat === 'mm/dd/yyyy') &&
    (preferences.timeFormat === '24h' || preferences.timeFormat === '12h')
  );
}
