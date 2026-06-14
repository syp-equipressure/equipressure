import {Component, computed, inject, signal} from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { AddHorse } from './add-horse/add-horse';

@Component({
  selector: 'app-horses-page',
  standalone: true,
  imports: [PageHeaderComponent, AddHorse],
  templateUrl: './horses.page.html',
  styleUrl: './horses.page.scss',
})
export class HorsesPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly data = inject(DataService);

  protected showForm = signal(false);

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly customer = computed(() => {
    const id = this.params().get('customerId') ?? '';
    return this.data.customers().find(c => c.id === id);
  });

  readonly horses = computed(() => {
    const c = this.customer();
    return c ? this.data.horses().filter(h => h.ownerId === c.id) : [];
  });

  readonly title = computed(() => {
    const c = this.customer();
    if (!c) return '';
    return c.isMe ? 'Deine Pferde' : `${fullName(c)}'s Pferde`;
  });

  openHorse(horseId: string): void {
    const c = this.customer();
    if (!c) return;
    this.router.navigate(['/customers', c.id, 'horses', horseId]);
  }

  addHorse(horse: Horse): void {
    const c = this.customer();
    console.log('customer:', c);
    console.log('customer id:', c?.id);
    if (!c) return;
    this.data.addHorse({ ...horse, ownerId: c.id });
    this.showForm.set(false);
  }

  back(): void {
    this.location.back();
  }
}
