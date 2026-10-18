import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Customer } from '../models/customer.model';
import { Horse } from '../models/horse.model';
import { Measurement, MeasurementDetail, MeasurementSegmentResult } from '../models/measurement.model';
import { Sattler } from '../models/sattler.model';
import { MeasurementSession } from '../models/measurement-session.model';
import { Saddle } from '../models/saddle.model';
import { PressureDevice } from '../models/device.model';
import { formatDuration } from '../utils/time-format';
import { BackendAddress, BackendApiService, BackendHorse, BackendPerson, BackendSaddle } from './backend-api.service';

@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly document = inject(DOCUMENT);
  private readonly devicesStorageKey = 'equipressure-sattler-devices';
  private readonly _devices = signal<PressureDevice[]>(this.readDevices());
  private readonly api = inject(BackendApiService);
  readonly backendLoading = signal(true);
  readonly backendError = signal('');
  readonly actionError = signal('');

  private readonly _customers = signal<Customer[]>([]);
  private readonly _horses = signal<Horse[]>([]);
  private readonly _saddles = signal<Saddle[]>([]);

  private readonly _measurements = signal<Measurement[]>([
    {
      id: 'm1',
      horseId: 'petzi',
      date: '24.06.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:22 min',
      symmetryPct: 96,
      notes: 'Sattel liegt ruhig. Im Trab gleichmäßige Druckverteilung.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm2',
      horseId: 'petzi',
      date: '17.01.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:22 min',
      symmetryPct: 96,
      notes: 'Petzi lief gleichmäßig und zeigte keine Druckempfindlichkeit.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm3',
      horseId: 'petzi',
      date: '23.11.2024',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:22 min',
      symmetryPct: 96,
      notes: 'Beim nächsten Termin den Widerristbereich erneut kontrollieren.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm4',
      horseId: 'kas',
      date: '10.06.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:18 min',
      symmetryPct: 92,
      notes: 'Beim Aufsteigen rutscht der Sattel leicht nach vorne. Gurtung prüfen.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm5',
      horseId: 'safira',
      date: '08.06.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:25 min',
      symmetryPct: 94,
      notes: 'Gleichmäßiger Sitz. Passform nach dem Training erneut prüfen.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm6',
      horseId: 'bella',
      date: '02.06.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:20 min',
      symmetryPct: 98,
      notes: 'Sehr ausgeglichene Druckverteilung, keine Auffälligkeiten.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm7',
      horseId: 'mira',
      date: '30.05.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:19 min',
      symmetryPct: 91,
      notes: 'Linke Seite beim nächsten Training weiter beobachten.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm8',
      horseId: 'isa',
      date: '25.05.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:23 min',
      symmetryPct: 95,
      notes: 'Im Galopp entspannt. Sattelposition ist unverändert.',
      detail: buildPetziDetail(),
    },
    {
      id: 'm9',
      horseId: 'my-1',
      date: '20.05.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:21 min',
      symmetryPct: 97,
      notes: 'Luna war während der Messung entspannt und aufmerksam.',
      detail: buildPetziDetail(),
    },
  ]);

  private readonly _sattler = signal<Sattler>({
    id: String(this.api.currentPersonId),
    firstName: '',
    lastName: '',
    email: '',
    memberSince: '',
  });

  constructor() {
    void this.loadBackendData();
  }

  // ── Sattler ──────────────────────────────────────────────────────────────────

  readonly sattler = this._sattler.asReadonly();

  getSattler(): Sattler {
    return this._sattler();
  }

  updateSattler(patch: Partial<Sattler>): void {
    this._sattler.update(current => ({ ...current, ...patch }));
  }

  // ── Druckmessgeräte ─────────────────────────────────────────────────────────

  readonly devices = this._devices.asReadonly();

  addDevice(device: PressureDevice): void {
    this.saveDevices([...this._devices(), device]);
  }

  updateDevice(updatedDevice: PressureDevice): void {
    this.saveDevices(this._devices().map(device =>
      device.id === updatedDevice.id ? updatedDevice : device,
    ));
  }

  deleteDevice(id: string): void {
    this.saveDevices(this._devices().filter(device => device.id !== id));
  }

  private saveDevices(devices: PressureDevice[]): void {
    this.document.defaultView?.localStorage.setItem(this.devicesStorageKey, JSON.stringify(devices));
    this._devices.set(devices);
  }

  private readDevices(): PressureDevice[] {
    const stored = this.document.defaultView?.localStorage.getItem(this.devicesStorageKey);
    if (!stored) {
      return [{ id: 'demo-device', name: 'EquiPressure Demo-Sensor', number: 'EP-DEMO-001' }];
    }

    try {
      const value: unknown = JSON.parse(stored);
      if (
        Array.isArray(value) &&
        value.every(device =>
          !!device &&
          typeof device.id === 'string' &&
          typeof device.name === 'string' &&
          typeof device.number === 'string',
        )
      ) {
        return value;
      }
    } catch {
      // Ignore malformed cached devices and start with an empty list.
    }

    return [];
  }

  async saveSattler(patch: Partial<Sattler>): Promise<void> {
    this.actionError.set('');
    const current = this._sattler();
    const id = Number(current.id);
    const request: Parameters<BackendApiService['updatePerson']>[1] = {};
    if (patch.firstName !== undefined) request.firstName = patch.firstName;
    if (patch.lastName !== undefined) request.lastName = patch.lastName;
    if (patch.email !== undefined) request.email = patch.email;
    if (patch.website !== undefined) request.websiteLink = patch.website;
    if (patch.description !== undefined) request.description = patch.description;
    if (patch.address) {
      const address = parseAddress(patch.address);
      if (address) request.address = address;
    }

    if (Number.isInteger(id) && id > 0 && Object.keys(request).length > 0) {
      await this.api.updatePerson(id, request);
    }
    this.updateSattler(patch);
    this._customers.update(customers =>
      customers.map(customer =>
        customer.isMe
          ? {
              ...customer,
              firstName: patch.firstName ?? customer.firstName,
              lastName: patch.lastName ?? customer.lastName,
              email: patch.email ?? customer.email,
              address: patch.address ?? customer.address,
            }
          : customer,
      ),
    );
  }

  reportActionError(error: unknown): void {
    const detail =
      error instanceof HttpErrorResponse && error.status === 0
        ? 'Keine Antwort vom Backend. Prüfe, ob es auf localhost:5200 läuft und ob die Frontend-Adresse in den CORS-Einstellungen des Backends erlaubt ist.'
        : error instanceof HttpErrorResponse && error.status === 409
          ? 'Der Eintrag steht bereits im Konflikt mit vorhandenen Daten. Prüfe insbesondere, ob die E-Mail-Adresse schon verwendet wird.'
        : error instanceof HttpErrorResponse && typeof error.error === 'object' && error.error !== null
          ? ('detail' in error.error && typeof error.error.detail === 'string'
              ? error.error.detail
              : 'title' in error.error && typeof error.error.title === 'string'
                ? error.error.title
                : error.message)
        : error instanceof Error
          ? error.message
          : 'Unbekannter Fehler';
    this.actionError.set(`Änderung konnte nicht gespeichert werden: ${detail}`);
    console.error('Backend action failed', error);
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
    return this._customers().find(c => c.isMe) ?? {
      id: String(this.api.currentPersonId),
      firstName: '',
      lastName: '',
      heightCm: 0,
      weightKg: 0,
      isMe: true,
      horseIds: [],
    };
  }

  addCustomer(customer: Customer): void {
    this._customers.update(list => [...list, customer]);
  }

  async createCustomer(customer: Customer): Promise<void> {
    this.actionError.set('');
    const address = parseAddress(customer.address);
    if (!customer.birthDate || !address) {
      this.addCustomer(customer);
      this.actionError.set(
        'Diese Kundin wurde nur lokal angelegt. Für das Backend sind Geburtsdatum und eine Adresse mit PLZ und Ort erforderlich.',
      );
      return;
    }

    const created = await this.api.createPerson({
      firstName: customer.firstName,
      lastName: customer.lastName,
      height: customer.heightCm,
      weight: customer.weightKg,
      dateOfBirth: customer.birthDate,
      email: customer.email ?? null,
      websiteLink: null,
      description: null,
      address,
      role: { name: 'Equestrian' },
    });
    await this.api.addContact(this.api.currentPersonId, created.id);
    this.addCustomer(mapCustomer(created, false));
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

  addSaddle(saddle: Saddle): void {
    this._saddles.update(list => [...list, saddle]);
  }

  addHorse(horse: Horse): void {
    this._horses.update(list => [...list, horse]);
    this._customers.update(customers =>
      customers.map(customer =>
        customer.id === horse.ownerId && !customer.horseIds.includes(horse.id)
          ? { ...customer, horseIds: [...customer.horseIds, horse.id] }
          : customer,
      ),
    );
  }

  async createHorse(horse: Horse): Promise<void> {
    this.actionError.set('');
    const ownerId = Number(horse.ownerId);
    const address = parseAddress(horse.address);
    const dateOfBirth = parseHorseDate(horse.birthDate);
    const gender = genderToBackend(horse.sex);

    if (!Number.isInteger(ownerId) || ownerId <= 0 || !address || !dateOfBirth || !gender) {
      this.addHorse(horse);
      this.actionError.set(
        'Dieses Pferd wurde nur lokal angelegt. Für das Backend werden Geschlecht, Geburtsdatum und eine Adresse mit PLZ und Ort benötigt.',
      );
      return;
    }

    const breeds = await this.api.getBreeds();
    const breedId = breeds.find(
      breed => breed.name.trim().toLocaleLowerCase('de') === horse.breed.trim().toLocaleLowerCase('de'),
    )?.id;
    if (!breedId) {
      this.addHorse(horse);
      this.actionError.set(
        `Die Rasse „${horse.breed}“ ist im Backend nicht hinterlegt. Das Pferd wurde nur lokal angelegt.`,
      );
      return;
    }

    const created = await this.api.createHorse({
      name: horse.name,
      dateOfBirth,
      weight: horse.weightKg,
      height: horse.heightCm,
      gender,
      address,
      breedIds: [breedId],
      ownerId,
    });
    this.addHorse({ ...horse, id: String(created.id), ownerId: String(ownerId) });
  }

  updateHorse(horse: Horse): void {
    this.actionError.set('Pferde bearbeiten wird vom Backend noch nicht unterstützt; die Änderung ist nur lokal.');
    this._horses.update(list => list.map(item => item.id === horse.id ? horse : item));
  }

  removeHorse(horseId: string): void {
    this.actionError.set('Pferde löschen wird vom Backend noch nicht unterstützt; die Änderung ist nur lokal.');
    const horse = this.getHorse(horseId);
    this._horses.update(list => list.filter(horse => horse.id !== horseId));
    this._saddles.update(list => list.filter(saddle => saddle.horseId !== horseId));
    this._measurements.update(list => list.filter(measurement => measurement.horseId !== horseId));
    if (horse) {
      this._customers.update(customers =>
        customers.map(customer => ({
          ...customer,
          horseIds: customer.horseIds.filter(id => id !== horseId),
        })),
      );
    }
  }

  // ── Measurements ───────────────────────────────────────────────────────────

  getMeasurementsOf(horseId: string): Measurement[] {
    return this._measurements().filter(m => m.horseId === horseId);
  }

  getMeasurement(id: string): Measurement | undefined {
    return this._measurements().find(m => m.id === id);
  }

  removeMeasurement(id: string): void {
    this._measurements.update(list => list.filter(measurement => measurement.id !== id));
  }

  getSamplePressureGrid(): number[][] {
    return buildPetziPressureGrid();
  }

  addSessionMeasurement(session: MeasurementSession): Measurement {
    const owner = this.getCustomer(session.ownerId);
    const saddle = this.getSaddlesOfHorse(session.horseId)
      .find(item => item.id === session.saddleId);
    const completedSegments = session.segments.filter(segment => segment.completed);
    const totalSeconds = completedSegments.reduce(
      (total, segment) => total + segment.durationSeconds,
      0,
    );
    const duration = formatDuration(totalSeconds);
    const detail = buildPetziDetail();
    detail.riderName = owner ? `${owner.firstName} ${owner.lastName}` : detail.riderName;
    detail.riderHeightM = owner ? owner.heightCm / 100 : detail.riderHeightM;
    detail.riderWeightKg = owner?.weightKg ?? detail.riderWeightKg;
    detail.saddleName = saddle?.name ?? detail.saddleName;
    detail.durationFull = `${duration} min`;
    detail.gaits = [...new Set(completedSegments.map(segment => segment.gait))];
    detail.segments = completedSegments.map(segment => ({
      gait: segment.gait,
      side: segment.side,
      durationSeconds: segment.durationSeconds,
      pressureGrid: createSegmentPressureGrid(segment.gait, segment.side),
    }));
    detail.pressureGrid = averageGrids(detail.segments.map(segment => segment.pressureGrid));
    detail.maxNcm2 = gridMaximum(detail.pressureGrid);
    detail.meanNcm2 = gridMean(detail.pressureGrid);

    const measurement: Measurement = {
      id: `m${Date.now()}`,
      horseId: session.horseId,
      date: new Date().toLocaleDateString('de-AT'),
      deviceName: saddle?.name ?? detail.saddleName,
      durationLabel: `${duration} min`,
      symmetryPct: 96,
      notes: session.notes,
      detail,
    };
    this._measurements.update(list => [measurement, ...list]);
    return measurement;
  }

  updateMeasurementNotes(id: string, notes: string): void {
    this._measurements.update(list =>
      list.map(measurement => measurement.id === id ? { ...measurement, notes } : measurement),
    );
  }

  private async loadBackendData(): Promise<void> {
    this.backendLoading.set(true);
    this.backendError.set('');
    try {
      const personId = this.api.currentPersonId;
      const [person, equestriansResponse, ownHorsesResponse] = await Promise.all([
        this.api.getPerson(personId),
        this.api.getEquestrians(),
        this.getHorsesOrEmpty(personId),
      ]);
      const customers = equestriansResponse.persons.filter(customer => customer.id !== personId);
      const customerData = await Promise.all(
        customers.map(async customer => {
          const [detail, horseResponse] = await Promise.all([
            this.api.getPerson(customer.id),
            this.getHorsesOrEmpty(customer.id),
          ]);
          return {
            customer: mapCustomer(detail, false),
            horses: horseResponse.horses.map(horse => mapHorse(horse, customer.id)),
          };
        }),
      );
      const currentCustomer = mapCustomer(person, true);
      const currentHorses = ownHorsesResponse.horses.map(horse => mapHorse(horse, personId));
      const allHorses = [...currentHorses, ...customerData.flatMap(entry => entry.horses)];
      const saddleLists = await Promise.all(
        allHorses.map(horse => this.getSaddlesOrEmpty(Number(horse.id))),
      );

      this._customers.set([
        {
          ...currentCustomer,
          horseIds: currentHorses.map(horse => horse.id),
        },
        ...customerData.map(({ customer, horses }) => ({
          ...customer,
          horseIds: horses.map(horse => horse.id),
        })),
      ]);
      this._horses.set(allHorses);
      this._saddles.set(saddleLists.flat().map(mapSaddle));
      this._sattler.set(mapSattler(person));
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unbekannter Fehler';
      this.backendError.set(`Backend-Daten konnten nicht geladen werden: ${message}`);
      console.error('Backend data loading failed', error);
    } finally {
      this.backendLoading.set(false);
    }
  }

  private async getHorsesOrEmpty(personId: number): Promise<{ horses: BackendHorse[] }> {
    try {
      return await this.api.getHorses(personId);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        return { horses: [] };
      }
      throw error;
    }
  }

  private async getSaddlesOrEmpty(horseId: number): Promise<BackendSaddle[]> {
    try {
      return (await this.api.getSaddles(horseId)).saddles;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        return [];
      }
      throw error;
    }
  }
}

