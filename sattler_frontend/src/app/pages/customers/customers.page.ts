import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { BackendApiService } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import {AddCustomer} from './add-customer/add-customer';

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

  constructor() {
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

  readonly filtered = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('de');
    const dateFrom = dateInputTimestamp(this.measurementDateFrom());
    const dateTo = dateInputTimestamp(this.measurementDateTo());
    const hasDateRange = Boolean(this.measurementDateFrom() || this.measurementDateTo());

    return this.customers()
      .filter(customer => {
        const searchText = [
          fullName(customer),
          customer.email ?? '',
        ].join(' ').toLocaleLowerCase('de');
        const matchesSearch = searchText.includes(query);
        const lastMeasurement = this.lastMeasurementTimestamp(customer.id);
        const matchesDateRange =
          !hasDateRange ||
          (lastMeasurement !== null &&
            (dateFrom === null || lastMeasurement >= dateFrom) &&
            (dateTo === null || lastMeasurement <= dateTo));

        return matchesSearch && matchesDateRange;
      })
      .sort((left, right) => fullName(left).localeCompare(fullName(right), 'de', { sensitivity: 'base' }));
  });

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

  private lastMeasurementTimestamp(customerId: string): number | null {
    const timestamps = this.data.getHorsesOf(customerId)
      .flatMap(horse => this.data.getMeasurementsOf(horse.id))
      .map(measurement => measurementTimestamp(measurement.date))
      .filter((timestamp): timestamp is number => timestamp !== null);

    return timestamps.length > 0 ? Math.max(...timestamps) : null;
  }
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
