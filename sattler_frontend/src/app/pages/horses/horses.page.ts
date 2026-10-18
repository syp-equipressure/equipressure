import {Component, computed, effect, inject, signal} from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { AddHorse } from './add-horse/add-horse';
import { BackendApiService } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';

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
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);

  protected showForm = signal(false);
  readonly backendHorses = signal<Horse[] | null>(null);
  readonly backendLoading = signal(false);
  readonly backendError = signal('');
  readonly backendPersonId = this.personContext.personId;
  readonly backendOwnerId = computed(() => this.params().get('customerId') ?? this.backendPersonId());
  readonly backendMode = computed(() => /^[1-9]\d*$/.test(this.backendOwnerId() ?? ''));

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly customer = computed(() => {
    const id = this.params().get('customerId');
    return id
      ? this.data.getCustomer(id)
      : this.data.getMe();
  });

  readonly horses = computed(() => {
    const c = this.customer();
    const backendHorses = this.backendHorses();
    return backendHorses ?? (c ? this.data.horses().filter(h => h.ownerId === c.id) : []);
  });

  readonly title = computed(() => {
    const c = this.customer();
    if (!c) return '';
    return c.isMe ? 'Meine Pferde' : `${fullName(c)} Pferde`;
  });

  constructor() {
    effect(onCleanup => {
      const ownerId = this.backendOwnerId();
      this.backendError.set('');
      if (!/^[1-9]\d*$/.test(ownerId)) {
        this.backendHorses.set(null);
        this.backendLoading.set(false);
        return;
      }
      this.backendLoading.set(true);
      const subscription = this.api.getHorses(ownerId).subscribe({
        next: horses => {
          this.data.upsertHorses(horses);
          this.backendHorses.set(horses);
          this.backendLoading.set(false);
        },
        error: error => {
          this.backendHorses.set([]);
          this.backendError.set(error instanceof Error ? error.message : 'Pferde konnten nicht geladen werden.');
          this.backendLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  openHorse(horseId: string): void {
    const c = this.customer();
    if (!c) return;
    if (c.isMe) {
      this.router.navigate(['/my-horses', horseId]);
      return;
    }
    this.router.navigate(['/customers', c.id, 'horses', horseId]);
  }

  addHorse(horse: Horse): void {
    if (this.backendMode()) {
      this.data.upsertHorses([horse]);
      this.backendHorses.update(horses => [...(horses ?? []).filter(item => item.id !== horse.id), horse]);
      this.showForm.set(false);
      return;
    }
    const c = this.customer();
    if (!c) return;
    this.data.addHorse({ ...horse, ownerId: c.id });
    this.showForm.set(false);
  }

  openAddHorseForm(): void {
    this.showForm.set(true);
  }

  back(): void {
    this.location.back();
  }
}
