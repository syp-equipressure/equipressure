import 'dart:convert';

import 'package:reiterappfrontend/services/measurement_service.dart';

/// Baut aus den Bluetooth-Daten des Messgeräts Aufnahmen für die Heatmap.
///
/// Das Gerät schickt wie beim Backend (MeasurementDataBluetoothHandler) pro Zeile eine Zahl oder ein
/// JSON-Array in mbar. Eine Bluetooth-Nachricht trägt nur wenige Bytes, längere Zeilen kommen in
/// mehreren Teilen an.
class DeviceFrameParser {
  static const int _cells =
      MeasurementService.heatmapGridSize * MeasurementService.heatmapGridSize;
  static const int _maxPendingLength = 64 * 1024;

  String _pending = '';

  /// Nimmt die nächsten Bytes entgegen und liefert alle Aufnahmen, die damit vollständig sind.
  List<List<double>> add(List<int> bytes) {
    _pending += utf8.decode(bytes, allowMalformed: true);

    final frames = <List<double>>[];
    var end = _pending.indexOf('\n');
    while (end >= 0) {
      final frame = _parseLine(_pending.substring(0, end).trim());
      _pending = _pending.substring(end + 1);
      if (frame != null) frames.add(frame);
      end = _pending.indexOf('\n');
    }

    if (_pending.length > _maxPendingLength) _pending = '';
    return frames;
  }

  /// Vergisst eine angefangene Zeile, z.B. nach einem Verbindungsabbruch.
  void reset() => _pending = '';

  static List<double>? _parseLine(String line) {
    if (line.isEmpty) return null;

    final List<double> values;
    try {
      values = MeasurementService.parseDataValues(line);
    } on FormatException {
      return null;
    }
    if (values.isEmpty || values.length > _cells) return null;
    return MeasurementService.toHeatmapGrid(values);
  }
}
