import 'dart:convert';
import 'package:flutter/services.dart';
import '../models/horse.dart';

class HorseService {
  static final HorseService _instance = HorseService._internal();
  factory HorseService() => _instance;
  HorseService._internal();

  List<Horse>? _cachedHorses;

  /// Lädt Pferde aus dem JSON (später aus der Datenbank)
  Future<List<Horse>> getHorses() async {
    if (_cachedHorses != null) {
      return _cachedHorses!;
    }

    try {
      // Option 1: Aus lib/data/ laden (Mock-Daten)
      final String response = await rootBundle.loadString('assets/data/horses.json');
      
      // Option 2: Aus assets/ laden (Production-Daten)
      // final String response = await rootBundle.loadString('assets/data/horses.json');
      
      final List<dynamic> data = json.decode(response);
      _cachedHorses = data.map((json) => Horse.fromJson(json)).toList();
      return _cachedHorses!;
    } catch (e) {
      return [];
    }
  }

  /// Fügt ein neues Pferd hinzu (später zur Datenbank)
  Future<void> addHorse(Horse horse) async {
    _cachedHorses ??= [];
    _cachedHorses!.add(horse);
    // TODO: Später hier zur Datenbank hinzufügen
  }

  /// Aktualisiert ein Pferd (später in der Datenbank)
  Future<void> updateHorse(Horse horse) async {
    if (_cachedHorses == null) return;
    
    final index = _cachedHorses!.indexWhere((h) => h.id == horse.id);
    if (index != -1) {
      _cachedHorses![index] = horse;
      // TODO: Später hier in der Datenbank aktualisieren
    }
  }

  /// Löscht ein Pferd (später aus der Datenbank)
  Future<void> deleteHorse(String id) async {
    if (_cachedHorses == null) return;
    
    _cachedHorses!.removeWhere((h) => h.id == id);
    // TODO: Später hier aus der Datenbank löschen
  }

  /// Cache leeren (z.B. beim Logout)
  void clearCache() {
    _cachedHorses = null;
  }
}