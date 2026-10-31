import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { Horse } from '../../../models/horse.model';
import { BackendApiService } from '../../../services/backend-api.service';
import { addressError, composeAddress, splitAddress } from '../../../utils/address';
import { IMAGE_FILE_TYPES, imageFileError } from '../../../utils/image';

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
  readonly calculatedAge = computed(() => calculateAge(this.birthDate()));
  street = signal('');
  postalCode = signal('');
  city = signal('');
  /** Die Adresse in der Form "Straße, PLZ Ort", wie sie das Pferd speichert und das Backend erwartet. */
  readonly address = computed(() => composeAddress(this.street(), this.postalCode(), this.city()));
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly imageFileTypes = IMAGE_FILE_TYPES;
  readonly selectedImage = signal<File | null>(null);
  readonly imageError = signal('');
  /** Ein neues Pferd, dessen Bild nicht hochgeladen werden konnte: ein weiterer Versuch darf es nicht doppelt anlegen */
  private createdHorse: Horse | null = null;
  private readonly api = inject(BackendApiService);
  /** Die Rassen aus dem Backend, leer solange sie nicht geladen sind (das Backend prüft beim Speichern) */
  readonly supportedBreeds = toSignal(
    this.api.getBreeds().pipe(
      map(breeds => breeds.map(breed => breed.name)),
      catchError(() => of([] as string[])),
    ),
    { initialValue: [] as string[] },
  );
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.imageError.set(imageFileError(file));
    this.selectedImage.set(this.imageError() ? null : file);
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
      const address = splitAddress(horse.address);
      this.street.set(address.street);
      this.postalCode.set(address.postalCode);
      this.city.set(address.city);
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
    if (this.backendMode() && this.supportedBreeds().length && !isSupportedBreedList(this.breed(), this.supportedBreeds())) {
      e.breed = 'Bitte nur Rassen aus der Liste verwenden.';
    }
    const addressProblem = addressError(
      { street: this.street(), postalCode: this.postalCode(), city: this.city() },
      this.backendMode(),
    );
    if (addressProblem) e.address = addressProblem;
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

    const existing = this.horse() ?? this.createdHorse;
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
      ...(this.address() ? { address: this.address() } : {}),
    };

    if (this.backendMode()) {
      if (this.saving()) return;
      this.saving.set(true);
      this.saveError.set('');
      // an existing horse is changed, a new one is added
      const request = existing ? this.api.updateHorse(horse) : this.api.createHorse(this.ownerId(), horse);
      request.pipe(
        tap(savedHorse => { this.createdHorse = this.horse() ? null : savedHorse; }),
        switchMap(savedHorse => this.saveImage(savedHorse)),
      ).subscribe({
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
      const image = this.selectedImage();
      this.saved.emit(image ? { ...horse, imageUrl: URL.createObjectURL(image) } : horse);
      this.reset();
    }
  }

  private saveImage(horse: Horse): Observable<Horse> {
    const image = this.selectedImage();
    if (!image) return of(horse);
    return this.api.uploadHorseImage(horse.id, image).pipe(map(imageUrl => ({ ...horse, imageUrl })));
  }

  cancel(): void {
    if (this.saving()) return;
    const createdHorse = this.createdHorse;
    this.reset();
    if (createdHorse) this.saved.emit(createdHorse);
    else this.cancelled.emit();
  }

  private reset(): void {
    this.name.set('');
    this.breed.set('');
    this.heightCm.set(null);
    this.weightKg.set(null);
    this.sex.set('Weiblich');
    this.birthDate.set('');
    this.street.set('');
    this.postalCode.set('');
    this.city.set('');
    this.errors.set({});
    this.saveError.set('');
    this.selectedImage.set(null);
    this.imageError.set('');
    this.createdHorse = null;
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

function isSupportedBreedList(value: string, supportedBreeds: string[]): boolean {
  const names = value.split(',').map(name => name.trim().toLocaleLowerCase('de')).filter(Boolean);
  return names.length > 0 && names.every(name =>
    supportedBreeds.some(breed => breed.toLocaleLowerCase('de') === name),
  );
}

