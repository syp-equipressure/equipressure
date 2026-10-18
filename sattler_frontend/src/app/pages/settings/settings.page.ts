import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SettingsService, UserPreferences } from '../../services/settings.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { BackendPersonContextService } from '../../services/backend-person-context.service';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss',
})
export class SettingsPage {
  private readonly settings = inject(SettingsService);
  private readonly personContext = inject(BackendPersonContextService);

  readonly backendPersonId = signal(this.personContext.personId());
  readonly backendPersonFeedback = signal('');
  readonly backendPersonFeedbackIsError = signal(false);
  readonly preferences = signal<UserPreferences>(copyPreferences(this.settings.preferences()));
  readonly preferencesSaving = signal(false);
  readonly notificationsFeedback = signal('');
  readonly notificationsFeedbackIsError = signal(false);
  readonly localeFeedback = signal('');
  readonly localeFeedbackIsError = signal(false);

  savePreferences(section: 'notifications' | 'locale'): void {
    this.preferencesSaving.set(true);
    const feedback = section === 'notifications' ? this.notificationsFeedback : this.localeFeedback;
    const feedbackIsError =
      section === 'notifications' ? this.notificationsFeedbackIsError : this.localeFeedbackIsError;
    feedback.set('');
    try {
      this.settings.savePreferences(this.preferences());
      feedback.set(
        'Auf diesem Gerät gespeichert. Für eine Synchronisierung mit deinem Konto fehlt noch der Backend-Endpunkt.',
      );
      feedbackIsError.set(false);
    } catch {
      feedback.set('Die Einstellungen konnten auf diesem Gerät nicht gespeichert werden.');
      feedbackIsError.set(true);
    } finally {
      this.preferencesSaving.set(false);
    }
  }

  updateNotification(key: keyof UserPreferences['notifications'], enabled: boolean): void {
    this.preferences.update(preferences => ({
      ...preferences,
      notifications: { ...preferences.notifications, [key]: enabled },
    }));
  }

  updateLanguage(language: UserPreferences['language']): void {
    this.preferences.update(preferences => ({ ...preferences, language }));
  }

  updateDateFormat(dateFormat: UserPreferences['dateFormat']): void {
    this.preferences.update(preferences => ({ ...preferences, dateFormat }));
  }

  updateTimeFormat(timeFormat: UserPreferences['timeFormat']): void {
    this.preferences.update(preferences => ({ ...preferences, timeFormat }));
  }

  saveBackendPersonId(): void {
    const id = this.backendPersonId();
    if (!this.personContext.savePersonId(id)) {
      this.backendPersonFeedback.set('Bitte eine positive numerische Personen-ID eingeben.');
      this.backendPersonFeedbackIsError.set(true);
      return;
    }
    this.backendPersonFeedback.set(
      id.trim()
        ? 'Personen-ID gespeichert. Kontakte, Pferde und Messgeräte verwenden diese ID.'
        : 'Personen-ID entfernt. Es werden wieder Demo-Daten angezeigt.',
    );
    this.backendPersonFeedbackIsError.set(false);
  }

}

function copyPreferences(preferences: UserPreferences): UserPreferences {
  return {
    ...preferences,
    notifications: { ...preferences.notifications },
  };
}
