import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { SidenavComponent } from './shared/sidenav/sidenav';
import { SidenavService } from './services/sidenav.service';
import { ChatToastsComponent } from './shared/chat-toasts/chat-toasts';
import { ChatService } from './services/chat.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidenavComponent, ChatToastsComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('sattler_frontend');
  protected readonly sidenav = inject(SidenavService);

  /** The onboarding after the first login is shown without the app navigation */
  protected readonly isOnboarding = toSignal(
    inject(Router).events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(event => event.urlAfterRedirects.startsWith('/onboarding')),
    ),
    { initialValue: false },
  );

  // connects the chat websocket on start, so new messages are notified on every page
  protected readonly chat = inject(ChatService);
}
