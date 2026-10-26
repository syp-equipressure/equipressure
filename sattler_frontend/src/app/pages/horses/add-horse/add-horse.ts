import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Horse } from '../../../models/horse.model';
import { BackendApiService, DEVELOPMENT_HORSE_BREEDS } from '../../../services/backend-api.service';

@Component({
  selector: 'add-horse',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-horse.html',
  styleUrl: './add-horse.scss',
})
export class AddHorse {
  readonly ownerId = input.required<string>();
  readonly backendMode = input(false);
  readonly horse = input<Horse | null>(null);

  readonly saved = output<Horse>();
  readonly cancelled = output<void>();

  name = signal('');
  breed = signal('');
  heightCm = signal<number | null>(null);
  weightKg = signal<number | null>(null);
  sex = signal<Exclude<Horse['sex'], undefined>>('Weiblich');
  birthDate = signal('');
  readonly today = formatDateInput(new Date());
  readonly supportedBreeds = DEVELOPMENT_HORSE_BREEDS.map(breed => breed.name);
  readonly calculatedAge = computed(() => calculateAge(this.birthDate()));
  address = signal('');
  readonly saving = signal(false);
  readonly saveError = signal('');
  private readonly api = inject(BackendApiService);
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
  }
  errors = signal<Partial<Record<keyof Horse, string>>>({});

  constructor() {
    effect(() => {
      const horse = this.horse();
      if (!horse) return;
      this.name.set(horse.name);
      this.breed.set(horse.breed);
      this.heightCm.set(horse.heightCm);
      this.weightKg.set(horse.weightKg);
      this.sex.set(normalizeSex(horse.sex));
      this.birthDate.set(toDateInputValue(horse.birthDate));
      this.address.set(horse.address ?? '');
    });
  }

  private validate(): boolean {
    const e: Partial<Record<keyof Horse, string>> = {};
    if (!this.name().trim()) e.name = 'Name ist erforderlich.';
    if (!this.birthDate()) {
      e.birthDate = 'Geburtsdatum ist erforderlich.';
    } else if (this.calculatedAge() === null) {
      e.birthDate = 'Bitte ein gültiges Geburtsdatum in der Vergangenheit auswählen.';
    }
    if (!this.breed().trim()) e.breed = 'Rasse ist erforderlich.';
    if (this.backendMode() && !isSupportedBreedList(this.breed())) {
      e.breed = `Bitte nur unterstützte Rassen verwenden: ${this.supportedBreeds.join(', ')}.`;
    }
    if (this.backendMode() && !parseHorseAddress(this.address())) {
      e.address = 'Bitte den Standort als „Straße, PLZ Ort“ angeben.';
    }
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

    const existing = this.horse();
    const horse: Horse = {
      ...existing,
      id: existing?.id ?? crypto.randomUUID(),
      ownerId: this.ownerId(),
      name: this.name().trim(),
      age: this.calculatedAge()!,
      breed: this.breed().trim(),
      heightCm: this.heightCm()!,
      weightKg: this.weightKg()!,
      imageUrl: existing?.imageUrl ?? '',
      sex: this.sex(),
      birthDate: toGermanDate(this.birthDate()),
      ...(this.address().trim() ? { address: this.address().trim() } : {}),
    };

    if (this.backendMode()) {
      if (this.saving()) return;
      this.saving.set(true);
      this.saveError.set('');
      this.api.createHorse(this.ownerId(), horse).subscribe({
        next: savedHorse => {
          this.saving.set(false);
          this.saved.emit(savedHorse);
          this.reset();
        },
        error: error => {
          this.saving.set(false);
          this.saveError.set(error instanceof Error ? error.message : 'Pferd konnte nicht gespeichert werden.');
        },
      });
    } else {
      this.saved.emit(horse);
      this.reset();
    }
  }

  cancel(): void {
    if (this.saving()) return;
    this.reset();
    this.cancelled.emit();
  }

  private reset(): void {
    this.name.set('');
    this.breed.set('');
    this.heightCm.set(null);
    this.weightKg.set(null);
    this.sex.set('Weiblich');
    this.birthDate.set('');
    this.address.set('');
    this.errors.set({});
    this.saveError.set('');
  }

}

function calculateAge(date: string): number | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date);
  if (!match) return null;
  const [, yearText, monthText, dayText] = match;
  const year = Number(yearText);
  const month = Number(monthText);
  const day = Number(dayText);
  const birth = new Date(year, month - 1, day);
  const today = new Date();
  if (
    birth.getFullYear() !== year ||
    birth.getMonth() !== month - 1 ||
    birth.getDate() !== day ||
    birth > today
  ) {
    return null;
  }

  let age = today.getFullYear() - year;
  if (today.getMonth() < month - 1 || (today.getMonth() === month - 1 && today.getDate() < day)) {
    age--;
  }
  return age;
}

function toDateInputValue(date: string | undefined): string {
  if (!date) return '';
  if (/^\d{4}-\d{2}-\d{2}$/.test(date)) return date;
  const germanDate = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(date);
  return germanDate ? `${germanDate[3]}-${germanDate[2]}-${germanDate[1]}` : '';
}

function toGermanDate(date: string): string {
  const [, year, month, day] = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date) ?? [];
  return year && month && day ? `${day}.${month}.${year}` : date;
}

function formatDateInput(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function normalizeSex(sex: string | undefined): Exclude<Horse['sex'], undefined> {
  if (sex === 'Männlich' || sex === 'Hengst' || sex === 'Wallach') return 'Männlich';
  return 'Weiblich';
}

function isSupportedBreedList(value: string): boolean {
  const names = value.split(',').map(name => name.trim().toLocaleLowerCase('de')).filter(Boolean);
  return names.length > 0 && names.every(name =>
    DEVELOPMENT_HORSE_BREEDS.some(breed => breed.name.toLocaleLowerCase('de') === name),
  );
}

function parseHorseAddress(value: string): boolean {
  return /^(.*?),\s*(\d{4,10})\s+(.+)$/.test(value.trim());
}
