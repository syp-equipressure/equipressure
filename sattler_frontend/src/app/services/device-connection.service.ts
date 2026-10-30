import { Injectable, signal } from '@angular/core';
import { PressureDevice } from '../models/device.model';
import { DeviceFrameParser } from './device-frame-parser';

/** The identifiers of the device, the same as in the backend (BluetoothOptions). */
const SERVICE_UUID = '12345678-1234-1234-1234-1234567890ab';
const CHARACTERISTIC_UUID = 'abcd1234-5678-90ab-cdef-1234567890ab';
/** The Bluetooth name of the device starts with this, e.g. EquiPressure_Dummy. */
const NAME_PREFIX = 'EquiPressure';

export type DeviceConnectionStatus = 'disconnected' | 'connecting' | 'connected';

/**
 * Keeps the Bluetooth LE connection to the measurement device over Web Bluetooth, which only exists in
 * Chrome and Edge and needs HTTPS or localhost.
 */
@Injectable({ providedIn: 'root' })
export class DeviceConnectionService {
  private readonly parser = new DeviceFrameParser();
  private bluetoothDevice: BluetoothDevice | null = null;
  private characteristic: BluetoothRemoteGATTCharacteristic | null = null;
  private decoder = new TextDecoder();

  readonly status = signal<DeviceConnectionStatus>('disconnected');
  /** Id of the connected device, or of the device that is being connected. */
  readonly deviceId = signal<string | null>(null);
  /** The newest frame of the device (20x20 values in N/cm²). */
  readonly frame = signal<number[][] | null>(null);
  readonly error = signal('');

  isConnectedTo(device: PressureDevice): boolean {
    return this.status() === 'connected' && this.deviceId() === device.id;
  }

  isConnectingTo(device: PressureDevice): boolean {
    return this.status() === 'connecting' && this.deviceId() === device.id;
  }

  /** Has to be called from a click, the browser only shows its device selection after a user action. */
  async connect(device: PressureDevice): Promise<void> {
    this.disconnect();
    this.error.set('');

    if (!navigator.bluetooth) {
      this.error.set('Dein Browser unterstützt kein Bluetooth. Bitte verwende Chrome oder Edge.');
      return;
    }

    this.deviceId.set(device.id);
    this.status.set('connecting');
    try {
      const bluetoothDevice = await navigator.bluetooth.requestDevice({
        filters: [{ namePrefix: NAME_PREFIX }, { services: [SERVICE_UUID] }],
        optionalServices: [SERVICE_UUID],
      });
      bluetoothDevice.addEventListener('gattserverdisconnected', this.onDisconnected);
      this.bluetoothDevice = bluetoothDevice;

      const server = await bluetoothDevice.gatt?.connect();
      if (!server) throw new Error('No GATT server');
      const service = await server.getPrimaryService(SERVICE_UUID);
      const characteristic = await service.getCharacteristic(CHARACTERISTIC_UUID);

      this.parser.reset();
      this.decoder = new TextDecoder();
      characteristic.addEventListener('characteristicvaluechanged', this.onData);
      await characteristic.startNotifications();
      this.characteristic = characteristic;
      this.status.set('connected');
    } catch (error) {
      this.disconnect();
      // NotFoundError: the selection was closed without choosing a device
      if (!(error instanceof DOMException && error.name === 'NotFoundError')) {
        this.error.set('Die Verbindung zum Messgerät ist fehlgeschlagen.');
      }
    }
  }

  disconnect(): void {
    this.characteristic?.removeEventListener('characteristicvaluechanged', this.onData);
    this.characteristic = null;
    this.bluetoothDevice?.removeEventListener('gattserverdisconnected', this.onDisconnected);
    this.bluetoothDevice?.gatt?.disconnect();
    this.bluetoothDevice = null;
    this.frame.set(null);
    this.deviceId.set(null);
    this.status.set('disconnected');
  }

  private readonly onData = (event: Event): void => {
    const value = (event.target as BluetoothRemoteGATTCharacteristic).value;
    if (!value) return;

    const frames = this.parser.add(this.decoder.decode(value, { stream: true }));
    const newest = frames.at(-1);
    if (newest) this.frame.set(newest);
  };

  private readonly onDisconnected = (): void => {
    this.disconnect();
    this.error.set('Die Verbindung zum Messgerät wurde getrennt.');
  };
}
