import { Component, Input, inject } from '@angular/core';
import { Location } from '@angular/common';
import { SidenavService } from '../../services/sidenav.service';

@Component({
  selector: 'app-page-header',
  standalone: true,
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

  openMenu() {
    this.sidenav.show();
  }

  back() {
    this.location.back();
  }
}
