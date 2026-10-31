import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, map, tap } from 'rxjs';
import {
  ChatContact,
  ChatConversation,
  ChatMessage,
  ChatMessageType,
  ChatToast,
  messagePreview,
} from '../models/chat.model';
import { BACKEND_BASE_URL, toApiError } from './backend-api.service';
import { AuthService } from './auth.service';
import { BackendPersonContextService } from './backend-person-context.service';

const TOAST_DURATION_MS = 6000;
const MAX_RECONNECT_DELAY_MS = 30000;

/**
 * Chat with the customers. Messages are sent over REST, new, read and accepted messages arrive live over the chat
 * websocket of the backend. The person is the one of the logged-in account.
 */
@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly personContext = inject(BackendPersonContextService);
  private readonly auth = inject(AuthService);

  readonly conversations = signal<ChatConversation[]>([]);
  readonly conversationsLoading = signal(false);
  readonly error = signal('');
  readonly connected = signal(false);
  readonly toasts = signal<ChatToast[]>([]);
  /** The messages of every opened conversation, by contact id. */
  private readonly messagesByContact = signal<ReadonlyMap<string, ChatMessage[]>>(new Map());
  /** The conversation that is open on the messages page, its messages are not notified. */
  readonly activeContactId = signal<string | null>(null);

  readonly unreadTotal = computed(() =>
    this.conversations().reduce((sum, conversation) => sum + conversation.unreadCount, 0));

  private socket: WebSocket | null = null;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private reconnectDelay = 1000;
  private toastId = 0;

  constructor() {
    effect(onCleanup => {
      const personId = this.personContext.personId();
      untracked(() => {
        this.conversations.set([]);
        this.messagesByContact.set(new Map());
        if (personId) {
          this.loadConversations().subscribe({ error: () => undefined });
          this.connect(personId);
        }
      });
      onCleanup(() => this.disconnect());
    });
  }

  messagesWith(contactId: string): ChatMessage[] | undefined {
    return this.messagesByContact().get(contactId);
  }

  loadConversations(): Observable<ChatConversation[]> {
    const personId = this.personContext.personId();
    this.conversationsLoading.set(true);
    return this.http.get<unknown>(`${BACKEND_BASE_URL}/persons/${personId}/chats`).pipe(
      map(response => listFrom(response, 'conversations').map(mapConversation)),
      catchError(toApiError),
      tap({
        next: conversations => {
          this.conversations.set(conversations);
          this.conversationsLoading.set(false);
          this.error.set('');
        },
        error: (error: Error) => {
          this.conversationsLoading.set(false);
          this.error.set(error.message);
        },
      }),
    );
  }

  loadMessages(contactId: string): Observable<ChatMessage[]> {
    const personId = this.personContext.personId();
    return this.http.get<unknown>(`${BACKEND_BASE_URL}/persons/${personId}/chats/${contactId}`).pipe(
      map(response => listFrom(response, 'messages').map(mapMessage)),
      tap(messages => this.setMessages(contactId, messages)),
      catchError(toApiError),
    );
  }

  sendText(contactId: string, text: string): Observable<ChatMessage> {
    return this.send(contactId, 'Text', { text });
  }

  /** Sends a file or image (at most 10 MB) with an optional text. */
  sendFile(contactId: string, file: File, text?: string): Observable<ChatMessage> {
    const personId = this.personContext.personId();
    const form = new FormData();
    form.append('file', file, file.name);
    if (text) form.append('text', text);
    return this.http.post<unknown>(`${BACKEND_BASE_URL}/persons/${personId}/chats/${contactId}/files`, form).pipe(
      map(mapMessage),
      tap(message => this.upsertMessage(message)),
      catchError(toApiError),
    );
  }

  /**
   * Address of the content of a sent file, usable as image source and download link.
   * Contains the access token, because images and links cannot send the Authorization header.
   */
  fileUrl(message: ChatMessage): string {
    const url = `${BACKEND_BASE_URL}/persons/${this.personContext.personId()}/chats/messages/${message.id}/file`;
    return `${url}?access_token=${encodeURIComponent(this.auth.accessToken)}`;
  }

  /** Sends a measurement, e.g. one the saddler did for the customer. */
  sendMeasurement(contactId: string, measurementGroupId: string, text?: string): Observable<ChatMessage> {
    return this.send(contactId, 'Measurement', { text, measurementGroupId });
  }

  markAsRead(contactId: string): void {
    const personId = this.personContext.personId();
    const conversation = this.conversations().find(item => item.contact.id === contactId);
    if (!personId || !conversation?.unreadCount) return;

    this.updateConversation(contactId, item => ({ ...item, unreadCount: 0 }));
    this.http.put<void>(`${BACKEND_BASE_URL}/persons/${personId}/chats/${contactId}/read`, {})
      .subscribe({ error: () => undefined });
  }

  /** Confirms that all received messages arrived (second tick for the senders). */
  private markAsDelivered(): void {
    const personId = this.personContext.personId();
    if (!personId) return;
    this.http.put<void>(`${BACKEND_BASE_URL}/persons/${personId}/chats/delivered`, {})
      .subscribe({ error: () => undefined });
  }

  /** Adds the horse or measurement shared in a received message. */
  accept(message: ChatMessage): Observable<ChatMessage> {
    const personId = this.personContext.personId();
    return this.http.put<unknown>(`${BACKEND_BASE_URL}/persons/${personId}/chats/messages/${message.id}/accept`, {})
      .pipe(map(mapMessage), tap(accepted => this.upsertMessage(accepted)), catchError(toApiError));
  }

  /** Makes sure the contact shows up in the list, even without messages yet. */
  contactOf(contactId: string): ChatContact | undefined {
    return this.conversations().find(item => item.contact.id === contactId)?.contact;
  }

  notificationPermission(): NotificationPermission | 'unsupported' {
    return typeof Notification === 'undefined' ? 'unsupported' : Notification.permission;
  }

  async requestNotificationPermission(): Promise<NotificationPermission | 'unsupported'> {
    if (typeof Notification === 'undefined') return 'unsupported';
    return Notification.requestPermission();
  }

  dismissToast(id: number): void {
    this.toasts.update(toasts => toasts.filter(toast => toast.id !== id));
  }

  openToast(toast: ChatToast): void {
    this.dismissToast(toast.id);
    void this.router.navigate(['/messages'], { queryParams: { contact: toast.contactId } });
  }

  private send(contactId: string, type: ChatMessageType,
               body: { text?: string; measurementGroupId?: string }): Observable<ChatMessage> {
    const personId = this.personContext.personId();
    return this.http.post<unknown>(`${BACKEND_BASE_URL}/persons/${personId}/chats/${contactId}`, {
      type,
      text: body.text || null,
      horseId: null,
      measurementGroupId: body.measurementGroupId ? Number(body.measurementGroupId) : null,
    }).pipe(
      map(mapMessage),
      // the websocket delivers the message as well, upserting twice is harmless
      tap(message => this.upsertMessage(message)),
      catchError(toApiError),
    );
  }

  private connect(personId: string): void {
    this.disconnect();
    // browsers cannot send the Authorization header when opening a websocket
    const url = `${BACKEND_BASE_URL.replace(/^http/, 'ws')}/persons/${personId}/chats/ws`
      + `?access_token=${encodeURIComponent(this.auth.accessToken)}`;
    const socket = new WebSocket(url);
    this.socket = socket;

    socket.onopen = () => {
      this.connected.set(true);
      this.reconnectDelay = 1000;
      // messages sent while offline are delivered now
      this.markAsDelivered();
    };
    socket.onmessage = event => this.handleEvent(event.data);
    socket.onclose = () => {
      if (this.socket !== socket) return;
      this.connected.set(false);
      this.socket = null;
      this.scheduleReconnect(personId);
    };
  }

  private disconnect(): void {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.reconnectTimer = null;
    const socket = this.socket;
    this.socket = null;
    socket?.close();
    this.connected.set(false);
  }

  private scheduleReconnect(personId: string): void {
    this.reconnectTimer = setTimeout(() => {
      if (this.personContext.personId() !== personId) return;
      this.connect(personId);
      // catch up with messages sent while the connection was gone
      this.loadConversations().subscribe({ error: () => undefined });
    }, this.reconnectDelay);
    this.reconnectDelay = Math.min(this.reconnectDelay * 2, MAX_RECONNECT_DELAY_MS);
  }

  private handleEvent(data: unknown): void {
    if (typeof data !== 'string') return;
    let event: unknown;
    try {
      event = JSON.parse(data);
    } catch {
      return;
    }
    if (!isRecord(event)) return;

    const me = this.personContext.personId();
    if (event['type'] === 'message' || event['type'] === 'accepted') {
      const message = mapMessage(event['message']);
      this.upsertMessage(message);
      if (event['type'] === 'message' && message.recipientId === me) this.onIncoming(message);
    } else if (event['type'] === 'delivered') {
      const recipientId = String(event['readerId']);
      if (recipientId !== me) this.markSent(recipientId, 'deliveredAt');
    } else if (event['type'] === 'read') {
      const readerId = String(event['readerId']);
      const contactId = String(event['contactId']);
      if (readerId === me) {
        // read in another tab
        this.updateConversation(contactId, item => ({ ...item, unreadCount: 0 }));
      } else {
        this.markSent(readerId, 'readAt');
      }
    }
  }

  private onIncoming(message: ChatMessage): void {
    const contactId = message.senderId;
    this.markAsDelivered();
    const isOpen = this.activeContactId() === contactId && this.router.url.startsWith('/messages');
    if (isOpen && !this.document.hidden) {
      this.markAsRead(contactId);
      return;
    }

    const contact = this.contactOf(contactId);
    if (!contact) {
      // first message of a new contact, the list needs their name
      this.loadConversations().subscribe({
        next: () => this.notify(message),
        error: () => undefined,
      });
      return;
    }
    this.notify(message);
  }

  private notify(message: ChatMessage): void {
    const contact = this.contactOf(message.senderId);
    const title = contact ? `${contact.firstName} ${contact.lastName}` : 'Neue Nachricht';
    const text = messagePreview(message);

    const toast: ChatToast = { id: ++this.toastId, contactId: message.senderId, title, text };
    this.toasts.update(toasts => [...toasts.slice(-2), toast]);
    setTimeout(() => this.dismissToast(toast.id), TOAST_DURATION_MS);

    // a system notification only helps if the tab is not visible
    if (this.document.hidden && this.notificationPermission() === 'granted') {
      const notification = new Notification(title, { body: text, tag: `chat-${message.senderId}` });
      notification.onclick = () => {
        this.document.defaultView?.focus();
        this.openToast(toast);
        notification.close();
      };
    }
  }

  private setMessages(contactId: string, messages: ChatMessage[]): void {
    this.messagesByContact.update(current => new Map(current).set(contactId, messages));
  }

  private upsertMessage(message: ChatMessage): void {
    const me = this.personContext.personId();
    const contactId = message.senderId === me ? message.recipientId : message.senderId;

    const loaded = this.messagesByContact().get(contactId);
    if (loaded) {
      const index = loaded.findIndex(item => item.id === message.id);
      this.setMessages(contactId, index < 0
        ? [...loaded, message]
        : loaded.map(item => item.id === message.id ? message : item));
    }

    const conversation = this.conversations().find(item => item.contact.id === contactId);
    if (!conversation) {
      if (message.senderId === me) this.loadConversations().subscribe({ error: () => undefined });
      return;
    }
    const isNew = !loaded?.some(item => item.id === message.id) && conversation.lastMessage.id !== message.id;
    const isLast = isNew || conversation.lastMessage.id === message.id;
    const unread = isNew && message.recipientId === me ? conversation.unreadCount + 1 : conversation.unreadCount;
    this.updateConversation(contactId, item => ({
      ...item,
      lastMessage: isLast ? message : item.lastMessage,
      unreadCount: unread,
    }));
  }

  /** Sets the status of the messages sent to [recipientId], read messages are always delivered as well. */
  private markSent(recipientId: string, status: 'deliveredAt' | 'readAt'): void {
    const now = new Date();
    const update = (message: ChatMessage): ChatMessage => message.recipientId === recipientId && !message[status]
      ? { ...message, [status]: now, deliveredAt: message.deliveredAt ?? now }
      : message;
    const loaded = this.messagesByContact().get(recipientId);
    if (loaded) this.setMessages(recipientId, loaded.map(update));
    this.updateConversation(recipientId, item => ({ ...item, lastMessage: update(item.lastMessage) }));
  }

  private updateConversation(contactId: string, update: (conversation: ChatConversation) => ChatConversation): void {
    this.conversations.update(conversations => conversations
      .map(item => item.contact.id === contactId ? update(item) : item)
      .sort((left, right) => right.lastMessage.sentAt.getTime() - left.lastMessage.sentAt.getTime()));
  }
}

