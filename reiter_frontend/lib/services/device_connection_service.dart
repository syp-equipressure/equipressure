import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/services/device_frame_parser.dart';

enum DeviceConnectionStatus { disconnected, connecting, connected }

/// Ein Problem beim Verbinden, das der Nutzer lesen und beheben kann.
class _ConnectionProblem implements Exception {
  final String message;

  _ConnectionProblem(this.message);
}

/// Hält die Bluetooth-LE-Verbindung zum Satteldruckmessgerät, damit alle Screens denselben
/// Status sehen (z.B. "Neue Messung" und "Meine Satteldruckmessgeräte").
///
/// Die Kennungen des Geräts sind dieselben wie im Backend (BluetoothOptions). Auf Geräten ohne
/// Bluetooth (Emulator) ist die Verbindung beim Entwickeln simuliert, siehe [isSimulated].
class DeviceConnectionService extends ChangeNotifier {
  static final Guid _serviceUuid = Guid('12345678-1234-1234-1234-1234567890ab');
  static final Guid _characteristicUuid = Guid('abcd1234-5678-90ab-cdef-1234567890ab');

  /// Damit beginnt der Bluetooth-Name des Geräts, z.B. EquiPressure_Dummy
  static const String _namePrefix = 'EquiPressure';
  static const Duration _scanTimeout = Duration(seconds: 10);
  static const Duration _connectTimeout = Duration(seconds: 15);
  static const Duration _simulatedConnectDelay = Duration(milliseconds: 1500);

  static final DeviceConnectionService _instance = DeviceConnectionService._internal();
  factory DeviceConnectionService() => _instance;
  DeviceConnectionService._internal();

  final StreamController<List<double>> _frames = StreamController.broadcast();
  final DeviceFrameParser _parser = DeviceFrameParser();

  Device? _device;
  DeviceConnectionStatus _status = DeviceConnectionStatus.disconnected;
  bool _simulated = false;
  String? _error;
  String? _errorDeviceId;
  BluetoothDevice? _bluetoothDevice;
  StreamSubscription<List<int>>? _dataSubscription;
  StreamSubscription<BluetoothConnectionState>? _stateSubscription;

  /// Wird bei jedem Verbinden und Trennen erhöht, damit ein älterer Versuch abbricht
  int _attempt = 0;

  /// Das verbundene Gerät bzw. das Gerät, zu dem gerade verbunden wird.
  Device? get device => _device;
  DeviceConnectionStatus get status => _status;

  /// True wenn kein echtes Gerät verbunden ist, dann kommen auch keine echten Messwerte.
  bool get isSimulated => _simulated;

  /// Die Aufnahmen des Geräts (20x20 Werte in mbar), solange es verbunden ist.
  Stream<List<double>> get frames => _frames.stream;

  bool isConnectedTo(Device device) =>
      _status == DeviceConnectionStatus.connected && _device?.id == device.id;

  bool isConnectingTo(Device device) =>
      _status == DeviceConnectionStatus.connecting && _device?.id == device.id;

  /// Warum das Verbinden mit [device] zuletzt nicht geklappt hat.
  String? errorFor(Device device) => _errorDeviceId == device.id ? _error : null;

  /// Verbindet mit [device], eine bestehende Verbindung zu einem anderen Gerät wird getrennt.
  Future<void> connect(Device device) async {
    if (isConnectedTo(device) || isConnectingTo(device)) return;
    await disconnect();

    final attempt = ++_attempt;
    _device = device;
    _status = DeviceConnectionStatus.connecting;
    _error = null;
    _errorDeviceId = null;
    notifyListeners();

    try {
      if (!await FlutterBluePlus.isSupported) {
        if (!kDebugMode) {
          throw _ConnectionProblem('Dieses Gerät unterstützt kein Bluetooth.');
        }
        await Future.delayed(_simulatedConnectDelay);
        if (attempt == _attempt) _setConnected(simulated: true);
        return;
      }

      await _ensureBluetoothIsOn();
      if (attempt != _attempt) return;
      final found = await _findDevice(device);
      if (attempt != _attempt) return;
      await _open(found, attempt);
    } on _ConnectionProblem catch (problem) {
      if (attempt == _attempt) await _fail(device, problem.message);
    } catch (_) {
      if (attempt == _attempt) {
        await _fail(device, 'Die Verbindung zum Messgerät ist fehlgeschlagen.');
      }
    }
  }

