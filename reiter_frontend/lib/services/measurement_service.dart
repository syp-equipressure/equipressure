import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:reiterappfrontend/models/measurement.dart';

class MeasurementService {
  Future<List<Measurement>> getMeasurements() async {
    try {
      print('Versuche measurements.json zu laden...');
      final String response = await rootBundle.loadString('assets/data/measurement.json');
      print('JSON geladen, Größe: ${response.length} Zeichen');
      
      final data = json.decode(response);
      print('JSON geparst, Anzahl Messungen: ${data['measurements'].length}');
      
      List<Measurement> measurements = [];
      for (var item in data['measurements']) {
        measurements.add(Measurement.fromJson(item));
      }
      
      print('${measurements.length} Messungen erfolgreich geladen');
      return measurements;
    } catch (e) {
      print('FEHLER beim Laden der Messungen: $e');
      print('Stack trace: ${StackTrace.current}');
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
}