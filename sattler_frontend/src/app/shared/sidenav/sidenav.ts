import { Component, EventEmitter, Output, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../../services/theme.service';
import { ChatService } from '../../services/chat.service';

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
  private readonly chat = inject(ChatService);

  // the badge shows the number of unread chat messages
  readonly items = computed<NavItem[]>(() => [
    { label: 'Neue Messung',  icon: 'add_circle_outline', route: '/new-measurement' },
    { label: 'Meine Pferde',  icon: 'horse',              route: '/my-horses' },
    { label: 'Kundenverwaltung', icon: 'people_outline', route: '/customers' },
    { label: 'Nachrichten',   icon: 'chat_bubble_outline', route: '/messages', badge: this.chat.unreadTotal() || undefined },
  ]);

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
