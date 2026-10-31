import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Sattler, sattlerFullName } from '../../models/sattler.model';
import { BackendApiService } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { addressError, composeAddress, splitAddress } from '../../utils/address';
import { imageFileError } from '../../utils/image';

type EditableField = 'firstName' | 'lastName' | 'email' | 'phoneNumber' | 'website' | 'address';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, RouterLink],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss',
})
export class ProfilePage {
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);

  readonly sattler = this.data.sattler;
  readonly backendPersonId = this.personContext.personId;
  readonly backendProfileLoading = signal(false);
  readonly backendProfileSaving = signal(false);
  readonly backendProfileError = signal('');
  readonly backendProfileNotice = signal('');
  readonly deviceCount = computed(() => this.data.devices().length);

  readonly fullName = computed(() => sattlerFullName(this.sattler()));

  readonly initials = computed(() => {
    const s = this.sattler();
    return `${s.firstName.charAt(0)}${s.lastName.charAt(0)}`.toUpperCase();
  });

  // ── Bearbeiten ───────────────────────────────────────────────────────────────

  readonly editMode = signal(false);

  readonly firstName = signal('');
  readonly lastName = signal('');
  readonly email = signal('');
  readonly phoneNumber = signal('');
  readonly companyName = signal('');
  readonly street = signal('');
  readonly postalCode = signal('');
  readonly city = signal('');
  /** Die Adresse in der Form "Straße, PLZ Ort". */
  readonly address = computed(() => composeAddress(this.street(), this.postalCode(), this.city()));
  readonly website = signal('');
  readonly description = signal('');

  readonly errors = signal<Partial<Record<EditableField, string>>>({});
  readonly avatarError = signal('');

  constructor() {
    effect(onCleanup => {
      const personId = this.backendPersonId();
      this.backendProfileError.set('');
      this.backendProfileNotice.set('');
      if (!personId) {
        this.backendProfileLoading.set(false);
        return;
      }
      this.backendProfileLoading.set(true);
      const subscription = this.api.getPerson(personId).subscribe({
        next: customer => {
          this.data.updateSattler({
            id: customer.id,
            firstName: customer.firstName,
            lastName: customer.lastName,
            email: customer.email ?? '',
            address: customer.address,
            website: customer.websiteLink,
            description: customer.description,
            phoneNumber: undefined,
            companyName: undefined,
            memberSince: formatMemberSince(customer.createdAt),
          });
          if (customer.imageUpdatedAt) this.loadAvatar(personId);
          this.backendProfileLoading.set(false);
        },
        error: error => {
          this.backendProfileError.set(
            error instanceof Error ? error.message : 'Das Sattlerprofil konnte nicht geladen werden.',
          );
          this.backendProfileLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  startEdit(): void {
    const s = this.sattler();
    this.firstName.set(s.firstName);
    this.lastName.set(s.lastName);
    this.email.set(s.email);
    this.phoneNumber.set(s.phoneNumber ?? '');
    this.companyName.set(s.companyName ?? '');
    const address = splitAddress(s.address);
    this.street.set(address.street);
    this.postalCode.set(address.postalCode);
    this.city.set(address.city);
    this.website.set(s.website ?? '');
    this.description.set(s.description ?? '');
    this.errors.set({});
    this.editMode.set(true);
  }

  cancel(): void {
    this.editMode.set(false);
    this.errors.set({});
  }

  save(): void {
    if (!this.validate()) return;

    const patch: Partial<Sattler> = {
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      email: this.email().trim(),
      phoneNumber: this.phoneNumber().trim() || undefined,
      companyName: this.companyName().trim() || undefined,
      address: this.address().trim() || undefined,
      website: normalizeWebsite(this.website()) || undefined,
      description: this.description().trim() || undefined,
    };

    const personId = this.backendPersonId();
    if (!personId) {
      this.data.updateSattler(patch);
      this.editMode.set(false);
      return;
    }

    const changes: Record<string, unknown> = {
      firstName: patch.firstName,
      lastName: patch.lastName,
      email: patch.email,
      websiteLink: patch.website ?? '',
      description: patch.description ?? '',
    };
    const addressChanged = patch.address !== this.sattler().address;
    if (addressChanged) {
      // ein Sattler braucht im Backend eine Adresse mit Straße
      const addressProblem = addressError(
        { street: this.street(), postalCode: this.postalCode(), city: this.city() },
        true,
        true,
      );
      const address = parseAddress(patch.address ?? '');
      if (addressProblem || !address) {
        this.errors.update(errors => ({
          ...errors,
          address: addressProblem ?? 'Bitte Straße, PLZ und Ort angeben.',
        }));
        return;
      }
      changes['address'] = address;
    }

    this.backendProfileError.set('');
    this.backendProfileNotice.set('');
    this.backendProfileSaving.set(true);
    this.api.updatePerson(personId, changes).subscribe({
      next: () => {
        this.data.updateSattler(patch);
        this.backendProfileSaving.set(false);
        this.backendProfileNotice.set('Profil im Backend gespeichert.');
        this.editMode.set(false);
      },
      error: error => {
        this.backendProfileSaving.set(false);
        this.backendProfileError.set(
          error instanceof Error ? error.message : 'Das Profil konnte nicht gespeichert werden.',
        );
      },
    });
  }

  private validate(): boolean {
    const e: Partial<Record<EditableField, string>> = {};
    if (!this.firstName().trim()) e.firstName = 'Vorname ist erforderlich.';
    if (!this.lastName().trim()) e.lastName = 'Nachname ist erforderlich.';
    const email = this.email().trim();
    if (!email) {
      e.email = 'E-Mail ist erforderlich.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      e.email = 'Bitte eine gültige E-Mail-Adresse eingeben.';
    }
    const phoneNumber = this.phoneNumber().trim();
    if (
      phoneNumber &&
      (!/^\+?[0-9\s()./-]+$/.test(phoneNumber) ||
        phoneNumber.replace(/\D/g, '').length < 7 ||
        phoneNumber.replace(/\D/g, '').length > 15)
    ) {
      e.phoneNumber = 'Bitte eine gültige Telefonnummer eingeben.';
    }
    const website = this.website().trim();
    if (website && !isValidWebsite(website)) {
      e.website = 'Bitte eine gültige Website eingeben.';
    }
    const addressProblem = addressError(
      { street: this.street(), postalCode: this.postalCode(), city: this.city() },
      false,
    );
    if (addressProblem) e.address = addressProblem;
    this.errors.set(e);
    return Object.keys(e).length === 0;
  }

  uploadAvatar(event: Event): void {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    this.avatarError.set('');

    if (!file) return;
    const fileError = imageFileError(file);
    if (fileError) {
      this.avatarError.set(fileError);
      return;
    }

    const personId = this.backendPersonId();
    if (personId) {
      this.api.uploadPersonImage(personId, file).subscribe({
        next: avatarUrl => this.data.updateSattler({ avatarUrl }),
        error: error => this.avatarError.set(
          error instanceof Error ? error.message : 'Das Bild konnte nicht gespeichert werden.',
        ),
      });
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        this.data.updateSattler({ avatarUrl: reader.result });
      } else {
        this.avatarError.set('Das Bild konnte nicht geladen werden.');
      }
    };
    reader.onerror = () => this.avatarError.set('Das Bild konnte nicht geladen werden.');
    reader.readAsDataURL(file);
  }

  private loadAvatar(personId: string): void {
    this.api.getPersonImage(personId).subscribe({
      next: avatarUrl => this.data.updateSattler({ avatarUrl }),
      error: () => this.avatarError.set('Das Bild konnte nicht geladen werden.'),
    });
  }
}

function parseAddress(value: string): { addressName: string; cityName: string; plz: string } | null {
  const match = /^(.*?),\s*(\d{4,})\s+(.+)$/.exec(value.trim());
  if (!match) return null;
  return { addressName: match[1].trim(), plz: match[2], cityName: match[3].trim() };
}

function normalizeWebsite(website: string): string {
  const value = website.trim();
  return value && !/^https?:\/\//i.test(value) ? `https://${value}` : value;
}

function isValidWebsite(website: string): boolean {
  try {
    const url = new URL(normalizeWebsite(website));
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

/** "2026-10-27T18:22:49Z" → "10/2026" */
function formatMemberSince(createdAt: string | undefined): string {
  const date = createdAt ? new Date(createdAt) : null;
  if (!date || Number.isNaN(date.getTime())) return '';
  return `${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
}