function mapConversation(value: unknown): ChatConversation {
  const record = isRecord(value) ? value : {};
  const contact = isRecord(record['contact']) ? record['contact'] : {};
  return {
    contact: {
      id: String(contact['id'] ?? ''),
      firstName: String(contact['firstName'] ?? ''),
      lastName: String(contact['lastName'] ?? ''),
      imageUpdatedAt: typeof contact['imageUpdatedAt'] === 'string' ? contact['imageUpdatedAt'] : undefined,
    },
    lastMessage: mapMessage(record['lastMessage']),
    unreadCount: Number(record['unreadCount'] ?? 0),
  };
}

function mapMessage(value: unknown): ChatMessage {
  const record = isRecord(value) ? value : {};
  const horse = isRecord(record['horse']) ? record['horse'] : null;
  const measurement = isRecord(record['measurement']) ? record['measurement'] : null;
  const file = isRecord(record['file']) ? record['file'] : null;
  const type = record['type'];
  return {
    id: String(record['id'] ?? ''),
    type: type === 'Horse' || type === 'Measurement' || type === 'File' ? type : 'Text',
    file: file
      ? {
          fileName: String(file['fileName'] ?? 'Datei'),
          contentType: String(file['contentType'] ?? 'application/octet-stream'),
          size: Number(file['size'] ?? 0),
        }
      : null,
    text: typeof record['text'] === 'string' ? record['text'] : null,
    sentAt: toDate(record['sentAt']) ?? new Date(),
    deliveredAt: toDate(record['deliveredAt']),
    readAt: toDate(record['readAt']),
    acceptedAt: toDate(record['acceptedAt']),
    senderId: String(record['senderId'] ?? ''),
    recipientId: String(record['recipientId'] ?? ''),
    horse: horse ? { id: String(horse['id']), name: String(horse['name'] ?? '') } : null,
    measurement: measurement
      ? {
          id: String(measurement['id']),
          name: String(measurement['name'] ?? ''),
          date: String(measurement['date'] ?? ''),
          horseId: String(measurement['horseId']),
          horseName: String(measurement['horseName'] ?? ''),
        }
      : null,
  };
}

function toDate(value: unknown): Date | null {
  if (typeof value !== 'string' || !value) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function listFrom(value: unknown, key: string): unknown[] {
  if (Array.isArray(value)) return value;
  const list = isRecord(value) ? value[key] : undefined;
  return Array.isArray(list) ? list : [];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
