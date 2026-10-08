import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:reiterappfrontend/models/measurement.dart';

class MeasurementService {
  // In-Memory Storage für neue Messungen
  static final List<Measurement> _localMeasurements = [];

  Future<List<Measurement>> getMeasurements() async {
    try {
      List<Measurement> allMeasurements = [];
      
      // 1. Lade von Assets (Beispiel-Daten)
      try {
        final String assetsJson = await rootBundle.loadString('assets/data/measurement.json');
        final Map<String, dynamic> assetsData = json.decode(assetsJson) as Map<String, dynamic>;
        final List<dynamic> assetsList = assetsData['measurements'] as List<dynamic>;
        
        final assetsMeasurements = assetsList
            .map((item) => Measurement.fromJson(item as Map<String, dynamic>))
            .toList();
        
        allMeasurements.addAll(assetsMeasurements);
      } catch (e) {
        // Assets-Messungen nicht gefunden
      }
      
      // 2. Füge In-Memory Messungen hinzu
      allMeasurements.addAll(_localMeasurements);
      
      // 3. Entferne Duplikate (basierend auf ID)
      final uniqueMeasurements = <String, Measurement>{};
      for (var measurement in allMeasurements) {
        uniqueMeasurements[measurement.id] = measurement;
      }
      
      final result = uniqueMeasurements.values.toList();
      
      // 4. Sortiere nach Datum (neueste zuerst)
      result.sort((a, b) => b.date.compareTo(a.date));

      return result;
    } catch (e) {
      return [];
    }
  }

  Future<void> addMeasurement(Measurement measurement) async {
    // Füge nur zum In-Memory Storage hinzu
    _localMeasurements.add(measurement);
  }

  Future<void> updateMeasurement(Measurement updatedMeasurement) async {
    final index = _localMeasurements.indexWhere((m) => m.id == updatedMeasurement.id);

    if (index != -1) {
      _localMeasurements[index] = updatedMeasurement;
    }
  }

  Future<void> deleteMeasurement(String id) async {
    _localMeasurements.removeWhere((m) => m.id == id);
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

  // Optional: Alle In-Memory Messungen löschen (z.B. bei Logout)
  void clearLocalMeasurements() {
    _localMeasurements.clear();
  }

  // Optional: Anzahl der In-Memory Messungen abrufen
  int getLocalMeasurementsCount() {
    return _localMeasurements.length;
  }
}