import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:reiterappfrontend/models/measurement.dart';

class MeasurementService {
  Future<List<Measurement>> getMeasurements() async {
  try {
    final String response =
        await rootBundle.loadString('assets/data/measurement.json');

    final Map<String, dynamic> data =
        json.decode(response) as Map<String, dynamic>;

    final List<dynamic> list =
        data['measurements'] as List<dynamic>;

    final measurements = list
        .map((item) =>
            Measurement.fromJson(item as Map<String, dynamic>))
        .toList();

    return measurements;
  } catch (e, s) {
    print('FEHLER beim Laden der Messungen: $e');
    print(s);
    return [];
  }
}


  Future<List<Measurement>> getMeasurementsForHorse(String horseId) async {
    final allMeasurements = await getMeasurements();
    return allMeasurements.where((m) => m.horseId == horseId).toList();
  }

  Future<Measurement?> getMeasurementById(String id) async {
    final allMeasurements = await getMeasurements();
    try {
      return allMeasurements.firstWhere((m) => m.id == id);
    } catch (e) {
      return null;
    }
  }

    Future<void> deleteMeasurement(String id) async {
    try {
      final measurements = await getMeasurements();
      measurements.removeWhere((m) => m.id == id);
      print('Messung $id erfolgreich gelöscht');
    } catch (e) {
      print('Fehler beim Löschen der Messung: $e');
      rethrow;
    }


  }
}