import { HttpErrorResponse } from '@angular/common/http';
import { Component, Input, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subscription, forkJoin } from 'rxjs';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { Measurement } from '../../models/measurement.model';
import { Saddle } from '../../models/saddle.model';
import { MeasurementSessionService } from '../../services/measurement-session.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService, BackendPerson } from '../../services/person-api.service';
import { MeasurementService } from '../../services/measurement.service';

interface OwnerCard {
  owner: Customer;
  horses: Horse[];
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
  score: string;
  tone: 'green' | 'red';
}

@Component({
  selector: 'app-stub-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './stub.page.html',
  styleUrl: './stub.page.scss',
})
export class StubPage {
  @Input() title = '';

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly data = inject(DataService);
  private readonly measurementSession = inject(MeasurementSessionService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);
  private readonly measurementService = inject(MeasurementService);
  private readonly routeData = toSignal(this.route.data, { initialValue: {} });

  readonly resolvedTitle = computed(() => {
    const dataTitle = (this.routeData() as { title?: string }).title ?? '';
    return this.title || dataTitle;
  });

  readonly isNewMeasurement = computed(() => this.resolvedTitle() === 'Neue Messung');
  readonly owners = signal<OwnerCard[]>([]);
  readonly selectionLoading = signal(false);
  readonly selectionError = signal('');
  readonly saddlesLoading = signal(false);
  readonly saddlesError = signal('');
  readonly availableSaddles = signal<Saddle[]>([]);
  readonly selectedOwnerId = signal<string | null>(null);
  readonly selectedHorseId = signal<string | null>(null);
  readonly selectedSaddleId = signal<string | null>(null);

  readonly selectedOwner = computed(() => {
    const selectedId = this.selectedOwnerId();
    return this.owners().find(entry => entry.owner.id === selectedId) ?? null;
  });

  readonly availableHorses = computed(() => this.selectedOwner()?.horses ?? []);

  readonly selectedHorse = computed(() => {
    const selectedId = this.selectedHorseId();
    return this.availableHorses().find(horse => horse.id === selectedId) ?? null;
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
    return horse ? this.measurementService.getMeasurementsForHorse(horse.id) : [];
  });

  readonly horseHeight = computed(() => this.selectedHorse()?.heightCm ?? 0);
  readonly horseWeight = computed(() => this.selectedHorse()?.weightKg ?? 0);
  readonly selectedSaddleName = computed(() => this.selectedSaddle()?.name ?? '');
  readonly selectedSaddleCategory = computed(() => this.selectedSaddle()?.category ?? '');

  readonly summaryRows = computed<SummaryRow[]>(() => {
    const owner = this.selectedOwner();
    const horse = this.selectedHorse();
    return [
      { label: 'Reiter:in', value: owner ? fullName(owner.owner) : '' },
      { label: 'Pferd', value: horse?.name ?? '' },
      { label: 'Sattel', value: this.selectedSaddleName() },
      {
        label: 'Messungen',
        value: horse ? `${this.selectedHorseMeasurements().length} vorhanden` : '',
        accent: true,
      },
    ];
  });

  readonly recentMeasurements = computed<RecentRow[]>(() =>
    this.selectedHorseMeasurements().slice(0, 2).map((measurement, index) => ({
      id: measurement.id,
      label: `${this.selectedHorse()?.name ?? ''} · ${measurement.deviceName}`,
      date: measurement.date,
      score: index === 0 ? '92' : '51',
      tone: index === 0 ? 'green' : 'red',
    })),
  );

