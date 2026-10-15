import { Component, Input, effect, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SidenavService } from '../../services/sidenav.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { PersonApiService } from '../../services/person-api.service';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
})
export class PageHeaderComponent {
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() showBack = false;
  @Input() showMenu = true;

  private readonly location = inject(Location);
  private readonly sidenav = inject(SidenavService);
  private readonly router = inject(Router);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly personApi = inject(PersonApiService);

  readonly accountName = signal('Konto');
  readonly accountEmail = signal('');

  get profileActive(): boolean {
    return this.router.url.startsWith('/profile');
  }

  constructor() {
    effect(onCleanup => {
      const personId = this.personContext.personId();
      this.accountName.set('Konto');
      this.accountEmail.set('');
      if (!personId) return;

      const subscription = forkJoin({
        person: this.personApi.getPersonDetails(personId),
        profile: this.personApi.getProfileData(personId),
      }).subscribe({
        next: ({ person, profile }) => {
          this.accountName.set(`${profile.firstName} ${profile.lastName}`);
          this.accountEmail.set(person.email ?? '');
        },
        error: () => {
          this.accountName.set('Konto');
          this.accountEmail.set('');
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  get sidenavIsOpen(): boolean {
    return this.sidenav.open();
  }

  get sidenavOpenLabel(): string {
    return this.sidenavIsOpen ? 'Menü schließen' : 'Menü öffnen';
  }

  openMenu() {
    this.sidenav.toggle();
  }

  back() {
    this.location.back();
  }
}