function mapCustomer(person: BackendPerson, isMe: boolean): Customer {
  return {
    id: String(person.id),
    firstName: person.firstName,
    lastName: person.lastName,
    email: person.email ?? undefined,
    birthDate: person.dateOfBirth,
    heightCm: person.height,
    weightKg: person.weight,
    address: formatAddress(person.address ?? person),
    isMe,
    horseIds: [],
  };
}

function mapSattler(person: BackendPerson): Sattler {
  return {
    id: String(person.id),
    firstName: person.firstName,
    lastName: person.lastName,
    email: person.email ?? '',
    address: formatAddress(person.address ?? person),
    website: person.websiteLink ?? undefined,
    description: person.description ?? undefined,
    memberSince: '',
  };
}

function mapHorse(horse: BackendHorse, ownerId: number): Horse {
  const birthDate = horse.dateOfBirth;
  const parsedBirthDate = new Date(`${birthDate}T00:00:00`);
  const today = new Date();
  const age = Number.isNaN(parsedBirthDate.getTime())
    ? 0
    : today.getFullYear() - parsedBirthDate.getFullYear() -
      (today < new Date(today.getFullYear(), parsedBirthDate.getMonth(), parsedBirthDate.getDate()) ? 1 : 0);

  return {
    id: String(horse.id),
    name: horse.name,
    age,
    breed: horse.breedNames.join(', '),
    imageUrl: '',
    ownerId: String(ownerId),
    heightCm: horse.height,
    weightKg: horse.weight,
    sex: horse.gender === 'Female' ? 'Stute' : 'Hengst',
    birthDate: formatDate(birthDate),
    address: formatAddress(horse.address),
  };
}

