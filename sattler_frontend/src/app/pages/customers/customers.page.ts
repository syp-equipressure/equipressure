import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService, BackendPerson } from '../../services/person-api.service';

@Component({
  selector: 'app-customers-page',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  templateUrl: './customers.page.html',
  styleUrl: './customers.page.scss',
})
export class CustomersPage {
  private readonly data = inject(DataService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);

  readonly query = signal('');
  readonly activeList = signal<'contacts' | 'favourites'>('contacts');
  readonly backendCustomers = signal<Customer[] | null>(null);
  readonly loadingBackendCustomers = signal(false);
  readonly backendCustomersError = signal('');
  readonly demoCustomers = signal<Customer[]>(this.data.getCustomers().filter(customer => !customer.isMe));

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
          // TODO Backend-Bug: contacts/favourites currently include the requested person.
          const customers = people.filter(person => person.id !== personId).map(toCustomer);
          this.data.upsertCustomers(customers);
          this.backendCustomers.set(customers);
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

    return this.customers()
      .filter(customer => {
        const searchText = [
          fullName(customer),
          customer.email ?? '',
        ].join(' ').toLocaleLowerCase('de');
        const matchesSearch = searchText.includes(query);
        return matchesSearch;
      })
      .sort((left, right) => fullName(left).localeCompare(fullName(right), 'de', { sensitivity: 'base' }));
  });

  fullName = fullName;
}

function toCustomer(person: BackendPerson): Customer {
  return {
    ...person,
    isMe: false,
  };
}
