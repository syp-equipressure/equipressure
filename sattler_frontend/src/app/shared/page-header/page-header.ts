import { Component, Input, inject } from '@angular/core';
import { Location } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { SidenavService } from '../../services/sidenav.service';
import { DataService } from '../../services/data.service';
import { fullName } from '../../models/customer.model';
import { AuthService } from '../../services/auth.service';
import { AccountService } from '../../services/account.service';
import { BackendApiService } from '../../services/backend-api.service';

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
  private readonly data = inject(DataService);
  private readonly auth = inject(AuthService);
  private readonly account = inject(AccountService);
  private readonly api = inject(BackendApiService);

  get accountName(): string {
    const me = this.account.me();
    return me ? `${me.firstName} ${me.lastName}` : fullName(this.data.getMe());
  }

  /** A freshly uploaded image comes first, it is newer than the data of the login. */
  get avatarUrl(): string | undefined {
    const me = this.account.me();
    return this.data.sattler().avatarUrl
      ?? (me ? this.api.personImageUrl(String(me.id), me.imageUpdatedAt ?? undefined) : undefined);
  }

  get accountEmail(): string {
    return this.account.me()?.email ?? this.data.getMe().email ?? '';
  }

  logout(): void {
    this.auth.logout();
  }

  get profileActive(): boolean {
    return this.router.url.startsWith('/profile');
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