function mapSaddle(saddle: BackendSaddle): Saddle {
  return {
    id: String(saddle.id),
    horseId: String(saddle.horseId),
    name: saddle.name,
    category: saddle.categoryName,
  };
}

function formatAddress(address?: Partial<BackendAddress> | Partial<BackendPerson> | null): string {
  if (!address) return '';
  const locality = [address.plz, address.cityName].filter(Boolean).join(' ');
  return [address.addressName, locality].filter(Boolean).join(', ');
}

function formatDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}

function parseAddress(value: string | undefined): {
  addressName: string | null;
  cityName: string;
  plz: string;
} | null {
  if (!value) return null;
  const match = /^(?:(.+?),\s*)?(\d{4,5})\s+(.+)$/.exec(value.trim());
  if (!match) return null;
  return {
    addressName: match[1]?.trim() || null,
    plz: match[2],
    cityName: match[3].trim(),
  };
}

function parseHorseDate(value: string | undefined): string | null {
  if (!value) return null;
  const isoDate = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (isoDate) return value;
  const localDate = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(value);
  return localDate ? `${localDate[3]}-${localDate[2]}-${localDate[1]}` : null;
}

function genderToBackend(value: Horse['sex']): BackendHorse['gender'] | null {
  if (value === 'Stute') return 'Female';
  if (value === 'Wallach' || value === 'Hengst') return 'Male';
  return null;
}

