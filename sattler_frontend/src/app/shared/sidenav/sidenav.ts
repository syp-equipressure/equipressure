import { Component, EventEmitter, Output, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
}

@Component({
  selector: 'app-sidenav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidenav.html',
  styleUrl: './sidenav.scss',
})
export class SidenavComponent {
  @Output() close = new EventEmitter<void>();

  private readonly router = inject(Router);

  readonly items: NavItem[] = [
    { label: 'Neue Messung',  icon: 'add_circle_outline', route: '/new-measurement' },
    { label: 'Kund*innen',    icon: 'people_outline',    route: '/customers' },
    { label: 'Nachrichten',   icon: 'chat_bubble_outline', route: '/messages' },
  ];

  readonly settings: NavItem = {
    label: 'Einstellungen',
    icon: 'settings',
    route: '/settings',
  };

  onClose() {
    this.close.emit();
  }
}
