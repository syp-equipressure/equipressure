import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { BackendApiService } from '../../services/backend-api.service';

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
  private readonly api = inject(BackendApiService);
  private readonly params = toSignal(this.route.paramMap, { requireSync: true });
  private readonly backendCustomer = signal<Customer | null>(null);
  readonly loadingBackendCustomer = signal(false);
  readonly backendCustomerError = signal('');

  readonly customerId = computed(() => this.params().get('customerId') ?? '');
  readonly customer = computed(
    () => this.backendCustomer() ?? this.data.getCustomer(this.customerId()),
  );
  readonly title = computed(() => {
    const customer = this.customer();
    return customer ? `${fullName(customer)} – Kundenprofil` : 'Kundenprofil';
  });
  readonly fullName = fullName;

  constructor() {
    effect(onCleanup => {
      const id = this.customerId();
      this.backendCustomerError.set('');
      if (!/^[1-9]\d*$/.test(id)) {
        this.backendCustomer.set(null);
        this.loadingBackendCustomer.set(false);
        return;
      }
      this.loadingBackendCustomer.set(true);
      const subscription = this.api.getPerson(id).subscribe({
        next: customer => {
          this.backendCustomer.set(customer);
          this.data.upsertCustomers([customer]);
          this.loadingBackendCustomer.set(false);
        },
        error: error => {
          this.backendCustomerError.set(
            error instanceof Error ? error.message : 'Das Kundenprofil konnte nicht geladen werden.',
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
