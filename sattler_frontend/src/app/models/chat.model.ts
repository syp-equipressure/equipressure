export type ChatMessageType = 'Text' | 'Horse' | 'Measurement' | 'File';

/** A file or image sent in a message, its content is loaded from the backend. */
export interface ChatFile {
  fileName: string;
  contentType: string;
  size: number;
}

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
  /** When an app of the recipient received the message */
  deliveredAt: Date | null;
  readAt: Date | null;
  acceptedAt: Date | null;
  senderId: string;
  recipientId: string;
  horse: SharedHorse | null;
  measurement: SharedMeasurement | null;
  file: ChatFile | null;
}

export interface ChatContact {
  id: string;
  firstName: string;
  lastName: string;
  /** ISO timestamp when the image was set in the backend, missing without an image */
  imageUpdatedAt?: string;
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
    // the backend removes the reference when the horse or measurement is deleted
    case 'Horse':
      return message.horse ? `Pferd geteilt: ${message.horse.name}` : 'Geteiltes Pferd gelöscht';
    case 'Measurement':
      return message.measurement ? `Messung geteilt: ${message.measurement.horseName}` : 'Geteilte Messung gelöscht';
    case 'File':
      return message.file?.contentType.startsWith('image/') ? 'Bild' : `Datei: ${message.file?.fileName ?? ''}`;
    default:
      return message.text ?? '';
  }
}
