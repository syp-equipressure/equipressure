import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';

@Component({
  selector: 'app-customers-page',
  standalone: true,
  imports: [RouterLink, FormsModule, PageHeaderComponent],
  templateUrl: './customers.page.html',
  styleUrl: './customers.page.scss',
})
export class CustomersPage {
  private readonly data = inject(DataService);

  readonly query = signal('');
  readonly customers = signal<Customer[]>(this.data.getCustomers());

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    const list = this.customers();
    if (!q) return list;
    return list.filter(c => fullName(c).toLowerCase().includes(q));
  });

  fullName = fullName;
}
