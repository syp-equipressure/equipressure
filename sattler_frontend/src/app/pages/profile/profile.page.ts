import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Sattler, sattlerFullName } from '../../models/sattler.model';

type EditableField = 'firstName' | 'lastName' | 'email' | 'phoneNumber' | 'website';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss',
})
export class ProfilePage {
  private readonly data = inject(DataService);

  readonly sattler = this.data.sattler;

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
  readonly address = signal('');
  readonly website = signal('');
  readonly description = signal('');

  readonly errors = signal<Partial<Record<EditableField, string>>>({});
  readonly avatarError = signal('');

  startEdit(): void {
    const s = this.sattler();
    this.firstName.set(s.firstName);
    this.lastName.set(s.lastName);
    this.email.set(s.email);
    this.phoneNumber.set(s.phoneNumber ?? '');
    this.companyName.set(s.companyName ?? '');
    this.address.set(s.address ?? '');
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

    this.data.updateSattler(patch);
    this.editMode.set(false);
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
    this.errors.set(e);
    return Object.keys(e).length === 0;
  }

  uploadAvatar(event: Event): void {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    this.avatarError.set('');

    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.avatarError.set('Bitte eine Bilddatei auswählen.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.avatarError.set('Das Bild darf höchstens 5 MB groß sein.');
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
