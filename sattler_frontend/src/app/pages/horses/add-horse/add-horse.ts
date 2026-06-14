import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Horse } from '../../../models/horse.model';

@Component({
  selector: 'add-horse',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-horse.html',
  styleUrl: './add-horse.scss',
})
export class AddHorse {
  readonly ownerId = input.required<string>();

  readonly saved = output<Horse>();
  readonly cancelled = output<void>();

  name = signal('');
  age = signal<number | null>(null);
  breed = signal('');
  heightCm = signal<number | null>(null);
  weightKg = signal<number | null>(null);
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
  }
  errors = signal<Partial<Record<keyof Horse, string>>>({});

  private validate(): boolean {
    const e: Partial<Record<keyof Horse, string>> = {};
    if (!this.name().trim()) e.name = 'Name ist erforderlich.';
    if (this.age() === null || this.age()! <= 0) e.age = 'Alter ist erforderlich.';
    if (!this.breed().trim()) e.breed = 'Rasse ist erforderlich.';
    if (this.heightCm() === null || this.heightCm()! <= 0) e.heightCm = 'Stockmaß ist erforderlich.';
    if (this.weightKg() === null || this.weightKg()! <= 0) e.weightKg = 'Gewicht ist erforderlich.';
    this.errors.set(e);
    return Object.keys(e).length === 0;
  }

  setNumber(setter: (v: number | null) => void, value: string): void {
    const n = parseFloat(value);
    setter(isNaN(n) ? null : n);
  }

  submit(): void {
    if (!this.validate()) return;

    const horse = {
      id: crypto.randomUUID(),
      ownerId: this.ownerId(),
      name: this.name().trim(),
      age: this.age()!,
      breed: this.breed().trim(),
      heightCm: this.heightCm()!,
      weightKg: this.weightKg()!,
      imageUrl: ""
    };

    console.log('submit called');
    console.log('valid:', this.validate());
    console.log('age:', this.age(), typeof this.age());
    console.log('heightCm:', this.heightCm(), typeof this.heightCm());
    console.log('weightKg:', this.weightKg(), typeof this.weightKg());

    this.saved.emit(horse);
    this.reset();
  }

  cancel(): void {
    this.reset();
    this.cancelled.emit();
  }

  private reset(): void {
    this.name.set('');
    this.age.set(null);
    this.breed.set('');
    this.heightCm.set(null);
    this.weightKg.set(null);
    this.errors.set({});
  }
}
