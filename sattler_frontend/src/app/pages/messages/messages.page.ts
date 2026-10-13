import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ChatConversation, ChatMessage, ChatService } from '../../services/chat.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';

const MAX_MESSAGE_LENGTH = 4000;

@Component({
  selector: 'app-messages-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './messages.page.html',
  styleUrl: './messages.page.scss',
})
export class MessagesPage implements OnInit, OnDestroy {
  private readonly chat = inject(ChatService);
  private readonly subscriptions = new Subscription();
  private toastTimer: ReturnType<typeof setTimeout> | undefined;
  private activeHistoryRequest = 0;

  @ViewChild('chatHistory') private chatHistory?: ElementRef<HTMLElement>;

  readonly conversations = signal<ChatConversation[]>([]);
  readonly selectedConversationId = signal<string | null>(null);
  readonly selectedConversation = signal<ChatConversation | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly conversationsError = signal('');
  readonly historyError = signal('');
  readonly nextCursor = signal<string | null>(null);
  readonly loadingConversations = signal(true);
  readonly loadingMessages = signal(false);
  readonly loadingOlder = signal(false);
  readonly draft = signal('');
  readonly mobileChatOpen = signal(false);
  readonly toast = signal('');
  readonly connectionStatus = this.chat.status;

  ngOnInit(): void {
    this.subscriptions.add(this.chat.messages$.subscribe(message => this.receiveMessage(message)));
    this.subscriptions.add(this.chat.errors$.subscribe(message => this.showToast(message)));
    this.chat.connectAfterLogin();
    void this.loadConversations();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.chat.disconnect();
    if (this.toastTimer !== undefined) clearTimeout(this.toastTimer);
  }

  async loadConversations(): Promise<void> {
    this.loadingConversations.set(true);
    this.conversationsError.set('');
    try {
      const conversations = await this.chat.loadConversations();
      const orderedConversations = sortConversations(conversations);
      this.conversations.set(orderedConversations);
      const firstConversation = orderedConversations[0];
      if (firstConversation) {
        await this.selectConversation(firstConversation.id);
      } else {
        this.mobileChatOpen.set(true);
      }
    } catch (error) {
      const message = errorMessage(error);
      this.conversationsError.set(message);
      this.showToast(message);
    } finally {
      this.loadingConversations.set(false);
    }
  }

  async selectConversation(conversationId: string): Promise<void> {
    const conversation = this.conversations().find(item => item.id === conversationId);
    if (!conversation) return;

    const requestId = ++this.activeHistoryRequest;
    this.selectedConversationId.set(conversationId);
    this.selectedConversation.set(conversation);
    this.mobileChatOpen.set(true);
    this.messages.set([]);
    this.nextCursor.set(null);
    this.loadingMessages.set(true);
    this.historyError.set('');
    try {
      const page = await this.chat.loadMessages(conversationId, null);
      if (requestId !== this.activeHistoryRequest) return;
      this.messages.set(page.items);
      this.nextCursor.set(page.nextCursor);
      this.scrollToBottom();
    } catch (error) {
      if (requestId === this.activeHistoryRequest) {
        this.historyError.set(errorMessage(error));
        this.showToast(this.historyError());
      }
    } finally {
      if (requestId === this.activeHistoryRequest) this.loadingMessages.set(false);
    }
  }

  async loadOlderMessages(): Promise<void> {
    const conversationId = this.selectedConversationId();
    const cursor = this.nextCursor();
    if (!conversationId || !cursor || this.loadingOlder()) return;

    this.loadingOlder.set(true);
    const previousHeight = this.chatHistory?.nativeElement.scrollHeight ?? 0;
    try {
      const page = await this.chat.loadMessages(conversationId, cursor);
      if (conversationId !== this.selectedConversationId()) return;
      this.messages.update(messages => [...page.items, ...messages]);
      this.nextCursor.set(page.nextCursor);
      requestAnimationFrame(() => {
        const history = this.chatHistory?.nativeElement;
        if (history) history.scrollTop = history.scrollHeight - previousHeight;
      });
    } catch (error) {
      this.showToast(errorMessage(error));
    } finally {
      this.loadingOlder.set(false);
    }
  }

  sendMessage(): void {
    const conversationId = this.selectedConversationId();
    const message = this.draft().trim();
    if (!conversationId || !message) return;
    if (message.length > MAX_MESSAGE_LENGTH) {
      this.showToast(`Eine Nachricht darf höchstens ${MAX_MESSAGE_LENGTH} Zeichen lang sein.`);
      return;
    }

    try {
      this.chat.sendMessage(conversationId, message);
      this.draft.set('');
    } catch (error) {
      this.showToast(errorMessage(error));
    }
  }

  backToConversations(): void {
    this.mobileChatOpen.set(false);
  }

  isOwnMessage(message: ChatMessage): boolean {
    return message.isMine;
  }

  avatarInitials(name: string): string {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part.charAt(0)).join('').toUpperCase();
  }

  formatTime(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? value
      : `${date.toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' })} Uhr`;
  }

  private receiveMessage(message: ChatMessage): void {
    if (this.selectedConversationId() === message.conversationId) {
      this.messages.update(messages =>
        messages.some(existing => existing.id === message.id) ? messages : [...messages, message],
      );
      this.scrollToBottom();
    }

    this.conversations.update(conversations =>
      sortConversations(conversations.map(conversation =>
        conversation.id === message.conversationId
          ? { ...conversation, lastMessageText: message.text, lastMessageAt: message.sentAt }
          : conversation,
      )),
    );
    const selected = this.selectedConversation();
    if (selected?.id === message.conversationId) {
      this.selectedConversation.set({
        ...selected,
        lastMessageText: message.text,
        lastMessageAt: message.sentAt,
      });
    }
  }

  private scrollToBottom(): void {
    requestAnimationFrame(() => {
      const history = this.chatHistory?.nativeElement;
      if (history) history.scrollTop = history.scrollHeight;
    });
  }

  private showToast(message: string): void {
    this.toast.set(message);
    if (this.toastTimer !== undefined) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toast.set(''), 5000);
  }
}

function sortConversations(conversations: ChatConversation[]): ChatConversation[] {
  return [...conversations].sort(
    (left, right) => new Date(right.lastMessageAt).getTime() - new Date(left.lastMessageAt).getTime(),
  );
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Die Anfrage ist fehlgeschlagen.';
}