  constructor() {
    effect(onCleanup => {
      if (!this.isNewMeasurement()) return;
      this.measurementSession.clear();
      this.selectionError.set('');
      this.owners.set([]);
      this.selectedOwnerId.set(null);
      this.selectedHorseId.set(null);
      this.selectedSaddleId.set(null);
      const personId = this.personContext.personId();
      if (!personId) {
        this.selectionLoading.set(false);
        return;
      }

      this.selectionLoading.set(true);
      const subscriptions = new Subscription();
      subscriptions.add(this.personApi.getContacts(personId).subscribe({
        next: contacts => {
          // TODO Backend-Bug: contacts currently includes the requested person, not just riders.
          const riderIds = contacts
            .filter(contact => contact.id !== personId)
            .map(contact => contact.id);
          if (riderIds.length === 0) {
            this.selectionLoading.set(false);
            return;
          }

          subscriptions.add(forkJoin(
            riderIds.map(id =>
              forkJoin({
                details: this.personApi.getPersonDetails(id),
                horses: this.personApi.getHorses(id),
              }),
            ),
          ).subscribe({
            next: riders => {
              const ownerCards = riders.map(({ details, horses }) => ({
                owner: toCustomer(details),
                horses,
              }));
              this.owners.set(ownerCards);
              this.data.upsertCustomers(ownerCards.map(entry => entry.owner));
              this.data.upsertHorses(ownerCards.flatMap(entry => entry.horses));

              const requestedOwner = this.route.snapshot.queryParamMap.get('ownerId');
              const selectedOwner = ownerCards.find(entry => entry.owner.id === requestedOwner);
              this.selectedOwnerId.set(selectedOwner?.owner.id ?? null);
              const requestedHorse = this.route.snapshot.queryParamMap.get('horseId');
              this.selectedHorseId.set(
                selectedOwner?.horses.find(horse => horse.id === requestedHorse)?.id ?? null,
              );
              this.selectionLoading.set(false);
            },
            error: error => {
              this.selectionError.set(
                error instanceof Error
                  ? error.message
                  : 'Die Kundendaten konnten nicht geladen werden.',
              );
              this.selectionLoading.set(false);
            },
          }));
        },
        error: error => {
          this.selectionError.set(
            error instanceof Error ? error.message : 'Die Kontakte konnten nicht geladen werden.',
          );
          this.selectionLoading.set(false);
        },
      }));
      onCleanup(() => subscriptions.unsubscribe());
    });

    effect(onCleanup => {
      const horse = this.selectedHorse();
      this.availableSaddles.set([]);
      this.selectedSaddleId.set(null);
      this.saddlesError.set('');
      if (!this.isNewMeasurement() || !horse) {
        this.saddlesLoading.set(false);
        return;
      }

      this.saddlesLoading.set(true);
      const subscription = this.personApi.getSaddles(horse.id).subscribe({
        next: saddles => {
          this.availableSaddles.set(saddles);
          this.data.upsertSaddles(saddles);
          const requestedSaddle = this.route.snapshot.queryParamMap.get('saddleId');
          this.selectedSaddleId.set(
            saddles.find(saddle => saddle.id === requestedSaddle)?.id ?? null,
          );
          this.saddlesLoading.set(false);
        },
        error: error => {
          if (error instanceof HttpErrorResponse && error.status === 404) {
            this.saddlesLoading.set(false);
            return;
          }
          this.saddlesError.set(
            error instanceof Error ? error.message : 'Die Sättel konnten nicht geladen werden.',
          );
          this.saddlesLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  selectOwner(ownerId: string): void {
    this.selectedOwnerId.set(ownerId || null);
    this.selectedHorseId.set(null);
    this.selectedSaddleId.set(null);
  }

  selectHorse(horseId: string): void {
    this.selectedHorseId.set(horseId || null);
    this.selectedSaddleId.set(null);
  }

  selectSaddle(saddleId: string): void {
    this.selectedSaddleId.set(saddleId || null);
  }

  continueToMeasurement(): void {
    const owner = this.selectedOwner();
    const horse = this.selectedHorse();
    const saddle = this.selectedSaddle();
    if (!owner || !horse || !saddle) return;

    this.router.navigate(['/new-measurement/session'], {
      queryParams: {
        ownerId: owner.owner.id,
        horseId: horse.id,
        saddleId: saddle.id,
      },
    });
  }

  readonly fullName = fullName;
}

function toCustomer(person: BackendPerson): Customer {
  return { ...person, isMe: false };
}
