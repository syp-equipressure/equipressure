import { Component, inject } from '@angular/core';
import { ChatService } from '../../services/chat.service';

/** Shows a notification for every new chat message, a click opens the conversation. */
@Component({
  selector: 'app-chat-toasts',
  standalone: true,
  template: `
    <div class="toasts" aria-live="polite">
      @for (toast of chat.toasts(); track toast.id) {
        <div class="toast" role="status">
          <button type="button" class="toast-body" (click)="chat.openToast(toast)">
            <span class="material-icons" aria-hidden="true">chat_bubble</span>
            <span class="toast-text">
              <strong>{{ toast.title }}</strong>
              <span>{{ toast.text }}</span>
            </span>
          </button>
          <button type="button" class="toast-close" aria-label="Benachrichtigung schließen"
            (click)="chat.dismissToast(toast.id)">
            <span class="material-icons">close</span>
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts {
      position: fixed;
      right: 20px;
      bottom: 20px;
      z-index: 1000;
      display: flex;
      flex-direction: column;
      gap: 10px;
      width: min(360px, calc(100vw - 32px));
    }

    .toast {
      display: flex;
      align-items: flex-start;
      border: 1px solid var(--border);
      border-left: 4px solid var(--purple-500);
      border-radius: 12px;
      background: var(--bg-card);
      box-shadow: var(--shadow-elev);
      animation: slide-in 0.2s ease-out;
    }

    .toast-body {
      display: flex;
      flex: 1;
      gap: 12px;
      min-width: 0;
      padding: 12px 4px 12px 14px;
      border: 0;
      background: none;
      color: var(--text);
      text-align: left;
      cursor: pointer;

      .material-icons {
        color: var(--purple-500);
        font-size: 20px;
      }
    }

    .toast-text {
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;

      strong {
        color: var(--text-strong);
        font-size: 14px;
      }

      span {
        overflow: hidden;
        color: var(--text-soft);
        font-size: 13px;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    }

    .toast-close {
      padding: 10px;
      border: 0;
      background: none;
      color: var(--text-muted);
      cursor: pointer;

      .material-icons {
        font-size: 18px;
      }
    }

    @keyframes slide-in {
      from {
        opacity: 0;
        transform: translateY(8px);
      }
    }
  `,
})
export class ChatToastsComponent {
  readonly chat = inject(ChatService);
}
