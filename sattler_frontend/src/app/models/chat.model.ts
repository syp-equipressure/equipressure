export type ChatMessageType = 'Text' | 'Horse' | 'Measurement';

export interface SharedHorse {
  id: string;
  name: string;
}

export interface SharedMeasurement {
  id: string;
  name: string;
  /** yyyy-mm-dd */
  date: string;
  horseId: string;
  horseName: string;
}

export interface ChatMessage {
  id: string;
  type: ChatMessageType;
  text: string | null;
  sentAt: Date;
  readAt: Date | null;
  acceptedAt: Date | null;
  senderId: string;
  recipientId: string;
  horse: SharedHorse | null;
  measurement: SharedMeasurement | null;
}

export interface ChatContact {
  id: string;
  firstName: string;
  lastName: string;
}

export interface ChatConversation {
  contact: ChatContact;
  lastMessage: ChatMessage;
  unreadCount: number;
}

/** A notification about a new message, shown as a toast in the app. */
export interface ChatToast {
  id: number;
  contactId: string;
  title: string;
  text: string;
}

/** Short text of a message for previews and notifications. */
export function messagePreview(message: ChatMessage): string {
  switch (message.type) {
    case 'Horse':
      return `Pferd geteilt: ${message.horse?.name ?? ''}`.trim();
    case 'Measurement':
      return `Messung geteilt: ${message.measurement?.horseName ?? ''}`.trim();
    default:
      return message.text ?? '';
  }
}
