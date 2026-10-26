import { Component, EventEmitter, Output, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../../services/theme.service';

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

  readonly theme = inject(ThemeService);

  readonly items: NavItem[] = [
    { label: 'Neue Messung',  icon: 'add_circle_outline', route: '/new-measurement' },
    { label: 'Meine Pferde',  icon: 'horse',              route: '/my-horses' },
    { label: 'Kundenverwaltung', icon: 'people_outline', route: '/customers' },
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

  onNavigate() {
    if (window.innerWidth <= 900) {
      this.close.emit();
    }
  }
}
