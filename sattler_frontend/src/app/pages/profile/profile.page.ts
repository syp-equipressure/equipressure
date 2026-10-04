import { Component, computed, inject } from '@angular/core';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { sattlerFullName } from '../../models/sattler.model';

interface StatTile {
  label: string;
  value: number;
  icon: string;
}

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss',
})
export class ProfilePage {
  private readonly data = inject(DataService);

  readonly sattler = this.data.sattler;

  readonly fullName = computed(() => sattlerFullName(this.sattler()));

  readonly initials = computed(() => {
    const s = this.sattler();
    return `${s.firstName.charAt(0)}${s.lastName.charAt(0)}`.toUpperCase();
  });

  readonly stats = computed<StatTile[]>(() => {
    const customerCount = this.data.getCustomers().filter(c => !c.isMe).length;
    const horseCount = this.data.horses().length;
    const measurementCount = this.data
      .horses()
      .reduce((sum, horse) => sum + this.data.getMeasurementsOf(horse.id).length, 0);

    return [
      { label: 'Kund*innen', value: customerCount, icon: 'people_outline' },
      { label: 'Pferde', value: horseCount, icon: 'pets' },
      { label: 'Messungen', value: measurementCount, icon: 'insights' },
    ];
  });
}
