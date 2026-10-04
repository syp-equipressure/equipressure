import { computed, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { Customer, fullName } from '../../models/customer.model';

interface ChatMessage {
  id: number;
  customerId: string;
  sender: 'customer' | 'sattler';
  message: string;
  timestamp: string;
}

@Component({
  selector: 'app-messages-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './messages.page.html',
  styleUrl: './messages.page.scss',
})
export class MessagesPage {
  private readonly data = inject(DataService);

  readonly customers = computed(() => this.data.getCustomers().filter(customer => !customer.isMe));
  readonly selectedCustomerId = signal(this.customers()[0]?.id ?? '');
  readonly selectedCustomer = computed<Customer | undefined>(() =>
    this.customers().find(customer => customer.id === this.selectedCustomerId()),
  );
  readonly draft = signal('');
  readonly messages = signal<ChatMessage[]>([
    {
      id: 1,
      customerId: 'max',
      sender: 'customer',
      message: 'Hallo Sophie, könnten wir einen Termin für Kas vereinbaren?',
      timestamp: '09:14',
    },
    {
      id: 2,
      customerId: 'max',
      sender: 'sattler',
      message: 'Gerne! Wie wäre es nächste Woche am Dienstag?',
      timestamp: '09:22',
    },
    {
      id: 3,
      customerId: 'max',
      sender: 'customer',
      message: 'Das passt sehr gut. Vielen Dank!',
      timestamp: '09:26',
    },
    {
      id: 4,
      customerId: 'anna',
      sender: 'customer',
      message: 'Ich wollte wegen der letzten Messung nachfragen.',
      timestamp: 'Gestern',
    },
  ]);
  readonly selectedMessages = computed(() =>
    this.messages().filter(message => message.customerId === this.selectedCustomerId()),
  );

  selectCustomer(customerId: string): void {
    this.selectedCustomerId.set(customerId);
  }

  latestMessage(customerId: string): string {
    return (
      [...this.messages()].reverse().find(message => message.customerId === customerId)?.message ??
      'Noch keine Nachrichten'
    );
  }

  sendMessage(): void {
    const message = this.draft().trim();
    const customerId = this.selectedCustomerId();
    if (!message || !customerId) return;

    this.messages.update(messages => [
      ...messages,
      {
        id: Date.now(),
        customerId,
        sender: 'sattler',
        message,
        timestamp: new Date().toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' }),
      },
    ]);
    this.draft.set('');
  }

  readonly fullName = fullName;
}
