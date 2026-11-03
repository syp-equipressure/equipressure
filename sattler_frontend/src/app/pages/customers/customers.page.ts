import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { BackendApiService } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import {AddCustomer} from './add-customer/add-customer';
import { catchError, forkJoin, map, of, switchMap } from 'rxjs';

@Component({
  selector: 'app-customers-page',
  standalone: true,
  imports: [AddCustomer, RouterLink, PageHeaderComponent],
  templateUrl: './customers.page.html',
  styleUrl: './customers.page.scss',
})
export class CustomersPage {
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);

  readonly query = signal('');
  readonly measurementDateFrom = signal('');
  readonly measurementDateTo = signal('');
  readonly backendCustomers = signal<Customer[] | null>(null);
  readonly backendLoading = signal(false);
  readonly backendError = signal('');
  readonly backendPersonId = this.personContext.personId;
  readonly customers = computed(
    () => this.backendCustomers() ?? this.data.getCustomers().filter(customer => !customer.isMe),
  );
  readonly showForm = signal(false);
  /** Tage aller Messungen je Backend-Kunde, für den Filter "Messungen von/bis". */
  private readonly backendMeasurementDates = signal<Map<string, number[]>>(new Map());
  readonly measurementDatesLoading = signal(false);

  constructor() {
    // die Messtage der Pferde jedes Kunden laden (nur die Daten der Messgruppen, keine Aufnahmen)
    effect(onCleanup => {
      const customers = this.backendCustomers();
      if (!customers?.length) {
        this.backendMeasurementDates.set(new Map());
        this.measurementDatesLoading.set(false);
        return;
      }
      this.measurementDatesLoading.set(true);
      const subscription = forkJoin(customers.map(customer =>
        this.api.getHorses(customer.id).pipe(
          switchMap(horses => horses.length
            ? forkJoin(horses.map(horse => this.api.getMeasurementDatesOfHorse(horse.id)))
            : of([] as string[][])),
          map(dates => [customer.id, backendDateTimestamps(dates.flat())] as const),
          catchError(() => of([customer.id, [] as number[]] as const)),
        ),
      )).subscribe(entries => {
        this.backendMeasurementDates.set(new Map(entries));
        this.measurementDatesLoading.set(false);
      });
      onCleanup(() => subscription.unsubscribe());
    });

    effect(onCleanup => {
      const personId = this.backendPersonId();
      this.backendError.set('');
      if (!personId) {
        this.backendCustomers.set(null);
        this.backendLoading.set(false);
        return;
      }

      this.backendLoading.set(true);
      const subscription = this.api.getContacts(personId).subscribe({
        next: customers => {
          this.data.upsertCustomers(customers);
          this.backendCustomers.set(customers);
          this.backendLoading.set(false);
        },
        error: error => {
          this.backendCustomers.set([]);
          this.backendError.set(error instanceof Error ? error.message : 'Kontakte konnten nicht geladen werden.');
          this.backendLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  readonly hasDateRange = computed(() => Boolean(this.measurementDateFrom() || this.measurementDateTo()));

  readonly filtered = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('de');
    const dateFrom = dateInputTimestamp(this.measurementDateFrom());
    const dateTo = dateInputTimestamp(this.measurementDateTo());
    const hasDateRange = this.hasDateRange();

    return this.customers()
      .filter(customer => {
        const searchText = [
          fullName(customer),
          customer.email ?? '',
        ].join(' ').toLocaleLowerCase('de');
        const matchesSearch = searchText.includes(query);
        // im Zeitraum muss mindestens eine Messung liegen (Von und Bis jeweils inklusive)
        const matchesDateRange =
          !hasDateRange ||
          this.measurementTimestamps(customer.id).some(timestamp =>
            (dateFrom === null || timestamp >= dateFrom) &&
            (dateTo === null || timestamp <= dateTo));

        return matchesSearch && matchesDateRange;
      })
      .sort((left, right) => {
        if (this.sortBy() === 'lastMeasurement') {
          // neueste Messung zuerst, Kunden ohne Messung ans Ende
          const leftLast = this.measurementSummary(left.id).last;
          const rightLast = this.measurementSummary(right.id).last;
          if (leftLast !== rightLast) {
            if (leftLast === null) return 1;
            if (rightLast === null) return -1;
            return rightLast - leftLast;
          }
        }
        return fullName(left).localeCompare(fullName(right), 'de', { sensitivity: 'base' });
      });
  });

  readonly sortBy = signal<'name' | 'lastMeasurement'>('name');

  /** Anzahl der Messungen und Tag der letzten Messung eines Kunden. */
  measurementSummary(customerId: string): { count: number; last: number | null } {
    const timestamps = this.measurementTimestamps(customerId);
    return { count: timestamps.length, last: timestamps.length ? Math.max(...timestamps) : null };
  }

  /** z.B. "02.09.2026 · 3 Messungen", während des Ladens aus dem Backend "…". */
  measurementLabel(customerId: string): string {
    if (this.backendCustomers() && this.measurementDatesLoading()) return '…';
    const { count, last } = this.measurementSummary(customerId);
    if (last === null) return 'Noch keine Messung';
    const date = new Date(last).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' });
    return `${date} · ${count} ${count === 1 ? 'Messung' : 'Messungen'}`;
  }

  addCustomer(c: Customer): void {
    if (this.backendPersonId()) {
      this.data.upsertCustomers([c]);
      this.backendCustomers.update(customers => [...(customers ?? []).filter(customer => customer.id !== c.id), c]);
    } else {
      this.data.addCustomer(c);
    }
    this.showForm.set(false);
  }

  fullName = fullName;

  avatarUrl(customer: Customer): string | undefined {
    return this.api.personImageUrl(customer.id, customer.imageUpdatedAt);
  }

  /** Die Tage aller Messungen eines Kunden (über alle seine Pferde). */
  private measurementTimestamps(customerId: string): number[] {
    if (this.backendCustomers()) return this.backendMeasurementDates().get(customerId) ?? [];
    return this.data.getHorsesOf(customerId)
      .flatMap(horse => this.data.getMeasurementsOf(horse.id))
      .map(measurement => measurementTimestamp(measurement.date))
      .filter((timestamp): timestamp is number => timestamp !== null);
  }
}

/** Backend dates (yyyy-mm-dd) as timestamps at midnight, like the date inputs of the filter. */
function backendDateTimestamps(dates: string[]): number[] {
  return dates
    .map(date => /^(\d{4})-(\d{2})-(\d{2})/.exec(date))
    .filter((match): match is RegExpExecArray => match !== null)
    .map(([, year, month, day]) => new Date(Number(year), Number(month) - 1, Number(day)).getTime());
}

function measurementTimestamp(date: string): number | null {
  const [day, month, year] = date.split('.').map(Number);
  if (!day || !month || !year) {
    return null;
  }

  const parsedDate = new Date(year, month - 1, day);
  if (parsedDate.getFullYear() !== year || parsedDate.getMonth() !== month - 1 || parsedDate.getDate() !== day) {
    return null;
  }

  return parsedDate.getTime();
}

function dateInputTimestamp(date: string): number | null {
  if (!date) {
    return null;
  }
  const [year, month, day] = date.split('-').map(Number);
  if (!day || !month || !year) {
    return null;
  }

  const parsedDate = new Date(year, month - 1, day);
  if (parsedDate.getFullYear() !== year || parsedDate.getMonth() !== month - 1 || parsedDate.getDate() !== day) {
    return null;
  }

  return parsedDate.getTime();
}
