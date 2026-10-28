import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { Measurement } from '../../models/measurement.model';
import { PressureDevice } from '../../models/device.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { BackendApiService } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';

interface RecentMeasurement {
  id: string;
  horseName: string;
  ownerName: string;
  /** dd.mm.yyyy */
  date: string;
  meanNcm2: number | null;
  link: string[];
}

interface OwnedHorse {
  horse: Horse;
  ownerName: string;
  isMe: boolean;
}

const RECENT_COUNT = 4;

@Component({
  selector: 'app-startpage',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink],
  templateUrl: './startpage.page.html',
  styleUrl: './startpage.page.scss',
})
export class StartPage {
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);

  private readonly backendCustomer = signal<Customer | null>(null);
  readonly customer = computed(() => this.backendCustomer() ?? this.data.getMe());
  readonly firstName = computed(() => this.customer().firstName.trim());
  readonly backendDevices = signal<PressureDevice[] | null>(null);
  readonly devices = computed(() => this.backendDevices() ?? this.data.devices());
  readonly backendDeviceError = signal('');
  readonly backendProfileError = signal('');
  readonly bluetoothNotice = signal('');

  /** Die neuesten Messungen aus dem Backend, null solange keine Backend-Person eingestellt ist. */
  private readonly backendRecent = signal<RecentMeasurement[] | null>(null);
  readonly recentLoading = signal(false);

  readonly recentMeasurements = computed<RecentMeasurement[]>(() =>
    this.backendRecent() ?? this.localRecent(),
  );

  fullName = fullName;

  constructor() {
    effect(onCleanup => {
      const personId = this.personContext.personId();
      this.backendDeviceError.set('');
      if (!personId) {
        this.backendDevices.set(null);
        this.backendCustomer.set(null);
        return;
      }
      const subscriptions = [
        this.api.getDevices(personId).subscribe({
          next: devices => this.backendDevices.set(devices),
          error: error => {
            this.backendDevices.set([]);
            this.backendDeviceError.set(
              error instanceof Error ? error.message : 'Messgeräte konnten nicht geladen werden.',
            );
          },
        }),
        this.api.getPerson(personId).subscribe({
          next: customer => {
            this.backendCustomer.set(customer);
            this.data.upsertCustomers([customer]);
            this.backendProfileError.set('');
          },
          error: error => {
            this.backendCustomer.set(null);
            this.backendProfileError.set(
              error instanceof Error ? error.message : 'Profil konnte nicht geladen werden.',
            );
          },
        }),
      ];
      onCleanup(() => subscriptions.forEach(subscription => subscription.unsubscribe()));
    });

    // die neuesten Messungen der eigenen Pferde und der Pferde aller Kunden
    effect(onCleanup => {
      const personId = this.personContext.personId();
      if (!personId) {
        this.backendRecent.set(null);
        this.recentLoading.set(false);
        return;
      }
      this.recentLoading.set(true);
      const subscription = this.loadOwnedHorses(personId).pipe(
        switchMap(owned => owned.length
          ? forkJoin(owned.map(item => this.api.getMeasurementSummariesOfHorse(item.horse.id).pipe(
              map(summaries => summaries.map(summary => ({ summary, item }))),
              catchError(() => of([])),
            )))
          : of([])),
        map(lists => lists.flat()
          .sort((left, right) => right.summary.date.localeCompare(left.summary.date))
          .slice(0, RECENT_COUNT)),
        switchMap(latest => latest.length
          ? forkJoin(latest.map(({ summary, item }) =>
              this.api.getMeasurementGroupOverview(summary.id).pipe(
                catchError(() => of({ name: '', meanNcm2: null })),
                map((overview): RecentMeasurement => ({
                  id: summary.id,
                  horseName: item.horse.name,
                  ownerName: item.ownerName,
                  date: toGermanDate(summary.date),
                  meanNcm2: overview.meanNcm2,
                  link: item.isMe
                    ? ['/my-horses', item.horse.id]
                    : ['/customers', item.horse.ownerId, 'horses', item.horse.id],
                })),
              )))
          : of([] as RecentMeasurement[])),
      ).subscribe({
        next: recent => {
          this.backendRecent.set(recent);
          this.recentLoading.set(false);
        },
        error: () => {
          this.backendRecent.set([]);
          this.recentLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  connectDevice(device: PressureDevice): void {
    this.bluetoothNotice.set(
      device.name
    );
  }

  /** Ohne Backend: die in dieser Sitzung aufgenommenen Messungen. */
  private localRecent(): RecentMeasurement[] {
    return this.data.recordedMeasurements()
      .flatMap(measurement => {
        const horse = this.data.getHorse(measurement.horseId);
        if (!horse) return [];
        const owner = this.data.getCustomer(horse.ownerId);
        return [toRecent(measurement, horse, owner, horse.ownerId === this.customer().id)];
      })
      .sort((left, right) => dateValue(right.date) - dateValue(left.date))
      .slice(0, RECENT_COUNT);
  }

  /** Eigene Pferde und die Pferde aller Kunden, jeweils mit dem Namen der Besitzer:in. */
  private loadOwnedHorses(personId: string): Observable<OwnedHorse[]> {
    const own$ = forkJoin({
      person: this.api.getPerson(personId),
      horses: this.api.getHorses(personId).pipe(catchError(() => of([] as Horse[]))),
    }).pipe(map(({ person, horses }) =>
      horses.map(horse => ({ horse, ownerName: fullName(person), isMe: true }))));
    const customers$ = this.api.getContacts(personId).pipe(
      switchMap(contacts => contacts.length
        ? forkJoin(contacts.map(contact => this.api.getHorses(contact.id).pipe(
            map(horses => horses.map(horse => ({ horse, ownerName: fullName(contact), isMe: false }))),
            catchError(() => of([] as OwnedHorse[])),
          )))
        : of([] as OwnedHorse[][])),
      map(lists => lists.flat()),
    );
    return forkJoin([own$, customers$]).pipe(map(([own, customers]) => [...own, ...customers]));
  }
}

function toRecent(measurement: Measurement, horse: Horse, owner: Customer | undefined, isMe: boolean): RecentMeasurement {
  return {
    id: measurement.id,
    horseName: horse.name,
    ownerName: owner ? fullName(owner) : 'Unbekannte Kundschaft',
    date: measurement.date,
    meanNcm2: measurement.detail.meanNcm2,
    link: isMe ? ['/my-horses', horse.id] : ['/customers', horse.ownerId, 'horses', horse.id],
  };
}

/** yyyy-mm-dd → dd.mm.yyyy */
function toGermanDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}

function dateValue(date: string): number {
  const [day, month, year] = date.split('.').map(Number);
  return new Date(year, month - 1, day).getTime();
}