function buildPetziDetail(): MeasurementDetail {
  const segments: MeasurementSegmentResult[] = ([
    ['Schritt', 'Links'],
    ['Trab', 'Links'],
    ['Schritt', 'Rechts'],
    ['Trab', 'Rechts'],
  ] as const).map(([gait, side]) => ({
    gait,
    side,
    durationSeconds: 30,
    pressureGrid: createSegmentPressureGrid(gait, side),
  }));
  const pressureGrid = averageGrids(segments.map(segment => segment.pressureGrid));

  return {
    maxNcm2: gridMaximum(pressureGrid),
    meanNcm2: gridMean(pressureGrid),
    riderName: 'Max Mustermann',
    riderHeightM: 1.75,
    riderWeightKg: 78,
    durationFull: '09:22 min',
    saddleName: 'Prestige X-D2',
    gaits: [...new Set(segments.map(segment => segment.gait))],
    pressureGrid,
    segments,
    videos: {
      main:
        'https://images.unsplash.com/photo-1553284965-83fd3e82fa5a?auto=format&fit=crop&w=800&q=80',
      sideRight:
        'https://images.unsplash.com/photo-1534773728080-33d31da27ae5?auto=format&fit=crop&w=400&q=80',
      sideLeft:
        'https://images.unsplash.com/photo-1500595046743-cd271d694d30?auto=format&fit=crop&w=400&q=80',
    },
  };
}

