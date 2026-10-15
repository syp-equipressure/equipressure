import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { Measurement } from '../../models/measurement.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService } from '../../services/person-api.service';
import { MeasurementService } from '../../services/measurement.service';

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
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);
  private readonly measurements = inject(MeasurementService);

  readonly firstName = signal('');
  readonly customer = computed(() => this.data.getMe());
  readonly recentMeasurements = computed<RecentMeasurement[]>(() =>
    this.measurements.getRecentMeasurements()
      .flatMap(measurement => {
        const horse = this.data.getHorse(measurement.horseId);
        if (!horse) return [];
        return [{
          measurement,
          horse,
          owner: this.data.getCustomer(horse.ownerId),
        }];
      })
      .sort((left, right) => dateValue(right.measurement.date) - dateValue(left.measurement.date))
      .slice(0, 4),
  );

  fullName = fullName;

  constructor() {
    effect(onCleanup => {
      const personId = this.personContext.personId();
      this.firstName.set('');
      if (!personId) return;

      const subscription = this.personApi.getProfileData(personId).subscribe({
        next: profile => this.firstName.set(profile.firstName),
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }
}

function dateValue(date: string): number {
  const [day, month, year] = date.split('.').map(Number);
  return new Date(year, month - 1, day).getTime();
}
