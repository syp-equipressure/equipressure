import { Component, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {Customer} from '../../../models/customer.model';

@Component({
  selector: 'app-new-customer-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-customer.html',
  styleUrl: './add-customer.scss',
})
export class AddCustomer {
  readonly saved = output<Customer>();
  readonly cancelled = output<void>();

  firstName = signal('');
  lastName = signal('');
  email = signal('');
  phoneNumber = signal('');
  heightCm = signal<number | null>(null);
  weightKg = signal<number | null>(null);

  errors = signal<Partial<Record<keyof Customer, string>>>({});

  private validate(): boolean {
    const e: Partial<Record<keyof Customer, string>> = {};
    if (!this.firstName().trim()) e.firstName = 'Vorname ist erforderlich.';
    if (!this.lastName().trim()) e.lastName = 'Nachname ist erforderlich.';
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

    const newCustomer: Customer = {
      id: crypto.randomUUID(),
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim(),
      email: this.email().trim() || undefined,
      phoneNumber: this.phoneNumber().trim() || undefined,
      heightCm: this.heightCm()!,
      weightKg: this.weightKg()!,
      horseIds: [],
    };

    this.saved.emit(newCustomer);
    this.reset();
  }

  cancel(): void {
    this.reset();
    this.cancelled.emit();
  }

  private reset(): void {
    this.firstName.set('');
    this.lastName.set('');
    this.email.set('');
    this.phoneNumber.set('');
    this.heightCm.set(null);
    this.weightKg.set(null);
    this.errors.set({});
  }
}
