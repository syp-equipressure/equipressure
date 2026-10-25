import { Component, Input, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { Measurement } from '../../models/measurement.model';
import { Saddle } from '../../models/saddle.model';
import { MeasurementSessionService } from '../../services/measurement-session.service';
import { BackendApiService, SaddleCategory } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

const LOCAL_SADDLE_CATEGORIES: SaddleCategory[] = [
  'Dressur', 'Springen', 'Vielseitigkeit', 'Gangpferd', 'Sonstige',
].map((name, index) => ({ id: index + 1, name }));

interface OwnerCard {
  owner: Customer;
  horses: Horse[];
  backend: boolean;
}

interface SummaryRow {
  label: string;
  value: string;
  accent?: boolean;
}

interface RecentRow {
  id: string;
  label: string;
  date: string;
  /** Ø Druck in N/cm² */
  score: string;
}

const RECENT_COUNT = 2;

function formatPressure(value: number): string {
  return value.toFixed(2).replace('.', ',');
}

/** yyyy-mm-dd → dd.mm.yyyy */
function toGermanDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}

@Component({
  selector: 'app-stub-page',
  standalone: true,
  imports: [PageHeaderComponent, FormsModule, RouterLink],
  templateUrl: './stub.page.html',
  styleUrl: './stub.page.scss',
})
export class StubPage {
  @Input() title = '';

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly data = inject(DataService);
  private readonly measurementSession = inject(MeasurementSessionService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly routeData = toSignal(this.route.data, { initialValue: {} });

  readonly selectionLoading = signal(false);
  readonly selectionError = signal('');
  readonly saddlesLoading = signal(false);
  readonly saddlesError = signal('');
  private readonly backendOwners = signal<OwnerCard[] | null>(null);

  readonly resolvedTitle = computed(() => {
    const dataTitle = (this.routeData() as { title?: string }).title ?? '';
    return this.title || dataTitle;
  });

  readonly isNewMeasurement = computed(() => this.resolvedTitle() === 'Neue Messung');

  readonly owners = computed<OwnerCard[]>(() => {
    const backendOwners = this.backendOwners();
    if (backendOwners !== null) return backendOwners;
    const cards: OwnerCard[] = [];

    for (const customer of this.data.getCustomers()) {
      const horses = this.data.getHorsesOf(customer.id);
      if (horses.length > 0) {
        cards.push({ owner: customer, horses, backend: false });
      }
    }

    return cards;
  });

  readonly selectedOwnerId = signal<string | null>(null);
  readonly selectedHorseId = signal<string | null>(null);

  readonly selectedOwner = computed(() => {
    const list = this.owners();
    const selectedId = this.selectedOwnerId();
    return list.find(entry => entry.owner.id === selectedId) ?? null;
  });

  readonly availableHorses = computed(() => this.selectedOwner()?.horses ?? []);

  readonly selectedHorse = computed(() => {
    const horses = this.availableHorses();
    const selectedId = this.selectedHorseId();
    return horses.find(horse => horse.id === selectedId) ?? null;
  });

  readonly availableSaddles = computed(() => {
    const horse = this.selectedHorse();
    return horse ? this.data.getSaddlesOfHorse(horse.id) : [];
  });

  readonly selectedSaddle = computed<Saddle | null>(() => {
    const selectedId = this.selectedSaddleId();
    return this.availableSaddles().find(saddle => saddle.id === selectedId) ?? null;
  });

  readonly riderName = computed(() => {
    const owner = this.selectedOwner();
    return owner ? fullName(owner.owner) : '';
  });

  readonly riderEmail = computed(() => this.selectedOwner()?.owner.email ?? '');
  readonly riderHeight = computed(() => this.selectedOwner()?.owner.heightCm ?? 0);
  readonly riderWeight = computed(() => this.selectedOwner()?.owner.weightKg ?? 0);

  readonly selectedHorseMeasurements = computed((): Measurement[] => {
    const horse = this.selectedHorse();
    return horse ? this.data.getMeasurementsOf(horse.id) : [];
  });

  readonly horseHeight = computed(() => this.selectedHorse()?.heightCm ?? 0);
  readonly horseWeight = computed(() => this.selectedHorse()?.weightKg ?? 0);

  readonly selectedSaddleId = signal<string | null>(null);
  readonly saddleFormOpen = signal(false);
  readonly saddleName = signal('');
  readonly saddleCategory = signal('Dressur');
  readonly saddleError = signal('');
  readonly saddleSaving = signal(false);
  private readonly backendSaddleCategories = signal<SaddleCategory[]>([]);
  /** Kategorien für das Formular: aus dem Backend für Backend-Pferde, sonst die lokalen. */
  readonly saddleCategories = computed(() =>
    this.selectedOwner()?.backend && this.backendSaddleCategories().length
      ? this.backendSaddleCategories()
      : LOCAL_SADDLE_CATEGORIES,
  );
  readonly selectedSaddleName = computed(() => this.selectedSaddle()?.name ?? '');
  readonly selectedSaddleCategory = computed(() => this.selectedSaddle()?.category ?? '');

  private readonly loadBackendOwners = effect(onCleanup => {
    if (!this.isNewMeasurement()) {
      this.backendOwners.set(null);
      this.selectionLoading.set(false);
      return;
    }
    const personId = this.personContext.personId();
    this.selectionError.set('');
    if (!personId) {
      this.backendOwners.set(null);
      this.selectionLoading.set(false);
      return;
    }
    this.selectionLoading.set(true);
    // der Sattler selbst kann auch für seine eigenen Pferde messen, er steht als erste Option in der Liste
    const self$ = forkJoin({
      person: this.api.getPerson(personId),
      horses: this.api.getHorses(personId).pipe(catchError(() => of([] as Horse[]))),
    }).pipe(map(({ person, horses }): OwnerCard => ({
      owner: { ...person, isMe: true },
      horses,
      backend: true,
    })));
    const contacts$ = this.api.getContacts(personId).pipe(
      switchMap(contacts => contacts.length
        ? forkJoin(contacts.map(contact =>
            forkJoin({
              // die Kontaktliste enthält keine Größe und kein Gewicht, die kommen aus den Personendaten
              person: this.api.getPerson(contact.id).pipe(catchError(() => of(contact))),
              horses: this.api.getHorses(contact.id),
            }).pipe(map(({ person, horses }): OwnerCard => ({
              owner: { ...contact, ...person, horseIds: contact.horseIds },
              horses,
              backend: true,
            }))),
          ))
        : of([] as OwnerCard[]),
      ),
    );
    const subscription = forkJoin([self$, contacts$]).pipe(
      map(([self, contacts]) => [self, ...contacts.filter(card => card.owner.id !== self.owner.id)]),
    ).subscribe({
      next: owners => {
        this.data.upsertCustomers(owners.map(item => item.owner));
        this.data.upsertHorses(owners.flatMap(item => item.horses));
        this.backendOwners.set(owners);
        this.selectionLoading.set(false);
        this.initializeSelection();
      },
      error: error => {
        this.backendOwners.set([]);
        this.selectionError.set(error instanceof Error ? error.message : 'Kontakte und Pferde konnten nicht geladen werden.');
        this.selectionLoading.set(false);
      },
    });
    onCleanup(() => subscription.unsubscribe());
  });

  private readonly loadBackendSaddles = effect(onCleanup => {
    const horse = this.selectedHorse();
    const owner = this.selectedOwner();
    this.saddlesError.set('');
    if (!horse || !owner?.backend) {
      this.saddlesLoading.set(false);
      return;
    }
    this.saddlesLoading.set(true);
    const subscription = this.api.getSaddles(horse.id).subscribe({
      next: saddles => {
        this.data.upsertSaddles(saddles);
        this.saddlesLoading.set(false);
        const requestedId = this.route.snapshot.queryParamMap.get('saddleId');
        const requestedSaddle = saddles.find(saddle => saddle.id === requestedId);
        if (requestedSaddle && this.selectedHorse()?.id === horse.id) {
          this.selectedSaddleId.set(requestedSaddle.id);
        }
      },
      error: error => {
        this.saddlesError.set(error instanceof Error ? error.message : 'Sättel konnten nicht geladen werden.');
        this.saddlesLoading.set(false);
      },
    });
    onCleanup(() => subscription.unsubscribe());
  });

  readonly summaryRows = computed<SummaryRow[]>(() => {
    const owner = this.selectedOwner();
    const horse = this.selectedHorse();
    const count = this.backendHorseMeasurements()?.count ?? this.selectedHorseMeasurements().length;

    return [
      { label: 'Reiter:in', value: owner ? fullName(owner.owner) : '' },
      { label: 'Pferd', value: horse?.name ?? '' },
      { label: 'Sattel', value: this.selectedSaddleName() },
      {
        label: 'Messungen',
        value: horse ? `${count} vorhanden` : '',
        accent: true,
      },
    ];
  });

  /** Anzahl und die zwei neuesten Messungen des gewählten Backend-Pferdes. */
  private readonly backendHorseMeasurements = signal<{ count: number; recent: RecentRow[] } | null>(null);

  readonly recentMeasurements = computed<RecentRow[]>(() => {
    const horse = this.selectedHorse();
    if (!horse) {
      return [];
    }
    const backend = this.backendHorseMeasurements();
    if (backend) return backend.recent;

    return this.selectedHorseMeasurements().slice(0, RECENT_COUNT).map(measurement => ({
      id: measurement.id,
      label: `${horse.name} · ${measurement.deviceName}`,
      date: measurement.date,
      score: formatPressure(measurement.detail.meanNcm2),
    }));
  });

  private readonly loadBackendHorseMeasurements = effect(onCleanup => {
    const horse = this.selectedHorse();
    this.backendHorseMeasurements.set(null);
    if (!horse || !this.selectedOwner()?.backend) return;

    const subscription = this.api.getMeasurementSummariesOfHorse(horse.id).pipe(
      switchMap(summaries => {
        const latest = summaries.slice(0, RECENT_COUNT);
        const rows$ = latest.length
          ? forkJoin(latest.map(summary => this.api.getMeasurementGroupOverview(summary.id).pipe(
              catchError(() => of({ name: '', meanNcm2: null })),
              map((overview): RecentRow => ({
                id: summary.id,
                label: `${horse.name} · ${overview.name || summary.saddleName}`,
                date: toGermanDate(summary.date),
                score: overview.meanNcm2 === null ? '–' : formatPressure(overview.meanNcm2),
              })),
            )))
          : of([] as RecentRow[]);
        return rows$.pipe(map(recent => ({ count: summaries.length, recent })));
      }),
      catchError(() => of(null)),
    ).subscribe(result => this.backendHorseMeasurements.set(result));
    onCleanup(() => subscription.unsubscribe());
  });

  constructor() {
    if (this.route.snapshot.data['title'] === 'Neue Messung') {
      this.measurementSession.clear();
    }

    const params = this.route.snapshot.queryParamMap;
    const horseId = params.get('horseId');
    const requestedHorse = horseId ? this.data.getHorse(horseId) : undefined;
    const ownerId = requestedHorse?.ownerId ?? params.get('ownerId');
    const owner = this.owners().find(entry => entry.owner.id === ownerId);

    this.selectedOwnerId.set(owner?.owner.id ?? null);
    const selectedHorseId = owner?.horses.find(horse => horse.id === requestedHorse?.id)?.id ?? null;
    this.selectedHorseId.set(selectedHorseId);
    const saddleId = params.get('saddleId');
    this.selectedSaddleId.set(
      selectedHorseId
        ? this.data.getSaddlesOfHorse(selectedHorseId).find(saddle => saddle.id === saddleId)?.id ?? null
        : null,
    );
  }

  private initializeSelection(): void {
    const params = this.route.snapshot.queryParamMap;
    const requestedHorseId = params.get('horseId');
    const requestedHorse = requestedHorseId
      ? this.owners().flatMap(entry => entry.horses).find(horse => horse.id === requestedHorseId)
      : undefined;
    const ownerId = requestedHorse?.ownerId ?? params.get('ownerId');
    const owner = this.owners().find(entry => entry.owner.id === ownerId);
    this.selectedOwnerId.set(owner?.owner.id ?? null);
    this.selectedHorseId.set(
      owner?.horses.find(horse => horse.id === requestedHorse?.id)?.id ?? null,
    );
    this.selectedSaddleId.set(null);
  }

  selectOwner(ownerId: string) {
    this.selectedOwnerId.set(ownerId || null);
    this.selectedHorseId.set(null);
    this.selectedSaddleId.set(null);
  }

  selectHorse(horseId: string) {
    this.selectedHorseId.set(horseId || null);
    this.selectedSaddleId.set(null);
  }

  selectSaddle(saddleId: string) {
    this.selectedSaddleId.set(saddleId || null);
  }

  openSaddleForm(): void {
    this.saddleName.set('');
    this.saddleCategory.set(this.saddleCategories()[0]?.name ?? 'Dressur');
    this.saddleError.set('');
    this.saddleFormOpen.set(true);
    if (this.selectedOwner()?.backend) this.loadSaddleCategories();
  }

  /** Lädt die Sattelkategorien aus dem Backend, sonst bleiben die lokalen Kategorien. */
  private loadSaddleCategories(): void {
    if (this.backendSaddleCategories().length) return;
    this.api.getSaddleCategories().subscribe({
      next: categories => {
        this.backendSaddleCategories.set(categories);
        if (!categories.some(category => category.name === this.saddleCategory())) {
          this.saddleCategory.set(categories[0]?.name ?? '');
        }
      },
      error: error => this.saddleError.set(
        error instanceof Error ? error.message : 'Sattelkategorien konnten nicht geladen werden.',
      ),
    });
  }

  cancelSaddleForm(): void {
    this.saddleFormOpen.set(false);
    this.saddleError.set('');
  }

  saveSaddle(): void {
    const horse = this.selectedHorse();
    const name = this.saddleName().trim();
    if (!horse || !name) {
      this.saddleError.set('Bitte einen Sattelnamen eingeben.');
      return;
    }

    if (this.selectedOwner()?.backend) {
      const category = this.backendSaddleCategories().find(item => item.name === this.saddleCategory());
      if (!category) {
        this.saddleError.set('Bitte eine Kategorie auswählen.');
        return;
      }
      this.saddleSaving.set(true);
      this.api.createSaddle(horse.id, name, category.id).subscribe({
        next: saddle => {
          this.data.upsertSaddles([saddle]);
          this.selectedSaddleId.set(saddle.id);
          this.saddleSaving.set(false);
          this.saddleFormOpen.set(false);
        },
        error: error => {
          this.saddleSaving.set(false);
          this.saddleError.set(error instanceof Error ? error.message : 'Der Sattel konnte nicht gespeichert werden.');
        },
      });
      return;
    }

    const saddle: Saddle = {
      id: crypto.randomUUID(),
      horseId: horse.id,
      name,
      category: this.saddleCategory(),
    };
    this.data.addSaddle(saddle);
    this.selectedSaddleId.set(saddle.id);
    this.saddleFormOpen.set(false);
  }

  continueToMeasurement() {
    const owner = this.selectedOwner();
    const horse = this.selectedHorse();
    const saddle = this.selectedSaddle();
    if (!owner || !horse || !saddle) {
      return;
    }

    if (!this.measurementSession.start(owner.owner.id, horse.id, saddle.id)) {
      return;
    }

    void this.router.navigate(['/new-measurement/session'], {
      queryParams: {
        ownerId: owner.owner.id,
        horseId: horse.id,
        saddleId: saddle.id,
      },
    });
  }

  readonly fullName = fullName;
}
