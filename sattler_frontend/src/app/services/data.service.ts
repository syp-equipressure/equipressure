import { Injectable } from '@angular/core';
import { Customer } from '../models/customer.model';
import { Horse } from '../models/horse.model';
import { Measurement, MeasurementDetail } from '../models/measurement.model';

/**
 * Mock data service. Once backend ships GET /api/persons + UserController,
 * swap this for an HttpClient-based version.
 */
@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly _customers: Customer[] = [
    {
      id: 'me',
      firstName: 'Sophie',
      lastName: 'Grüneis',
      email: 'sophie.grueneis@example.com',
      isMe: true,
      horseIds: ['my-1', 'my-2'],
    },
    {
      id: 'max',
      firstName: 'Max',
      lastName: 'Mustermann',
      email: 'max@example.com',
      horseIds: ['kas', 'petzi', 'safira', 'bella', 'mira', 'isa'],
    },
    {
      id: 'anna',
      firstName: 'Anna',
      lastName: 'Nass',
      email: 'anna.nass@example.com',
      horseIds: [],
    },
    {
      id: 'flora',
      firstName: 'Flora',
      lastName: 'Fauna',
      email: 'flora@example.com',
      horseIds: [],
    },
    {
      id: 'kathy',
      firstName: 'Kathy',
      lastName: 'Rattenburg',
      email: 'kathy@example.com',
      horseIds: [],
    },
    {
      id: 'kai',
      firstName: 'Kai',
      lastName: 'Huber',
      email: 'kai@example.com',
      horseIds: [],
    },
  ];

  private readonly _horses: Horse[] = [
    {
      id: 'kas',
      name: 'Kas',
      age: 18,
      breed: 'Englisches Vollblut',
      ownerId: 'max',
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRhcJ4yrtCZcwZc1TLgAuVvcN_P_nOTSFNroA&s',
    },
    {
      id: 'petzi',
      name: 'Petzi',
      age: 10,
      breed: 'Haflinger',
      ownerId: 'max',
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSWt6wuJmZQE2BDls6b8qBH9tZn53jbqvx2xg&s',
    },
    {
      id: 'safira',
      name: 'Safira',
      age: 13,
      breed: 'KWPN',
      ownerId: 'max',
      imageUrl:
        'https://www.landtiere.de/assets/images/34/738/34738694-haflinger-pferd-feld-gelb-fell-langhaar-2o4uhwbOmce9.jpg',
    },
    {
      id: 'bella',
      name: 'Bella',
      age: 8,
      breed: 'Isländer',
      ownerId: 'max',
      imageUrl:
        'https://www.peta.de/wp-content/uploads/2020/11/horse-721136_1920-1024x682.jpg',
    },
    {
      id: 'mira',
      name: 'Mira',
      age: 18,
      breed: 'Englisches Vollblut',
      ownerId: 'max',
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRBLATKZfxrR8DGubqewwirncrMtwXZzF02sQ&s',
    },
    {
      id: 'isa',
      name: 'Isa',
      age: 18,
      breed: 'Englisches Vollblut',
      ownerId: 'max',
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSkqcEdRShL_kALpMAucGhIcsUs7yABkEZpng&s',
    },
    // Sophie's own
    {
      id: 'my-1',
      name: 'Luna',
      age: 6,
      breed: 'Hannoveraner',
      ownerId: 'me',
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSSw0pJRnJEQ5mLbJk_DsEoMDyGIdkVhxua3w&s',
    },
    {
      id: 'my-2',
      name: 'Stella',
      age: 1,
      breed: 'Trakehner',
      ownerId: 'me',
      imageUrl:
        'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ30aD8kRv-jJmBgc6toXFbMnbpogQFtagj4w&s',
    },
  ];

  // ─── Measurements ────────────────────────────────────────────────────────
  private readonly _measurements: Measurement[] = [
    {
      id: 'm1',
      horseId: 'petzi',
      date: '24.06.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:22 min',
      symmetryPct: 96,
      detail: buildPetziDetail(),
    },
    {
      id: 'm2',
      horseId: 'petzi',
      date: '17.01.2025',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:22 min',
      symmetryPct: 96,
      detail: buildPetziDetail(),
    },
    {
      id: 'm3',
      horseId: 'petzi',
      date: '23.11.2024',
      deviceName: 'Prestige X-D2',
      durationLabel: '01:22 min',
      symmetryPct: 96,
      detail: buildPetziDetail(),
    },
  ];

  getCustomers(): Customer[] {
    return [...this._customers];
  }

  getCustomer(id: string): Customer | undefined {
    return this._customers.find(c => c.id === id);
  }

  getMe(): Customer {
    return this._customers.find(c => c.isMe)!;
  }

  getHorsesOf(customerId: string): Horse[] {
    return this._horses.filter(h => h.ownerId === customerId);
  }

  getHorse(id: string): Horse | undefined {
    return this._horses.find(h => h.id === id);
  }

  getMeasurementsOf(horseId: string): Measurement[] {
    return this._measurements.filter(m => m.horseId === horseId);
  }

  getMeasurement(id: string): Measurement | undefined {
    return this._measurements.find(m => m.id === id);
  }
}

// ─── Helpers ───────────────────────────────────────────────────────────────

function buildPetziDetail(): MeasurementDetail {
  return {
    maxNcm2: 5.8,
    meanNcm2: 2.1,
    riderName: 'Max Mustermann',
    riderHeightM: 1.75,
    riderWeightKg: 78,
    durationFull: '09:22 min',
    saddleName: 'Prestige X-D2',
    gaits: ['Schritt', 'Trab', 'Galopp'],
    pressureGrid: buildPetziPressureGrid(),
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

/**
 * 18 rows × 18 cols pressure grid that produces the heatmap from the screenshot:
 * two hot bands (left+right of the spine), one bright hotspot in the upper-left,
 * one hotspot in the lower-right.
 */
function buildPetziPressureGrid(): number[][] {
  const rows = 18;
  const cols = 18;
  const grid: number[][] = [];
  for (let r = 0; r < rows; r++) {
    const row: number[] = [];
    for (let c = 0; c < cols; c++) {
      // Two vertical pressure bands centered around col=5 and col=13.
      const leftBand = bell(c, 5, 1.5) * bellVertical(r, rows);
      const rightBand = bell(c, 13, 1.5) * bellVertical(r, rows);
      // Hotspots
      const topLeftHot = bell(c, 5, 1.2) * bell(r, 3, 1.3) * 2.8;
      const bottomRightHot = bell(c, 13, 1.4) * bell(r, 14, 1.8) * 2.4;

      let v = (leftBand + rightBand) * 3.4 + topLeftHot + bottomRightHot;
      // Floor + slight noise so the grid looks natural.
      const noise = ((r * 13 + c * 7) % 5) * 0.05;
      v = Math.max(0, v + noise);
      // Spine gutter (no pressure exactly on center)
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
  // Pressure profile along the spine: smooth peak around the middle, tapered at the ends.
  const mid = rows / 2;
  return Math.max(0, 1 - Math.abs(r - mid) / (rows * 0.7));
}
