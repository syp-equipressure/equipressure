import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
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
}