function buildPetziPressureGrid(): number[][] {
  const rows = 18;
  const cols = 18;
  const grid: number[][] = [];
  for (let r = 0; r < rows; r++) {
    const row: number[] = [];
    for (let c = 0; c < cols; c++) {
      const leftBand = bell(c, 5, 1.5) * bellVertical(r, rows);
      const rightBand = bell(c, 13, 1.5) * bellVertical(r, rows);
      const topLeftHot = bell(c, 5, 1.2) * bell(r, 3, 1.3) * 2.8;
      const bottomRightHot = bell(c, 13, 1.4) * bell(r, 14, 1.8) * 2.4;

      let v = (leftBand + rightBand) * 3.4 + topLeftHot + bottomRightHot;
      const noise = ((r * 13 + c * 7) % 5) * 0.05;
      v = Math.max(0, v + noise);
      if (c >= 8 && c <= 10) {
        v *= 0.05;
      }
      row.push(Math.round(v * 10) / 10);
    }
    grid.push(row);
  }
  return grid;
}

function bell(x: number, center: number, sigma: number): number {
  const d = x - center;
  return Math.exp(-(d * d) / (2 * sigma * sigma));
}

function bellVertical(r: number, rows: number): number {
  const mid = rows / 2;
  return Math.max(0, 1 - Math.abs(r - mid) / (rows * 0.7));
}

function createSegmentPressureGrid(gait: MeasurementSegmentResult['gait'], side: MeasurementSegmentResult['side']): number[][] {
  const gaitScale = gait === 'Schritt' ? 0.75 : gait === 'Galopp' ? 1.2 : 1;
  const grid = buildPetziPressureGrid().map(row => row.map(value => value * gaitScale));
  return side === 'Rechts' ? grid.map(row => row.reverse()) : grid;
}

function averageGrids(grids: number[][][]): number[][] {
  if (grids.length === 0) return buildPetziPressureGrid();
  return grids[0].map((row, y) =>
    row.map((_, x) => grids.reduce((total, grid) => total + (grid[y]?.[x] ?? 0), 0) / grids.length),
  );
}

function gridMaximum(grid: number[][]): number {
  return Math.max(0, ...grid.flat());
}

function gridMean(grid: number[][]): number {
  const values = grid.flat();
  return values.length ? values.reduce((total, value) => total + value, 0) / values.length : 0;
}
