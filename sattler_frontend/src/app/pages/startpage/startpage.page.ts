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

interface RecentMeasurement {
  measurement: Measurement;
  horse: Horse;
  owner: Customer | undefined;
}

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
  readonly recentMeasurements = computed<RecentMeasurement[]>(() =>
    this.data.recordedMeasurements()
      .flatMap(measurement => {
        const horse = this.data.getHorse(measurement.horseId);
        return horse ? [{
          measurement,
          horse,
          owner: this.data.getCustomer(horse.ownerId),
        }] : [];
      })
      .sort((left, right) => dateValue(right.measurement.date) - dateValue(left.measurement.date))
      .slice(0, 4),
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
  }

  connectDevice(device: PressureDevice): void {
    this.bluetoothNotice.set(
      device.name
    );
  }
}

function dateValue(date: string): number {
  const [day, month, year] = date.split('.').map(Number);
  return new Date(year, month - 1, day).getTime();
}
