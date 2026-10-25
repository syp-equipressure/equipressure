import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { Customer } from '../models/customer.model';
import { Horse } from '../models/horse.model';
import { Measurement, MeasurementDetail, MeasurementSegmentResult } from '../models/measurement.model';
import { Sattler } from '../models/sattler.model';
import { MeasurementSession } from '../models/measurement-session.model';
import { Saddle } from '../models/saddle.model';
import { PressureDevice } from '../models/device.model';
import { formatDuration } from '../utils/time-format';
import { hasNoRecordedData } from './measurement-mapper';

/** Local demo and measurement-session data; backend-backed entities are cached here for the UI. */
@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly document = inject(DOCUMENT);
  private readonly devicesStorageKey = 'equipressure-sattler-devices';
  private readonly _devices = signal<PressureDevice[]>(this.readDevices());

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
      address: 'Hauptstraße 12, 1010 Wien',
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
      weightKg: 452,
      sex: 'Weiblich',
      birthDate: '12.10.2015',
      address: 'Musterstraße 1, 1111 Musterdorf',
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
      address: 'Am Reitplatz 4, 2340 Mödling',
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
      address: 'Wiesenweg 8, 5020 Salzburg',
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
      address: 'Feldgasse 21, 8010 Graz',
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
      address: 'Mühlenweg 6, 4020 Linz',
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
      address: 'Birkenstraße 3, 3100 St. Pölten',
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
      address: 'Am Sonnenhang 15, 7000 Eisenstadt',
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
  private readonly _recordedMeasurements = signal<Measurement[]>([]);

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
    // kommt der Sattler selbst aus dem Backend, ist nur noch er "ich" (nicht mehr das lokale Beispielprofil)
    const hasMe = customers.some(customer => customer.isMe);
    this._customers.update(current => {
      const byId = new Map(current.map(customer => [
        customer.id,
        hasMe && customer.isMe ? { ...customer, isMe: false } : customer,
      ]));
      for (const customer of customers) {
        const existing = byId.get(customer.id);
        byId.set(customer.id, {
          ...existing,
          ...customer,
          heightCm: customer.heightCm || existing?.heightCm || 0,
          weightKg: customer.weightKg || existing?.weightKg || 0,
          birthDate: customer.birthDate ?? existing?.birthDate,
          address: customer.address ?? existing?.address,
          phoneNumber: customer.phoneNumber ?? existing?.phoneNumber,
        });
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

  addSaddle(saddle: Saddle): void {
    this._saddles.update(list => [...list, saddle]);
  }

  addHorse(horse: Horse): void {
    this._horses.update(list => [...list, horse]);
  }

  upsertHorses(horses: Horse[]): void {
    this._horses.update(current => {
      const byId = new Map(current.map(horse => [horse.id, horse]));
      for (const horse of horses) {
        byId.set(horse.id, { ...byId.get(horse.id), ...horse });
      }
      return [...byId.values()];
    });
  }

  upsertSaddles(saddles: Saddle[]): void {
    this._saddles.update(current => {
      const byId = new Map(current.map(saddle => [saddle.id, saddle]));
      for (const saddle of saddles) {
        byId.set(saddle.id, { ...byId.get(saddle.id), ...saddle });
      }
      return [...byId.values()];
    });
  }

  updateHorse(horse: Horse): void {
    this._horses.update(list => list.map(item => item.id === horse.id ? horse : item));
  }

  removeHorse(horseId: string): void {
    this._horses.update(list => list.filter(horse => horse.id !== horseId));
    this._saddles.update(list => list.filter(saddle => saddle.horseId !== horseId));
    this._measurements.update(list => list.filter(measurement => measurement.horseId !== horseId));
    this._recordedMeasurements.update(list => list.filter(measurement => measurement.horseId !== horseId));
  }

  // ── Measurements ───────────────────────────────────────────────────────────

  readonly recordedMeasurements = this._recordedMeasurements.asReadonly();

  getMeasurementsOf(horseId: string): Measurement[] {
    return this._measurements().filter(m => m.horseId === horseId);
  }

  getMeasurement(id: string): Measurement | undefined {
    return this._measurements().find(m => m.id === id);
  }

  removeMeasurement(id: string): void {
    this._measurements.update(list => list.filter(measurement => measurement.id !== id));
    this._recordedMeasurements.update(list => list.filter(measurement => measurement.id !== id));
  }

  /**
   * Replaces the measurements of a horse with the ones loaded from the backend. The backend cannot
   * store recorded data yet, so a measurement recorded in this session keeps its local data.
   */
  setMeasurementsOf(horseId: string, measurements: Measurement[]): void {
    const recorded = new Map(this._recordedMeasurements().map(measurement => [measurement.id, measurement]));
    const merged = measurements.map(measurement =>
      hasNoRecordedData(measurement) && recorded.has(measurement.id)
        ? { ...recorded.get(measurement.id)!, date: measurement.date, notes: measurement.notes }
        : measurement,
    );
    this._measurements.update(list => [
      ...merged,
      ...list.filter(measurement => measurement.horseId !== horseId),
    ]);
  }

  getSamplePressureGrid(): number[][] {
    return buildPetziPressureGrid();
  }

  /** Adds a recorded measurement, [id] is the id of the measurement group in the backend. */
  addSessionMeasurement(session: MeasurementSession, id = `m${Date.now()}`): Measurement {
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
      id,
      horseId: session.horseId,
      date: new Date().toLocaleDateString('de-AT'),
      deviceName: saddle?.name ?? detail.saddleName,
      durationLabel: `${duration} min`,
      symmetryPct: 96,
      notes: session.notes,
      detail,
    };
    this._measurements.update(list => [measurement, ...list]);
    this._recordedMeasurements.update(list => [measurement, ...list]);
    return measurement;
  }

  updateMeasurementNotes(id: string, notes: string): void {
    this._measurements.update(list =>
      list.map(measurement => measurement.id === id ? { ...measurement, notes } : measurement),
    );
  }
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
