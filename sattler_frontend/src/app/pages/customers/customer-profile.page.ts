import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { BackendApiService } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { ConfirmService } from '../../services/confirm.service';
import { AddCustomer } from './add-customer/add-customer';

@Component({
  selector: 'app-customer-profile-page',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink, AddCustomer],
  templateUrl: './customer-profile.page.html',
  styleUrl: './customer-profile.page.scss',
})
export class CustomerProfilePage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly confirmDialog = inject(ConfirmService);
  private readonly router = inject(Router);
  private readonly params = toSignal(this.route.paramMap, { requireSync: true });
  private readonly backendCustomer = signal<Customer | null>(null);
  readonly loadingBackendCustomer = signal(false);
  readonly backendCustomerError = signal('');
  readonly showEdit = signal(false);
  readonly actionError = signal('');

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

  customerSaved(customer: Customer): void {
    this.backendCustomer.set(customer);
    this.data.upsertCustomers([customer]);
    this.showEdit.set(false);
  }

  /** Customers the saddler added themselves are gone completely, nobody else can manage them. */
  async deleteCustomer(): Promise<void> {
    const customer = this.customer();
    if (!customer) return;
    const confirmed = await this.confirmDialog.ask({
      title: 'Kund*in löschen',
      message: `${fullName(customer)} wird mit allen Pferden und Messungen endgültig gelöscht. Das kann nicht rückgängig gemacht werden.`,
      confirmLabel: 'Endgültig löschen',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.api.deletePerson(customer.id).subscribe({
      next: () => {
        this.data.removeCustomer(customer.id);
        void this.router.navigate(['/customers']);
      },
      error: (error: Error) => this.actionError.set(error.message),
    });
  }

  /** Customers with an account keep all their data, the saddler only drops the contact. */
  async removeContact(): Promise<void> {
    const customer = this.customer();
    if (!customer) return;
    const confirmed = await this.confirmDialog.ask({
      title: 'Aus Kontakten entfernen',
      message: `${fullName(customer)} verschwindet aus deiner Kundschaft und du siehst die Pferde nicht mehr. Der Chat und die Messungen bleiben erhalten.`,
      confirmLabel: 'Entfernen',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.api.removeContact(this.personContext.personId(), customer.id).subscribe({
      next: () => {
        this.data.removeCustomer(customer.id);
        void this.router.navigate(['/customers']);
      },
      error: (error: Error) => this.actionError.set(error.message),
    });
  }

  formatBirthDate(birthDate: string | undefined): string {
    if (!birthDate) return '–';
    const [year, month, day] = birthDate.split('-');
    return year && month && day ? `${day}.${month}.${year}` : birthDate;
  }
}
