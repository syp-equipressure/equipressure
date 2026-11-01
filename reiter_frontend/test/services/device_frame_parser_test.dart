import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:reiterappfrontend/services/device_frame_parser.dart';

void main() {
  final fullFrame = jsonEncode(List<double>.generate(400, (i) => i.toDouble()));

  group('DeviceFrameParser', () {
    test('setzt eine in Teilen empfangene Aufnahme zusammen', () {
      final parser = DeviceFrameParser();

      expect(parser.add(utf8.encode(fullFrame.substring(0, 100))), isEmpty);
      final frames = parser.add(utf8.encode('${fullFrame.substring(100)}\n'));

      expect(frames, hasLength(1));
      expect(frames.single, hasLength(400));
      expect(frames.single[399], 399);
    });

    test('liefert mehrere Aufnahmen aus einer Nachricht', () {
      final parser = DeviceFrameParser();

      expect(parser.add(utf8.encode('$fullFrame\r\n$fullFrame\n')), hasLength(2));
    });

    test('bringt kurze Werte auf das Heatmap-Raster', () {
      final parser = DeviceFrameParser();

      final frames = parser.add(utf8.encode('[12.5, 13.1, 14.8]\n12.5\n'));

      expect(frames, hasLength(2));
      expect(frames.every((frame) => frame.length == 400), isTrue);
    });

    test('überspringt ungültige Zeilen', () {
      final parser = DeviceFrameParser();

      expect(parser.add(utf8.encode('kaputt\n[1, 2\n\n')), isEmpty);
    });

    test('verwirft nach reset eine angefangene Zeile', () {
      final parser = DeviceFrameParser();
      parser.add(utf8.encode('[1, 2'));

      parser.reset();

      expect(parser.add(utf8.encode('5\n')).single, hasLength(400));
    });
  });
}
