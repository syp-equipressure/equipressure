import { Injectable, signal } from '@angular/core';
import { Customer } from '../models/customer.model';
import { Horse } from '../models/horse.model';
import { Sattler } from '../models/sattler.model';
import { Saddle } from '../models/saddle.model';

/**
 * Mock data service. Once backend ships GET /api/persons + UserController,
 * swap this for an HttpClient-based version.
 */
@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly _customers = signal<Customer[]>([
    {
      id: 'me',
      firstName: 'Sophie',
      lastName: 'Grüneis',
      email: 'sophie.grueneis@example.com',
      heightCm: 170,
      weightKg: 62,
      isMe: true,
      horseIds: ['my-1', 'my-2'],
    },
    {
      id: 'max',
      firstName: 'Max',
      lastName: 'Mustermann',
      email: 'max@example.com',
      heightCm: 182,
      weightKg: 84,
      horseIds: ['kas', 'petzi', 'safira', 'bella', 'mira', 'isa'],
    },
    {
      id: 'anna',
      firstName: 'Anna',
      lastName: 'Nass',
      email: 'anna.nass@example.com',
      heightCm: 168,
      weightKg: 59,
      horseIds: [],
    },
    {
      id: 'flora',
      firstName: 'Flora',
      lastName: 'Fauna',
      email: 'flora@example.com',
      heightCm: 171,
      weightKg: 63,
      horseIds: [],
    },
    {
      id: 'kathy',
      firstName: 'Kathy',
      lastName: 'Rattenburg',
      email: 'kathy@example.com',
      heightCm: 166,
      weightKg: 57,
      horseIds: [],
    },
    {
      id: 'kai',
      firstName: 'Kai',
      lastName: 'Huber',
      email: 'kai@example.com',
      heightCm: 180,
      weightKg: 79,
      horseIds: [],
    },
  ]);

  private readonly _horses = signal<Horse[]>([
    {
      id: 'kas',
      name: 'Kas',
      age: 18,
      breed: 'Englisches Vollblut',
      ownerId: 'max',
      heightCm: 165,
      weightKg: 520,
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRhcJ4yrtCZcwZc1TLgAuVvcN_P_nOTSFNroA&s',
    },
    {
      id: 'petzi',
      name: 'Petzi',
      age: 10,
      breed: 'Haflinger',
      ownerId: 'max',
      heightCm: 152,
      weightKg: 480,
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSWt6wuJmZQE2BDls6b8qBH9tZn53jbqvx2xg&s',
    },
    {
      id: 'safira',
      name: 'Safira',
      age: 13,
      breed: 'KWPN',
      ownerId: 'max',
      heightCm: 168,
      weightKg: 540,
      imageUrl:
        'https://www.landtiere.de/assets/images/34/738/34738694-haflinger-pferd-feld-gelb-fell-langhaar-2o4uhwbOmce9.jpg',
    },
    {
      id: 'bella',
      name: 'Bella',
      age: 8,
      breed: 'Isländer',
      ownerId: 'max',
      heightCm: 145,
      weightKg: 410,
      imageUrl:
        'https://www.peta.de/wp-content/uploads/2020/11/horse-721136_1920-1024x682.jpg',
    },
    {
      id: 'mira',
      name: 'Mira',
      age: 18,
      breed: 'Englisches Vollblut',
      ownerId: 'max',
      heightCm: 164,
      weightKg: 505,
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRBLATKZfxrR8DGubqewwirncrMtwXZzF02sQ&s',
    },
    {
      id: 'isa',
      name: 'Isa',
      age: 18,
      breed: 'Englisches Vollblut',
      ownerId: 'max',
      heightCm: 163,
      weightKg: 500,
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSkqcEdRShL_kALpMAucGhIcsUs7yABkEZpng&s',
    },
    {
      id: 'my-1',
      name: 'Luna',
      age: 6,
      breed: 'Hannoveraner',
      ownerId: 'me',
      heightCm: 160,
      weightKg: 470,
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSSw0pJRnJEQ5mLbJk_DsEoMDyGIdkVhxua3w&s',
    },
    {
      id: 'my-2',
      name: 'Stella',
      age: 1,
      breed: 'Trakehner',
      ownerId: 'me',
      heightCm: 138,
      weightKg: 320,
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ30aD8kRv-jJmBgc6toXFbMnbpogQFtagj4w&s',
    },
  ]);

  private readonly _saddles = signal<Saddle[]>([
    { id: 'saddle-kas-1', horseId: 'kas', name: 'Prestige X-D2', category: 'Dressur' },
    { id: 'saddle-kas-2', horseId: 'kas', name: 'Kentaur Ithaka', category: 'Dressur' },
    { id: 'saddle-petzi-1', horseId: 'petzi', name: 'Prestige X-D2', category: 'Dressur' },
    { id: 'saddle-petzi-2', horseId: 'petzi', name: 'Wintec 500', category: 'Vielseitigkeit' },
    { id: 'saddle-safira-1', horseId: 'safira', name: 'Amerigo Vega', category: 'Dressur' },
    { id: 'saddle-safira-2', horseId: 'safira', name: 'Prestige Roma', category: 'Springen' },
    { id: 'saddle-bella-1', horseId: 'bella', name: 'Icelandic Pro', category: 'Gangpferd' },
    { id: 'saddle-bella-2', horseId: 'bella', name: 'Top Reiter', category: 'Gangpferd' },
    { id: 'saddle-mira-1', horseId: 'mira', name: 'Prestige X-D2', category: 'Dressur' },
    { id: 'saddle-isa-1', horseId: 'isa', name: 'Prestige X-D2', category: 'Dressur' },
    { id: 'saddle-luna-1', horseId: 'my-1', name: 'Passier Compact', category: 'Dressur' },
    { id: 'saddle-luna-2', horseId: 'my-1', name: 'Kieffer Wien', category: 'Vielseitigkeit' },
    { id: 'saddle-stella-1', horseId: 'my-2', name: 'Wintec 500', category: 'Vielseitigkeit' },
  ]);

  private readonly _sattler = signal<Sattler>({
    id: 'sattler-1',
    firstName: 'Sophie',
    lastName: 'Grüneis',
    email: 'sophie.grueneis@sattlerei.example',
    phoneNumber: '+43 660 1234567',
    companyName: 'Sattlerei Grüneis',
    address: 'Lenaustraße 18, 4050 Traun',
    website: 'https://www.flexibler-sattel.at',
    description: 'Wir passen alle Sättel an, die gepolstert werden können und deren Kopf-eisen sich kalt verstellen lässt.',
    memberSince: '03/2024',
  });

  // ── Sattler ──────────────────────────────────────────────────────────────────

  readonly sattler = this._sattler.asReadonly();

  getSattler(): Sattler {
    return this._sattler();
  }

  updateSattler(patch: Partial<Sattler>): void {
    this._sattler.update(current => ({ ...current, ...patch }));
  }

  // ── Customers ──────────────────────────────────────────────────────────────

  readonly customers = this._customers.asReadonly();

  getCustomers(): Customer[] {
    return this._customers();
  }

  getCustomer(id: string): Customer | undefined {
    return this._customers().find(c => c.id === id);
  }

  getMe(): Customer {
    return this._customers().find(c => c.isMe)!;
  }

  addCustomer(customer: Customer): void {
    this._customers.update(list => [...list, customer]);
  }

  upsertCustomers(customers: Customer[]): void {
    this._customers.update(current => {
      const byId = new Map(current.map(customer => [customer.id, customer]));
      for (const customer of customers) {
        byId.set(customer.id, customer);
      }
      return [...byId.values()];
    });
  }

  // ── Horses ─────────────────────────────────────────────────────────────────

  readonly horses = this._horses.asReadonly();

  getHorsesOf(customerId: string): Horse[] {
    return this._horses().filter(h => h.ownerId === customerId);
  }

  getHorse(id: string): Horse | undefined {
    return this._horses().find(h => h.id === id);
  }

  getSaddlesOfHorse(horseId: string): Saddle[] {
    return this._saddles().filter(saddle => saddle.horseId === horseId);
  }

  addHorse(horse: Horse): void {
    this._horses.update(list => [...list, horse]);
  }

  upsertHorses(horses: Horse[]): void {
    this._horses.update(current => {
      const byId = new Map(current.map(horse => [horse.id, horse]));
      for (const horse of horses) {
        byId.set(horse.id, horse);
      }
      return [...byId.values()];
    });
  }

}
