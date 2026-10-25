import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
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
  // connects the chat websocket on start, so new messages are notified on every page
  protected readonly chat = inject(ChatService);
}
