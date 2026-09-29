import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:reiterappfrontend/models/saddle.dart';

class SaddleService {
  static final SaddleService _instance = SaddleService._internal();
  factory SaddleService() => _instance;
  SaddleService._internal();

  List<Saddle>? _cachedSaddles;

  /// Lädt Sättel aus dem JSON (später aus der Datenbank)
  Future<List<Saddle>> getSaddles() async {
    if (_cachedSaddles != null) {
      return _cachedSaddles!;
    }

    try {
      final String response = await rootBundle.loadString('assets/data/saddle.json');
      final List<dynamic> data = json.decode(response);
      _cachedSaddles = data.map((json) => Saddle.fromJson(json)).toList();
      return _cachedSaddles!;
    } catch (e) {
      return [];
    }
  }

  /// Fügt einen neuen Sattel hinzu (später zur Datenbank)
  Future<void> addSaddle(Saddle saddle) async {
    _cachedSaddles ??= [];
    _cachedSaddles!.add(saddle);
    // TODO: Später hier zur Datenbank hinzufügen
  }

  /// Aktualisiert einen Sattel (später in der Datenbank)
  Future<void> updateSaddle(Saddle saddle) async {
    if (_cachedSaddles == null) return;
    
    final index = _cachedSaddles!.indexWhere((s) => s.id == saddle.id);
    if (index != -1) {
      _cachedSaddles![index] = saddle;
      // TODO: Später hier in der Datenbank aktualisieren
    }
  }

  /// Löscht einen Sattel (später aus der Datenbank)
  Future<void> deleteSaddle(String id) async {
    if (_cachedSaddles == null) return;
    
    _cachedSaddles!.removeWhere((s) => s.id == id);
    // TODO: Später hier aus der Datenbank löschen
  }

  /// Cache leeren (z.B. beim Logout)
  void clearCache() {
    _cachedSaddles = null;
  }
}