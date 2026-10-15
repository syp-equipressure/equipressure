import { Component, computed, effect, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Sattler, sattlerFullName } from '../../models/sattler.model';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService } from '../../services/person-api.service';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss',
})
export class ProfilePage {
  private readonly data = inject(DataService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);

  readonly sattler = this.data.sattler;
  readonly backendPersonId = this.personContext.personId;
  readonly loadingBackendProfile = signal(false);
  readonly backendProfileError = signal('');

  readonly fullName = computed(() => sattlerFullName(this.sattler()));

  readonly initials = computed(() => {
    const s = this.sattler();
    return `${s.firstName.charAt(0)}${s.lastName.charAt(0)}`.toUpperCase();
  });

  constructor() {
    effect(onCleanup => {
      const personId = this.backendPersonId();
      this.backendProfileError.set('');
      if (!personId) {
        this.loadingBackendProfile.set(false);
        return;
      }

      this.loadingBackendProfile.set(true);
      const subscription = forkJoin({
        details: this.personApi.getPersonDetails(personId),
        profile: this.personApi.getProfileData(personId),
        location: this.personApi.getLocation(personId),
      }).subscribe({
        next: ({ details, profile, location }) => {
          const address = [
            location.addressName,
            `${location.postalCode} ${location.cityName}`,
          ]
            .filter(Boolean)
            .join(', ');
          this.data.updateSattler({
            id: details.id,
            firstName: profile.firstName,
            lastName: profile.lastName,
            email: details.email ?? '',
            phoneNumber: undefined,
            companyName: undefined,
            address,
            website: details.websiteLink,
            description: details.description,
          });
          this.loadingBackendProfile.set(false);
        },
        error: error => {
          this.backendProfileError.set(
            error instanceof Error
              ? error.message
              : 'Das Profil konnte nicht aus dem Backend geladen werden.',
          );
          this.loadingBackendProfile.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }
}
