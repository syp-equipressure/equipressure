import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DataService } from '../../services/data.service';
import { Customer, fullName } from '../../models/customer.model';
import { Horse } from '../../models/horse.model';
import { Measurement } from '../../models/measurement.model';
import { PageHeaderComponent } from '../../shared/page-header/page-header';

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

  readonly customer = computed(() => this.data.getMe());
  readonly recentMeasurements = computed<RecentMeasurement[]>(() =>
    this.data.horses()
      .flatMap(horse =>
        this.data.getMeasurementsOf(horse.id).map(measurement => ({
          measurement,
          horse,
          owner: this.data.getCustomer(horse.ownerId),
        })),
      )
      .sort((left, right) => dateValue(right.measurement.date) - dateValue(left.measurement.date))
      .slice(0, 4),
  );

  fullName = fullName;
}

function dateValue(date: string): number {
  const [day, month, year] = date.split('.').map(Number);
  return new Date(year, month - 1, day).getTime();
}
