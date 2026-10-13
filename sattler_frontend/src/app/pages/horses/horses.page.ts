import { Component, computed, effect, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { AddHorse } from './add-horse/add-horse';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService } from '../../services/person-api.service';

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
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);

  protected showForm = signal(false);
  readonly backendHorses = signal<Horse[] | null>(null);
  readonly horsesLoading = signal(false);
  readonly horsesError = signal('');

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });
  readonly backendPersonId = computed(() => {
    const routePersonId = this.params().get('customerId');
    if (routePersonId) {
      return /^[1-9]\d*$/.test(routePersonId) ? routePersonId : '';
    }
    return this.personContext.personId();
  });

  readonly customer = computed(() => {
    const id = this.params().get('customerId');
    return id ? this.data.getCustomer(id) : this.data.getMe();
  });

  readonly horses = computed((): Horse[] => {
    if (this.backendPersonId()) {
      return this.backendHorses() ?? [];
    }
    const c = this.customer();
    return c ? this.data.horses().filter(h => h.ownerId === c.id) : [];
  });

  readonly title = computed(() => {
    const c = this.customer();
    if (!c) return this.backendPersonId() ? 'Pferdeübersicht' : '';
    return c.isMe ? 'Meine Pferde' : `${fullName(c)} Pferde`;
  });

  constructor() {
    effect(onCleanup => {
      const personId = this.backendPersonId();
      this.horsesError.set('');
      if (!personId) {
        this.backendHorses.set(null);
        this.horsesLoading.set(false);
        return;
      }

      this.horsesLoading.set(true);
      const subscription = this.personApi.getHorses(personId).subscribe({
        next: horses => {
          this.backendHorses.set(horses);
          this.horsesLoading.set(false);
        },
        error: error => {
          this.backendHorses.set([]);
          this.horsesError.set(
            error instanceof Error ? error.message : 'Die Pferde konnten nicht geladen werden.',
          );
          this.horsesLoading.set(false);
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
    if (this.backendPersonId()) return;
    const c = this.customer();
    if (!c) return;
    this.data.addHorse({ ...horse, ownerId: c.id });
    this.showForm.set(false);
  }

  back(): void {
    this.location.back();
  }
}
