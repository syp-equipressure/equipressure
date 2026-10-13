import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { PersonApiService } from '../../services/person-api.service';

@Component({
  selector: 'app-customer-profile-page',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink],
  templateUrl: './customer-profile.page.html',
  styleUrl: './customer-profile.page.scss',
})
export class CustomerProfilePage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = inject(DataService);
  private readonly personApi = inject(PersonApiService);
  private readonly params = toSignal(this.route.paramMap, { requireSync: true });
  private readonly backendCustomer = signal<Customer | null>(null);
  readonly loadingBackendCustomer = signal(false);
  readonly backendCustomerError = signal('');

  readonly customerId = computed(() => this.params().get('customerId') ?? '');
  readonly customer = computed(() => {
    const id = this.customerId();
    return /^[1-9]\d*$/.test(id)
      ? this.backendCustomer() ?? undefined
      : this.data.getCustomer(id);
  });
  readonly title = computed(() => {
    const customer = this.customer();
    return customer ? `${fullName(customer)} – Kundenprofil` : 'Kundenprofil';
  });
  readonly fullName = fullName;

  constructor() {
    effect(onCleanup => {
      const personId = this.customerId();
      this.backendCustomerError.set('');
      if (!/^[1-9]\d*$/.test(personId)) {
        this.backendCustomer.set(null);
        this.loadingBackendCustomer.set(false);
        return;
      }

      this.loadingBackendCustomer.set(true);
      const subscription = forkJoin({
        details: this.personApi.getPersonDetails(personId),
        profile: this.personApi.getProfileData(personId),
        location: this.personApi.getLocation(personId),
      }).subscribe({
        next: ({ details, profile, location }) => {
          const address = [
            location.addressName,
            `${location.postalCode} ${location.cityName}`,
          ]
            .filter(Boolean)
            .join(', ');
          const customer: Customer = {
            ...details,
            firstName: profile.firstName,
            lastName: profile.lastName,
            address,
            isMe: false,
          };
          this.backendCustomer.set(customer);
          this.data.upsertCustomers([customer]);
          this.loadingBackendCustomer.set(false);
        },
        error: error => {
          this.backendCustomerError.set(
            error instanceof Error
              ? error.message
              : 'Das Kundenprofil konnte nicht geladen werden.',
          );
          this.loadingBackendCustomer.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  formatBirthDate(birthDate: string | undefined): string {
    if (!birthDate) return '–';
    const [year, month, day] = birthDate.split('-');
    return year && month && day ? `${day}.${month}.${year}` : birthDate;
  }
}
