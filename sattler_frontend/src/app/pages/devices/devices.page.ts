import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PressureDevice } from '../../models/device.model';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';

@Component({
  selector: 'app-devices-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './devices.page.html',
  styleUrl: './devices.page.scss',
})
export class DevicesPage {
  private readonly data = inject(DataService);

  readonly devices = this.data.devices;
  readonly formOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly name = signal('');
  readonly number = signal('');
  readonly error = signal('');
  readonly saveError = signal('');

  openAddForm(): void {
    this.editingId.set(null);
    this.name.set('');
    this.number.set('');
    this.error.set('');
    this.saveError.set('');
    this.formOpen.set(true);
  }

  editDevice(device: PressureDevice): void {
    this.editingId.set(device.id);
    this.name.set(device.name);
    this.number.set(device.number);
    this.error.set('');
    this.saveError.set('');
    this.formOpen.set(true);
  }

  cancelForm(): void {
    this.formOpen.set(false);
    this.error.set('');
  }

  saveDevice(): void {
    const name = this.name().trim();
    const number = this.number().trim();
    if (!name || !number) {
      this.error.set('Bitte Gerätename und Gerätenummer eingeben.');
      return;
    }

    const existing = this.devices().find(device =>
      device.number.toLocaleLowerCase('de') === number.toLocaleLowerCase('de') &&
      device.id !== this.editingId(),
    );
    if (existing) {
      this.error.set('Diese Gerätenummer ist bereits eingetragen.');
      return;
    }

    const device: PressureDevice = {
      id: this.editingId() ?? crypto.randomUUID(),
      name,
      number,
    };
    try {
      if (this.editingId()) {
        this.data.updateDevice(device);
      } else {
        this.data.addDevice(device);
      }
      this.formOpen.set(false);
      this.saveError.set('');
    } catch {
      this.saveError.set('Das Gerät konnte nicht gespeichert werden. Bitte prüfe den verfügbaren Speicherplatz.');
    }
  }

  deleteDevice(device: PressureDevice): void {
    if (!window.confirm(`Möchtest du das Gerät „${device.name}“ wirklich löschen?`)) return;
    try {
      this.data.deleteDevice(device.id);
      this.saveError.set('');
    } catch {
      this.saveError.set('Das Gerät konnte nicht gelöscht werden.');
    }
  }
}
