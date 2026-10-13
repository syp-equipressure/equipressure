import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom, Subject } from 'rxjs';

export interface ChatConversation {
  id: string;
  participantId: string;
  participantName: string;
  lastMessageText: string;
  lastMessageAt: string;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  senderId: string;
  isMine: boolean;
  text: string;
  sentAt: string;
}

export interface ChatMessagePage {
  items: ChatMessage[];
  nextCursor: string | null;
}

export type ChatConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'unavailable';

const API_BASE = '/api/chat';
const WEBSOCKET_PATH = '/api/chat/ws';
const MAX_MESSAGE_LENGTH = 4000;

// TODO (Backend): implement authenticated conversation listing and cursor-paged history,
// plus a token-validated WebSocket that persists/broadcasts authorized messages.
@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http: HttpClient;
  private socket: WebSocket | null = null;
  private reconnectTimer: ReturnType<typeof setTimeout> | undefined;
  private stableConnectionTimer: ReturnType<typeof setTimeout> | undefined;
  private reconnectAttempts = 0;
  private shouldReconnect = false;
  private readonly incoming = new Subject<ChatMessage>();
  private readonly connectionErrors = new Subject<string>();

  readonly status = signal<ChatConnectionStatus>('disconnected');
  readonly messages$ = this.incoming.asObservable();
  readonly errors$ = this.connectionErrors.asObservable();

  constructor(http: HttpClient) {
    this.http = http;
  }

  async loadConversations(): Promise<ChatConversation[]> {
    const headers = this.authHeaders();
    return firstValueFrom(this.http.get<ChatConversation[]>(`${API_BASE}/conversations`, { headers }));
  }

  async loadMessages(conversationId: string, cursor: string | null): Promise<ChatMessagePage> {
    const headers = this.authHeaders();
    const params: Record<string, string> = { limit: '50' };
    if (cursor) params['cursor'] = cursor;
    return firstValueFrom(
      this.http.get<ChatMessagePage>(
        `${API_BASE}/conversations/${encodeURIComponent(conversationId)}/messages`,
        { headers, params },
      ),
    );
  }

  connectAfterLogin(): void {
    // TODO: Call this from the real auth login lifecycle once an auth service exists.
    this.shouldReconnect = true;
    this.reconnectAttempts = 0;
    void this.openConnection();
  }

  closeOnLogout(): void {
    // TODO: Bind this to the real auth logout lifecycle once an auth service exists.
    this.disconnect();
  }

  disconnect(): void {
    this.shouldReconnect = false;
    this.clearReconnectTimer();
    this.clearStableConnectionTimer();
    this.socket?.close(1000, 'Sitzung beendet');
    this.socket = null;
    this.status.set('disconnected');
  }

  sendMessage(conversationId: string, text: string): void {
    const value = text.trim();
    if (!value) throw new Error('Eine Nachricht darf nicht leer sein.');
    if (value.length > MAX_MESSAGE_LENGTH) {
      throw new Error(`Eine Nachricht darf höchstens ${MAX_MESSAGE_LENGTH} Zeichen lang sein.`);
    }
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      throw new Error('Die Verbindung ist unterbrochen. Nachricht wurde nicht gesendet.');
    }

    this.socket.send(JSON.stringify({
      type: 'message',
      conversationId,
      text: value,
    }));
  }

  private async openConnection(): Promise<void> {
    if (!this.shouldReconnect || this.socket) return;

    let token: string;
    try {
      token = this.getAccessToken();
    } catch (error) {
      this.status.set('unavailable');
      this.connectionErrors.next(errorMessage(error));
      return;
    }

    this.status.set(this.reconnectAttempts === 0 ? 'connecting' : 'reconnecting');
    const url = new URL(WEBSOCKET_PATH, window.location.origin);
    url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:';
    // TODO: Prefer an authenticated cookie or WebSocket handshake header when backend auth supports it.
    // The backend/proxy must redact this query parameter from request logs.
    url.searchParams.set('access_token', token);

    let socket: WebSocket;
    try {
      socket = new WebSocket(url);
    } catch (error) {
      this.onConnectionFailure(errorMessage(error));
      return;
    }
    this.socket = socket;

    socket.addEventListener('open', () => {
      if (this.socket !== socket) return;
      this.clearStableConnectionTimer();
      this.stableConnectionTimer = setTimeout(() => {
        this.reconnectAttempts = 0;
        this.stableConnectionTimer = undefined;
      }, 30000);
      this.status.set('connected');
    });
    socket.addEventListener('message', event => this.handleSocketMessage(event));
    socket.addEventListener('error', () => {
      if (this.socket === socket) this.connectionErrors.next('Die Chat-Verbindung ist fehlgeschlagen.');
    });
    socket.addEventListener('close', event => {
      if (this.socket !== socket) return;
      this.socket = null;
      this.clearStableConnectionTimer();
      if (event.code === 1008 || event.code === 4401 || event.code === 4403) {
        this.shouldReconnect = false;
        this.status.set('unavailable');
        this.connectionErrors.next('Die Anmeldung für den Chat ist ungültig oder abgelaufen.');
        return;
      }
      if (this.shouldReconnect) this.scheduleReconnect();
      else this.status.set('disconnected');
    });
  }

  private handleSocketMessage(event: MessageEvent<string>): void {
    let payload: unknown;
    try {
      payload = JSON.parse(event.data);
    } catch {
      this.connectionErrors.next('Der Chatserver hat eine ungültige Antwort gesendet.');
      return;
    }

    if (!payload || typeof payload !== 'object') {
      this.connectionErrors.next('Der Chatserver hat eine ungültige Antwort gesendet.');
      return;
    }
    const data = payload as { type?: unknown; message?: unknown; payload?: unknown };
    if (data.type === 'error') {
      this.connectionErrors.next(typeof data.message === 'string' ? data.message : 'Nachricht konnte nicht gesendet werden.');
      return;
    }
    if (data.type !== 'message' || !isChatMessage(data.payload)) {
      this.connectionErrors.next('Der Chatserver hat eine unbekannte Antwort gesendet.');
      return;
    }
    this.incoming.next(data.payload);
  }

  private scheduleReconnect(): void {
    this.reconnectAttempts += 1;
    this.status.set('reconnecting');
    this.connectionErrors.next('Verbindung unterbrochen. Der Chat versucht, die Verbindung wiederherzustellen.');
    const delay = Math.min(1000 * 2 ** (this.reconnectAttempts - 1), 30000);
    this.clearReconnectTimer();
    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = undefined;
      void this.openConnection();
    }, delay);
  }

  private onConnectionFailure(message: string): void {
    this.socket = null;
    if (this.shouldReconnect) this.scheduleReconnect();
    else this.status.set('unavailable');
    this.connectionErrors.next(message);
  }

  private clearReconnectTimer(): void {
    if (this.reconnectTimer !== undefined) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = undefined;
    }
  }

  private clearStableConnectionTimer(): void {
    if (this.stableConnectionTimer !== undefined) {
      clearTimeout(this.stableConnectionTimer);
      this.stableConnectionTimer = undefined;
    }
  }

  private authHeaders(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.getAccessToken()}` });
  }

  private getAccessToken(): string {
    // TODO: Replace this placeholder with the existing login/auth token provider.
    throw new Error('Die Chat-Anmeldung kann noch nicht verwendet werden: Im Frontend fehlt ein Auth-Service.');
  }
}

function isChatMessage(value: unknown): value is ChatMessage {
  if (!value || typeof value !== 'object') return false;
  const message = value as Partial<ChatMessage>;
  return (
    typeof message.id === 'string' &&
    typeof message.conversationId === 'string' &&
    typeof message.senderId === 'string' &&
    typeof message.isMine === 'boolean' &&
    typeof message.text === 'string' &&
    typeof message.sentAt === 'string'
  );
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Die Anfrage ist fehlgeschlagen.';
}
