import { Component, Input, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { Measurement } from '../../models/measurement.model';
import { Saddle } from '../../models/saddle.model';

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
  private readonly routeData = toSignal(this.route.data, { initialValue: {} });

  readonly resolvedTitle = computed(() => {
    const dataTitle = (this.routeData() as { title?: string }).title ?? '';
    return this.title || dataTitle;
  });

  readonly isNewMeasurement = computed(() => this.resolvedTitle() === 'Neue Messung');

  readonly owners = computed<OwnerCard[]>(() => {
    const cards: OwnerCard[] = [];

    for (const customer of this.data.getCustomers()) {
      const horses = this.data.getHorsesOf(customer.id);
      if (horses.length > 0) {
        cards.push({ owner: customer, horses });
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

  readonly recentMeasurements = computed<RecentRow[]>(() => {
    const horse = this.selectedHorse();
    if (!horse) {
      return [];
    }

    return this.selectedHorseMeasurements().slice(0, 2).map((measurement, index) => ({
      id: measurement.id,
      label: `${horse.name} · ${measurement.deviceName}`,
      date: measurement.date,
      score: index === 0 ? '92' : '51',
      tone: index === 0 ? 'green' : 'red',
    }));
  });

  constructor() {
    const params = this.route.snapshot.queryParamMap;
    const horseId = params.get('horseId');
    const requestedHorse = horseId ? this.data.getHorse(horseId) : undefined;
    const ownerId = requestedHorse?.ownerId ?? params.get('ownerId');
    const owner = this.owners().find(entry => entry.owner.id === ownerId);

    this.selectedOwnerId.set(owner?.owner.id ?? null);
    this.selectedHorseId.set(
      owner?.horses.find(horse => horse.id === requestedHorse?.id)?.id ?? null,
    );
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

  continueToMeasurement() {
    const owner = this.selectedOwner();
    const horse = this.selectedHorse();
    if (!owner || !horse || !this.selectedSaddle()) {
      return;
    }

    this.router.navigate(['/customers', owner.owner.id, 'horses', horse.id]);
  }

  readonly fullName = fullName;
}
