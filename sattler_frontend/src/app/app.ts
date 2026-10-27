import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { SidenavComponent } from './shared/sidenav/sidenav';
import { SidenavService } from './services/sidenav.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidenavComponent],
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
}
