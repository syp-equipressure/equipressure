import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';

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
  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly customer = computed(() => this.data.getCustomer(this.params().get('customerId') ?? ''));
  readonly title = computed(() => {
    const customer = this.customer();
    return customer ? `${fullName(customer)} – Kundenprofil` : 'Kundenprofil';
  });
  readonly fullName = fullName;

  formatBirthDate(birthDate: string | undefined): string {
    if (!birthDate) return '–';
    const [year, month, day] = birthDate.split('-');
    return year && month && day ? `${day}.${month}.${year}` : birthDate;
  }
}
