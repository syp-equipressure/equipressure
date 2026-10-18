import { Component, Input, inject } from '@angular/core';
import { Location } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { SidenavService } from '../../services/sidenav.service';
import { DataService } from '../../services/data.service';
import { fullName } from '../../models/customer.model';

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

  get accountName(): string {
    return fullName(this.data.getMe());
  }

  get accountEmail(): string {
    return this.data.getMe().email ?? '';
  }

  get backendError(): string {
    return this.data.backendError();
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
