import { Component, ElementRef, OnDestroy, computed, effect, inject, signal, untracked, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { ChatService } from '../../services/chat.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';
import { ChatContact, ChatMessage, messagePreview } from '../../models/chat.model';

interface ContactItem {
  contact: ChatContact;
  preview: string;
  time: string;
  unread: number;
}

interface MessageDay {
  label: string;
  messages: ChatMessage[];
}

@Component({
  selector: 'app-messages-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, RouterLink],
  templateUrl: './messages.page.html',
  styleUrl: './messages.page.scss',
})
export class MessagesPage implements OnDestroy {
  readonly chat = inject(ChatService);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly history = viewChild<ElementRef<HTMLElement>>('history');

  readonly personId = this.personContext.personId;
  private readonly contactFromRoute = toSignal(this.route.queryParamMap.pipe(map(params => params.get('contact'))));

  readonly selectedContactId = signal('');
  readonly draft = signal('');
  readonly sending = signal(false);
  readonly sendError = signal('');
  readonly messagesLoading = signal(false);
  readonly acceptingId = signal('');
  readonly notificationPermission = signal(this.chat.notificationPermission());

  // only persons with at least one message, the backend returns only those as conversations
  readonly contactItems = computed<ContactItem[]>(() =>
    this.chat.conversations().map(conversation => ({
      contact: conversation.contact,
      preview: messagePreview(conversation.lastMessage),
      time: relativeTime(conversation.lastMessage.sentAt),
      unread: conversation.unreadCount,
    })));

  readonly selectedContact = computed(() =>
    this.contactItems().find(item => item.contact.id === this.selectedContactId())?.contact);

  readonly messageDays = computed<MessageDay[]>(() => {
    const days: MessageDay[] = [];
    for (const message of this.chat.messagesWith(this.selectedContactId()) ?? []) {
      const label = dayLabel(message.sentAt);
      const last = days.at(-1);
      if (last?.label === label) last.messages.push(message);
      else days.push({ label, messages: [message] });
    }
    return days;
  });

  constructor() {
    // the contact from the link of a notification, otherwise the newest conversation
    effect(() => {
      const fromRoute = this.contactFromRoute();
      const items = this.contactItems();
      untracked(() => {
        if (fromRoute && fromRoute !== this.selectedContactId()) {
          this.selectContact(fromRoute);
        } else if (!this.selectedContactId() && items.length) {
          this.selectContact(items[0].contact.id);
        }
      });
    });

    // keep the newest message visible
    effect(() => {
      this.messageDays();
      setTimeout(() => {
        const element = this.history()?.nativeElement;
        if (element) element.scrollTop = element.scrollHeight;
      });
    });
  }

  ngOnDestroy(): void {
    this.chat.activeContactId.set(null);
  }

  selectContact(contactId: string): void {
    this.selectedContactId.set(contactId);
    this.chat.activeContactId.set(contactId);
    this.sendError.set('');
    if (this.contactFromRoute() && this.contactFromRoute() !== contactId) {
      void this.router.navigate([], { queryParams: { contact: null }, replaceUrl: true });
    }

    this.messagesLoading.set(!this.chat.messagesWith(contactId));
    this.chat.loadMessages(contactId).subscribe({
      next: () => {
        this.messagesLoading.set(false);
        this.chat.markAsRead(contactId);
      },
      error: (error: Error) => {
        this.messagesLoading.set(false);
        this.sendError.set(error.message);
      },
    });
  }

  sendMessage(): void {
    const text = this.draft().trim();
    const contactId = this.selectedContactId();
    if (!text || !contactId || this.sending()) return;

    this.sending.set(true);
    this.sendError.set('');
    this.chat.sendText(contactId, text).subscribe({
      next: () => {
        this.draft.set('');
        this.sending.set(false);
      },
      error: (error: Error) => {
        this.sendError.set(error.message);
        this.sending.set(false);
      },
    });
  }

  accept(message: ChatMessage): void {
    this.acceptingId.set(message.id);
    this.sendError.set('');
    this.chat.accept(message).subscribe({
      next: () => this.acceptingId.set(''),
      error: (error: Error) => {
        this.sendError.set(error.message);
        this.acceptingId.set('');
      },
    });
  }

  async enableNotifications(): Promise<void> {
    this.notificationPermission.set(await this.chat.requestNotificationPermission());
  }

  isOutgoing(message: ChatMessage): boolean {
    return message.senderId === this.personId();
  }

  /** The horse page of the customer who shared the horse or measurement. */
  shareLink(message: ChatMessage): string[] {
    const horseId = message.horse?.id ?? message.measurement?.horseId ?? '';
    return ['/customers', message.senderId, 'horses', horseId];
  }

  shareQueryParams(message: ChatMessage): Record<string, string> | null {
    return message.measurement ? { measurementId: message.measurement.id } : null;
  }

  initials(contact: ChatContact): string {
    return `${contact.firstName.charAt(0)}${contact.lastName.charAt(0)}`;
  }

  time(date: Date): string {
    return `${date.toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' })} Uhr`;
  }

  germanDate(value: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
  }
}

function dayLabel(date: Date): string {
  const today = startOfDay(new Date());
  const days = Math.round((today - startOfDay(date)) / 86_400_000);
  if (days === 0) return 'Heute';
  if (days === 1) return 'Gestern';
  return date.toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function relativeTime(date: Date): string {
  const minutes = Math.floor((Date.now() - date.getTime()) / 60_000);
  if (minutes < 1) return 'gerade eben';
  if (minutes < 60) return `vor ${minutes} Min.`;
  if (dayLabel(date) === 'Heute') return date.toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' });
  return dayLabel(date);
}

function startOfDay(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}
