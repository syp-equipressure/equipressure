import 'package:flutter_test/flutter_test.dart';
import 'package:reiterappfrontend/models/horse.dart';

void main() {
  group('Horse.splitStreet', () {
    test('trennt die Hausnummer von der Straße', () {
      final result = Horse.splitStreet('Stallweg 3');

      expect(result.street, 'Stallweg');
      expect(result.houseNumber, '3');
    });

    test('versteht Hausnummern mit Buchstaben und Stiegen', () {
      expect(Horse.splitStreet('Am Reiterhof 12a').houseNumber, '12a');
      expect(Horse.splitStreet('Linzer Straße 5/2').houseNumber, '5/2');
    });

    test('lässt eine Adresse ohne Hausnummer unverändert', () {
      final result = Horse.splitStreet('Reiterhof');

      expect(result.street, 'Reiterhof');
      expect(result.houseNumber, isNull);
    });
  });
}
