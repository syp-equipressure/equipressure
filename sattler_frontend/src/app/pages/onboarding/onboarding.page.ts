import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { AuthService } from '../../services/auth.service';

type Step = 'profile' | 'device';

/** First login: set up the profile, then claim the saddler device with its serial number */
@Component({
  selector: 'app-onboarding-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './onboarding.page.html',
  styleUrl: './onboarding.page.scss',
})
export class OnboardingPage implements OnInit {
  private readonly account = inject(AccountService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly step = signal<Step>('profile');
  readonly saving = signal(false);
  readonly error = signal('');
  readonly email = this.auth.email;

  readonly profile = {
    firstName: this.auth.firstName,
    lastName: this.auth.lastName,
    dateOfBirth: '',
    height: null as number | null,
    weight: null as number | null,
    street: '',
    plz: '',
    city: '',
  };
  serialNumber = '';

  async ngOnInit(): Promise<void> {
    // the guard already loaded the state, a person without saddler role only needs the device
    if (this.account.me()) {
      this.step.set('device');
    }
  }

  async saveProfile(): Promise<void> {
    const p = this.profile;
    if (!p.firstName.trim() || !p.lastName.trim() || !p.dateOfBirth || !p.height || !p.weight ||
      !p.street.trim() || !p.plz.trim() || !p.city.trim()) {
      this.error.set('Bitte fülle alle Felder aus.');
      return;
    }

    await this.run(async () => {
      await this.account.createAccount({
        firstName: p.firstName.trim(),
        lastName: p.lastName.trim(),
        dateOfBirth: p.dateOfBirth,
        height: p.height!,
        weight: p.weight!,
        address: { addressName: p.street.trim(), cityName: p.city.trim(), plz: p.plz.trim() },
      });
      this.step.set('device');
    }, {
      400: 'Bitte überprüfe deine Angaben (z.B. Geburtsdatum in der Vergangenheit, Größe und Gewicht über 0).',
      409: 'Mit dieser E-Mail-Adresse gibt es bereits ein Profil.',
    });
  }

  async claimDevice(): Promise<void> {
    const serialNumber = this.serialNumber.trim();
    if (!serialNumber) {
      this.error.set('Bitte gib die Seriennummer deines Satteldruckmessgeräts ein.');
      return;
    }

    await this.run(async () => {
      await this.account.claimDevice(serialNumber);
      await this.router.navigateByUrl('/');
    }, {
      409: 'Dieses Gerät ist bereits einem anderen Konto zugeordnet.',
    });
  }

  logout(): void {
    this.auth.logout();
  }

  private async run(action: () => Promise<void>, messages: Record<number, string>): Promise<void> {
    this.saving.set(true);
    this.error.set('');
    try {
      await action();
    } catch (error) {
      const status = error instanceof HttpErrorResponse ? error.status : 0;
      this.error.set(messages[status] ?? 'Das hat leider nicht funktioniert. Bitte versuche es später noch einmal.');
    } finally {
      this.saving.set(false);
    }
  }
}
