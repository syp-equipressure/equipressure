import 'package:flutter/foundation.dart';
import 'package:reiterappfrontend/models/device.dart';

enum DeviceConnectionStatus { disconnected, connecting, connected }

/// Hält die Verbindung zum Satteldruckmessgerät, damit alle Screens denselben
/// Status sehen (z.B. "Neue Messung" und "Meine Satteldruckmessgeräte").
///
/// Die Verbindung ist vorerst simuliert, wie im Sattler-Frontend. Sobald klar ist,
/// wie das Gerät angebunden wird (Bluetooth/WLAN), kommt die echte Verbindung in
/// [connect] und [disconnect].
class DeviceConnectionService extends ChangeNotifier {
  static const Duration _simulatedConnectDelay = Duration(milliseconds: 1500);

  static final DeviceConnectionService _instance = DeviceConnectionService._internal();
  factory DeviceConnectionService() => _instance;
  DeviceConnectionService._internal();

  Device? _device;
  DeviceConnectionStatus _status = DeviceConnectionStatus.disconnected;

  /// Das verbundene Gerät bzw. das Gerät, zu dem gerade verbunden wird.
  Device? get device => _device;
  DeviceConnectionStatus get status => _status;

  bool isConnectedTo(Device device) =>
      _status == DeviceConnectionStatus.connected && _device?.id == device.id;

  bool isConnectingTo(Device device) =>
      _status == DeviceConnectionStatus.connecting && _device?.id == device.id;

  /// Verbindet mit [device], eine bestehende Verbindung zu einem anderen Gerät wird getrennt.
  Future<void> connect(Device device) async {
    if (isConnectedTo(device) || isConnectingTo(device)) return;

    _device = device;
    _status = DeviceConnectionStatus.connecting;
    notifyListeners();

    await Future.delayed(_simulatedConnectDelay);

    // in der Zwischenzeit kann getrennt oder ein anderes Gerät gewählt worden sein
    if (_device?.id != device.id || _status != DeviceConnectionStatus.connecting) return;
    _status = DeviceConnectionStatus.connected;
    notifyListeners();
  }

  void disconnect() {
    if (_status == DeviceConnectionStatus.disconnected) return;
    _device = null;
    _status = DeviceConnectionStatus.disconnected;
    notifyListeners();
  }
}
