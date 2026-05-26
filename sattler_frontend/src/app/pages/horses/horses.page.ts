import { Component, computed, inject } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { fullName } from '../../models/customer.model';

@Component({
  selector: 'app-horses-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './horses.page.html',
  styleUrl: './horses.page.scss',
})
export class HorsesPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly data = inject(DataService);

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly customer = computed(() => {
    const id = this.params().get('customerId') ?? '';
    return this.data.getCustomer(id);
  });

  readonly horses = computed(() => {
    const c = this.customer();
    return c ? this.data.getHorsesOf(c.id) : [];
  });

  readonly title = computed(() => {
    const c = this.customer();
    if (!c) return '';
    return c.isMe ? 'Deine Pferde' : `${fullName(c)}'s Pferde`;
  });

  openHorse(horseId: string) {
    const c = this.customer();
    if (!c) return;
    this.router.navigate(['/customers', c.id, 'horses', horseId]);
  }

  back() {
    this.location.back();
  }
}
