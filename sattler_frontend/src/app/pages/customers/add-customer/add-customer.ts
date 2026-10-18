import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {Customer} from '../../../models/customer.model';
import { BackendApiService } from '../../../services/backend-api.service';
import { BackendPersonContextService } from '../../../services/backend-person-context.service';

@Component({
  selector: 'app-new-customer-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-customer.html',
  styleUrl: './add-customer.scss',
})
export class AddCustomer {
  readonly backendMode = input(false);
  readonly maxBirthDate = todayDate();
  readonly saved = output<Customer>();
  readonly cancelled = output<void>();
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);
  readonly pendingContactId = signal<string | null>(null);

  firstName = signal('');
  lastName = signal('');
  email = signal('');
  birthDate = signal('');
  address = signal('');
  heightCm = signal<number | null>(null);
  weightKg = signal<number | null>(null);

  errors = signal<Partial<Record<keyof Customer, string>>>({});
  saveError = signal('');
  saving = signal(false);

  private validate(): boolean {
    const e: Partial<Record<keyof Customer, string>> = {};
    if (!this.firstName().trim()) e.firstName = 'Vorname ist erforderlich.';
    if (!this.lastName().trim()) e.lastName = 'Nachname ist erforderlich.';
    if (this.backendMode() && !this.birthDate()) e.birthDate = 'Geburtsdatum ist erforderlich.';
    if (this.backendMode() && this.birthDate() && !isValidPastDate(this.birthDate())) {
      e.birthDate = 'Bitte ein gültiges Geburtsdatum in der Vergangenheit eingeben.';
    }
    if (this.backendMode() && !parseAddress(this.address())) {
      e.address = 'Bitte „Straße, PLZ Ort“ angeben.';
    }
    if (this.email() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email())) {
      e.email = 'Bitte eine gültige E-Mail-Adresse eingeben.';
    }
    if (!this.heightCm() || this.heightCm()! <= 0) {
      e.heightCm = 'Körpergröße ist erforderlich.';
    }
    if (!this.weightKg() || this.weightKg()! <= 0) {
      e.weightKg = 'Gewicht ist erforderlich.';
    }
    this.errors.set(e);
    return Object.keys(e).length === 0;
  }

  submit(): void {
    if (!this.validate()) return;

    if (this.backendMode()) {
      this.submitToBackend();
      return;
    }

    const newCustomer: Customer = {
      id: crypto.randomUUID(),
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      email: this.email().trim() || undefined,
      birthDate: this.birthDate() || undefined,
      address: this.address().trim() || undefined,
      heightCm: this.heightCm()!,
      weightKg: this.weightKg()!,
      horseIds: [],
    };

    this.saved.emit(newCustomer);
    this.reset();
  }

  cancel(): void {
    if (this.pendingContactId()) return;
    this.reset();
    this.cancelled.emit();
  }

  private reset(): void {
    this.firstName.set('');
    this.lastName.set('');
    this.email.set('');
    this.birthDate.set('');
    this.address.set('');
    this.heightCm.set(null);
    this.weightKg.set(null);
    this.errors.set({});
    this.saveError.set('');
    this.pendingContactId.set(null);
  }

  private submitToBackend(): void {
    if (this.saving()) return;
    const personId = this.personContext.personId();
    const pendingContactId = this.pendingContactId();
    this.saving.set(true);
    this.saveError.set('');

    const finish = (customer: Customer): void => {
      this.saving.set(false);
      this.saved.emit(customer);
      this.reset();
    };

    if (pendingContactId) {
      this.api.addContact(personId, pendingContactId).subscribe({
        next: () => {
          this.pendingContactId.set(null);
          finish({
            id: pendingContactId,
            firstName: this.firstName().trim(),
            lastName: this.lastName().trim(),
            email: this.email().trim() || undefined,
            birthDate: this.birthDate(),
            address: this.address().trim(),
            heightCm: this.heightCm()!,
            weightKg: this.weightKg()!,
            horseIds: [],
          });
        },
        error: error => {
          this.saving.set(false);
          this.saveError.set(error instanceof Error ? error.message : 'Kontakt konnte nicht hinzugefügt werden.');
        },
      });
      return;
    }

    const address = parseAddress(this.address())!;
    this.api.createPerson({
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      height: this.heightCm(),
      weight: this.weightKg(),
      dateOfBirth: this.birthDate(),
      email: this.email().trim() || null,
      websiteLink: null,
      description: null,
      address,
      role: 'Equestrian',
    }).subscribe({
      next: newPersonId => {
        const customer: Customer = {
          id: newPersonId,
          firstName: this.firstName().trim(),
          lastName: this.lastName().trim(),
          email: this.email().trim() || undefined,
          birthDate: this.birthDate(),
          address: this.address().trim(),
          heightCm: this.heightCm()!,
          weightKg: this.weightKg()!,
          horseIds: [],
        };
        this.pendingContactId.set(newPersonId);
        this.api.addContact(personId, newPersonId).subscribe({
          next: () => {
            this.pendingContactId.set(null);
            finish(customer);
          },
          error: error => {
            this.saving.set(false);
            this.saveError.set(
              `Person ${customer.id} wurde angelegt, konnte aber nicht als Kontakt verknüpft werden. ` +
              `${error instanceof Error ? error.message : 'Speichern erneut ausführen, um die Verknüpfung zu wiederholen.'}`,
            );
          },
        });
      },
      error: error => {
        this.saving.set(false);
        this.saveError.set(error instanceof Error ? error.message : 'Person konnte nicht angelegt werden.');
      },
    });
  }
}

function parseAddress(value: string): { addressName: string; plz: string; cityName: string } | null {
  const match = /^(.*?),\s*(\d{4,})\s+(.+)$/.exec(value.trim());
  if (!match) return null;
  return { addressName: match[1].trim(), plz: match[2], cityName: match[3].trim() };
}

function todayDate(): string {
  const now = new Date();
  return `${now.getFullYear()}-${`${now.getMonth() + 1}`.padStart(2, '0')}-${`${now.getDate()}`.padStart(2, '0')}`;
}

function isValidPastDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || value >= todayDate()) return false;
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
}
