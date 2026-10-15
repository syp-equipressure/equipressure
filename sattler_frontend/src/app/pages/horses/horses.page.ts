import { Component, computed, effect, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService, BackendPerson } from '../../services/person-api.service';

interface RiderHorses {
  rider: Customer;
  horses: Horse[];
}

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
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);
  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  readonly horsesByRider = signal<RiderHorses[]>([]);
  readonly horsesLoading = signal(false);
  readonly horsesError = signal('');

  readonly customerId = computed(() => this.params().get('customerId') ?? '');
  readonly title = computed(() => this.customerId() ? 'Pferde' : 'Meine Pferde');

  constructor() {
    effect(onCleanup => {
      const customerId = this.customerId();
      const personId = this.personContext.personId();
      this.horsesError.set('');
      this.horsesByRider.set([]);

      if (customerId) {
        if (!/^[1-9]\d*$/.test(customerId)) {
          this.horsesLoading.set(false);
          this.horsesError.set('Die Personen-ID ist ungültig.');
          return;
        }
        this.loadRiders([customerId], onCleanup);
        return;
      }

      if (!personId) {
        this.horsesLoading.set(false);
        return;
      }

      this.horsesLoading.set(true);
      const subscription = this.personApi.getContacts(personId).subscribe({
        next: contacts => {
          // TODO Backend-Bug: contacts currently includes the requested person, not just customers.
          const riderIds = contacts
            .filter(contact => contact.id !== personId)
            .map(contact => contact.id);
          if (riderIds.length === 0) {
            this.horsesByRider.set([]);
            this.horsesLoading.set(false);
            return;
          }

          this.loadRiders(riderIds, onCleanup);
        },
        error: error => {
          this.horsesError.set(
            error instanceof Error ? error.message : 'Die Kontakte konnten nicht geladen werden.',
          );
          this.horsesLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  openHorse(riderId: string, horseId: string): void {
    void this.router.navigate(['/customers', riderId, 'horses', horseId]);
  }

  back(): void {
    this.location.back();
  }

  readonly fullName = fullName;

  private loadRiders(riderIds: string[], onCleanup: (cleanupFn: () => void) => void): void {
    this.horsesLoading.set(true);
    const subscription = forkJoin(
      riderIds.map(id =>
        forkJoin({
          details: this.personApi.getPersonDetails(id),
          horses: this.personApi.getHorses(id),
        }),
      ),
    ).subscribe({
      next: results => {
        const groups = results.map(({ details, horses }) => ({
          rider: toCustomer(details),
          horses,
        }));
        this.data.upsertCustomers(groups.map(group => group.rider));
        this.data.upsertHorses(groups.flatMap(group => group.horses));
        this.horsesByRider.set(groups.filter(group => group.horses.length > 0));
        this.horsesLoading.set(false);
      },
      error: error => {
        this.horsesError.set(
          error instanceof Error ? error.message : 'Die Pferde konnten nicht geladen werden.',
        );
        this.horsesLoading.set(false);
      },
    });
    onCleanup(() => subscription.unsubscribe());
  }
}

function toCustomer(person: BackendPerson): Customer {
  return { ...person, isMe: false };
}
