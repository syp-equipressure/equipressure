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

  readonly messages = signal<ChatMessage[]>([
    {
      id: 1,
      customerId: 'max',
      sender: 'sattler',
      message: 'Lorem ipsum dolor sit amet, consectetur sadipscing elitr, sed',
      timestamp: '12:04 Uhr',
    },
    {
      id: 2,
      customerId: 'max',
      sender: 'customer',
      message: 'Vielen Dank! Lg',
      timestamp: '12:49 Uhr',
    },
    {
      id: 3,
      customerId: 'anna',
      sender: 'customer',
      message: 'Sonntag, 5 Uhr? Oder passt dir Montag besser?',
      timestamp: 'vor 3 Minuten',
    },
  ]);
  readonly customers = computed(() => {
    const conversationIds = new Set(this.messages().map(message => message.customerId));
    return this.data.getCustomers().filter(customer => conversationIds.has(customer.id) && !customer.isMe);
  });
  readonly selectedCustomerId = signal(this.customers()[0]?.id ?? '');
  readonly selectedCustomer = computed<Customer | undefined>(() =>
    this.customers().find(customer => customer.id === this.selectedCustomerId()),
  );
  readonly draft = signal('');
  readonly selectedMessages = computed(() =>
    this.messages().filter(message => message.customerId === this.selectedCustomerId()),
  );

  selectCustomer(customerId: string): void {
    this.selectedCustomerId.set(customerId);
  }

  latestMessage(customerId: string): string {
    return [...this.messages()].reverse().find(message => message.customerId === customerId)?.message ??
      'Noch keine Nachrichten';
  }

  latestMessageTime(customerId: string): string {
    if (customerId === 'max') return 'vor 2 Stunden';
    return [...this.messages()].reverse().find(message => message.customerId === customerId)?.timestamp ?? '';
  }

  avatarUrl(customer: Customer): string | undefined {
    return customer.horseIds
      .map(horseId => this.data.getHorse(horseId)?.imageUrl)
      .find((imageUrl): imageUrl is string => !!imageUrl);
  }

  customerHorseNames(customer: Customer): string[] {
    return customer.horseIds
      .map(horseId => this.data.getHorse(horseId)?.name)
      .filter((name): name is string => !!name)
      .slice(0, 2)
      .reverse();
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
        timestamp: `${new Date().toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' })} Uhr`,
      },
    ]);
    this.draft.set('');
  }

  readonly fullName = fullName;
}
