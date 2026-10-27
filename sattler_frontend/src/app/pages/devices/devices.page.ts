import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PressureDevice } from '../../models/device.model';
import { DataService } from '../../services/data.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header';
import { BackendApiService, DEVELOPMENT_DEVICE_CATEGORIES } from '../../services/backend-api.service';
import { BackendPersonContextService } from '../../services/backend-person-context.service';

@Component({
  selector: 'app-devices-page',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  templateUrl: './devices.page.html',
  styleUrl: './devices.page.scss',
})
export class DevicesPage {
  private readonly data = inject(DataService);
  private readonly api = inject(BackendApiService);
  private readonly personContext = inject(BackendPersonContextService);

  readonly backendDevices = signal<PressureDevice[] | null>(null);
  readonly backendLoading = signal(false);
  readonly backendError = signal('');
  readonly backendPersonId = this.personContext.personId;
  readonly devices = computed(() => this.backendDevices() ?? this.data.devices());
  readonly formOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly name = signal('');
  readonly number = signal('');
  readonly error = signal('');
  readonly saveError = signal('');
  readonly saving = signal(false);
  readonly categories = DEVELOPMENT_DEVICE_CATEGORIES;
  readonly categoryId = signal<number>(DEVELOPMENT_DEVICE_CATEGORIES[0].id);

  constructor() {
    effect(onCleanup => {
      const personId = this.backendPersonId();
      this.backendError.set('');
      if (!personId) {
        this.backendDevices.set(null);
        this.backendLoading.set(false);
        return;
      }
      this.backendLoading.set(true);
      const subscription = this.api.getDevices(personId).subscribe({
        next: devices => {
          this.backendDevices.set(devices);
          this.backendLoading.set(false);
        },
        error: error => {
          this.backendDevices.set([]);
          this.backendError.set(error instanceof Error ? error.message : 'Messgeräte konnten nicht geladen werden.');
          this.backendLoading.set(false);
        },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  openAddForm(): void {
    this.editingId.set(null);
    this.name.set('');
    this.number.set('');
    this.categoryId.set(DEVELOPMENT_DEVICE_CATEGORIES[0].id);
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
    const personId = this.backendPersonId();
    if (personId) {
      this.saveBackendDevice(personId);
      return;
    }
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

  /** Das Backend kann Geräte nur anlegen (Seriennummer + Kategorie), nicht bearbeiten oder löschen. */
  private saveBackendDevice(personId: string): void {
    const number = this.number().trim();
    if (!number) {
      this.error.set('Bitte die Gerätenummer eingeben.');
      return;
    }
    if (this.devices().some(device => device.number.toLocaleLowerCase('de') === number.toLocaleLowerCase('de'))) {
      this.error.set('Diese Gerätenummer ist bereits eingetragen.');
      return;
    }

    this.saving.set(true);
    this.api.createDevice(number, personId, this.categoryId()).subscribe({
      next: device => {
        this.backendDevices.update(devices => [...(devices ?? []), device]);
        this.saving.set(false);
        this.formOpen.set(false);
      },
      error: error => {
        this.saving.set(false);
        this.error.set(error instanceof Error ? error.message : 'Das Gerät konnte nicht gespeichert werden.');
      },
    });
  }

  deleteDevice(device: PressureDevice): void {
    if (this.backendPersonId()) {
      this.saveError.set('Das Backend unterstützt derzeit kein Löschen von Geräten.');
      return;
    }
    if (!window.confirm(`Möchtest du das Gerät „${device.name}“ wirklich löschen?`)) return;
    try {
      this.data.deleteDevice(device.id);
      this.saveError.set('');
    } catch {
      this.saveError.set('Das Gerät konnte nicht gelöscht werden.');
    }
  }
}
