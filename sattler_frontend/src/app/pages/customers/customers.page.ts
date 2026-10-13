import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import { AddCustomer } from './add-customer/add-customer';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService, BackendPerson } from '../../services/person-api.service';

@Component({
  selector: 'app-customers-page',
  standalone: true,
  imports: [AddCustomer, RouterLink, PageHeaderComponent],
  templateUrl: './customers.page.html',
  styleUrl: './customers.page.scss',
})
export class CustomersPage {
  private readonly data = inject(DataService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);

  readonly query = signal('');
  readonly measurementDateFrom = signal('');
  readonly measurementDateTo = signal('');
  readonly activeList = signal<'contacts' | 'favourites'>('contacts');
  readonly backendCustomers = signal<Customer[] | null>(null);
  readonly loadingBackendCustomers = signal(false);
  readonly backendCustomersError = signal('');
  readonly demoCustomers = signal<Customer[]>(this.data.getCustomers().filter(customer => !customer.isMe));
  readonly showForm = signal(false);

  readonly backendPersonId = this.personContext.personId;
  readonly customers = computed(() => this.backendCustomers() ?? this.demoCustomers());

  constructor() {
    effect(onCleanup => {
      const personId = this.personContext.personId();
      const list = this.activeList();
      this.backendCustomersError.set('');

      if (!personId) {
        this.backendCustomers.set(null);
        this.loadingBackendCustomers.set(false);
        return;
      }

      this.loadingBackendCustomers.set(true);
      const request =
        list === 'contacts'
          ? this.personApi.getContacts(personId)
          : this.personApi.getFavourites(personId);
      const subscription = request.subscribe({
        next: people => {
          this.backendCustomers.set(people.map(toCustomer));
          this.loadingBackendCustomers.set(false);
        },
        error: error => {
          this.backendCustomers.set([]);
          this.backendCustomersError.set(
            error instanceof Error
              ? error.message
              : 'Die Personen konnten nicht geladen werden.',
          );
          this.loadingBackendCustomers.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  readonly filtered = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('de');
    const dateFrom = dateInputTimestamp(this.measurementDateFrom());
    const dateTo = dateInputTimestamp(this.measurementDateTo());
    const hasDateRange =
      !this.backendPersonId() &&
      Boolean(this.measurementDateFrom() || this.measurementDateTo());

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
    if (this.backendPersonId()) return;
    this.data.addCustomer(c);
    this.demoCustomers.update(list => [...list, c]);
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

function toCustomer(person: BackendPerson): Customer {
  return {
    ...person,
    isMe: false,
  };
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