  Future<void> disconnect() async {
    _attempt++;
    final wasActive = _status != DeviceConnectionStatus.disconnected;
    await _close();
    _device = null;
    _status = DeviceConnectionStatus.disconnected;
    _simulated = false;
    if (wasActive) notifyListeners();
  }

  Future<void> _ensureBluetoothIsOn() async {
    final state = await FlutterBluePlus.adapterState
        .where((state) => state != BluetoothAdapterState.unknown)
        .first
        .timeout(const Duration(seconds: 3), onTimeout: () => BluetoothAdapterState.unknown);
    if (state == BluetoothAdapterState.on) return;

    if (defaultTargetPlatform == TargetPlatform.android) {
      try {
        await FlutterBluePlus.turnOn();
        return;
      } on FlutterBluePlusException {
        // abgelehnt, dann gilt der Hinweis unten
      }
    }
    throw _ConnectionProblem('Bitte schalte Bluetooth ein.');
  }

  /// Sucht ein Messgerät in der Nähe, das mit der Gerätenummer im Namen wird bevorzugt.
  Future<BluetoothDevice> _findDevice(Device device) async {
    final found = Completer<BluetoothDevice>();
    final subscription = FlutterBluePlus.onScanResults.listen((results) {
      if (results.isEmpty || found.isCompleted) return;
      final match = results.where((r) => r.advertisementData.advName.contains(device.number));
      found.complete((match.isNotEmpty ? match.first : results.first).device);
    });

    try {
      await FlutterBluePlus.startScan(withKeywords: const [_namePrefix], timeout: _scanTimeout);
      final scanEnded = FlutterBluePlus.isScanning.where((scanning) => !scanning).first;
      await Future.any([found.future, scanEnded]);
    } finally {
      await subscription.cancel();
      await FlutterBluePlus.stopScan();
    }

    if (!found.isCompleted) {
      throw _ConnectionProblem('Kein Messgerät gefunden. Ist es eingeschaltet und in der Nähe?');
    }
    return found.future;
  }

  Future<void> _open(BluetoothDevice bluetoothDevice, int attempt) async {
    _bluetoothDevice = bluetoothDevice;
    // nicht gewinnorientierte Nutzung, die Lizenz gilt für Bildungseinrichtungen
    await bluetoothDevice.connect(license: License.nonprofit, timeout: _connectTimeout);
    if (attempt != _attempt) return;

    final services = await bluetoothDevice.discoverServices();
    if (attempt != _attempt) return;
    final characteristic = services
        .where((service) => service.uuid == _serviceUuid)
        .expand((service) => service.characteristics)
        .where((characteristic) => characteristic.uuid == _characteristicUuid)
        .firstOrNull;
    if (characteristic == null) {
      throw _ConnectionProblem('Das gefundene Gerät ist kein EquiPressure-Messgerät.');
    }

    _parser.reset();
    _dataSubscription = characteristic.onValueReceived.listen((bytes) {
      _parser.add(bytes).forEach(_frames.add);
    });
    _stateSubscription = bluetoothDevice.connectionState.listen((state) {
      if (state == BluetoothConnectionState.disconnected && attempt == _attempt) {
        final device = _device;
        if (device != null) _fail(device, 'Die Verbindung zum Messgerät wurde getrennt.');
      }
    });
    await characteristic.setNotifyValue(true);

    if (attempt == _attempt) _setConnected(simulated: false);
  }

  void _setConnected({required bool simulated}) {
    _simulated = simulated;
    _status = DeviceConnectionStatus.connected;
    notifyListeners();
  }

  Future<void> _fail(Device device, String message) async {
    _attempt++;
    await _close();
    _device = null;
    _status = DeviceConnectionStatus.disconnected;
    _simulated = false;
    _error = message;
    _errorDeviceId = device.id;
    notifyListeners();
  }

  Future<void> _close() async {
    await _dataSubscription?.cancel();
    await _stateSubscription?.cancel();
    _dataSubscription = null;
    _stateSubscription = null;

    final bluetoothDevice = _bluetoothDevice;
    _bluetoothDevice = null;
    try {
      await bluetoothDevice?.disconnect();
    } on FlutterBluePlusException {
      // war schon getrennt
    }
  }
}
