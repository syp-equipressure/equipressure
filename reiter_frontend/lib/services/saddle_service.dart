import 'dart:convert';
import 'package:reiterappfrontend/services/api_client.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/horse_service.dart';

/// Ein Sattel, mit dem schon gemessen wurde, kann nicht gelöscht werden.
class SaddleInUseException implements Exception {
  @override
  String toString() =>
      'Mit dem Sattel wurde schon gemessen. Lösche zuerst die Messungen.';
}

class SaddleService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 5);

  static final SaddleService _instance = SaddleService._internal();
  factory SaddleService() => _instance;
  SaddleService._internal();

  List<Saddle>? _cachedSaddles;
  int? _cachedPersonId;
  List<SaddleCategory>? _cachedCategories;

  /// Lädt die Sättel der Pferde des aktuellen Reiters aus dem Backend.
  Future<List<Saddle>> getSaddles() async {
    final personId = await HorseService().currentPersonId();
    if (_cachedSaddles != null && _cachedPersonId == personId) {
      return _cachedSaddles!;
    }

    final horses = await HorseService().getHorses();
    final saddlesByHorse = await Future.wait(
      horses.map((horse) => _fetchSaddlesForHorse(horse.id)),
    );
    _cachedSaddles = saddlesByHorse.expand((saddles) => saddles).toList();
    _cachedPersonId = personId;
    return _cachedSaddles!;
  }

  Future<List<Saddle>> _fetchSaddlesForHorse(String horseId) async {
    final url = Uri.parse('$baseUrl/horses/$horseId/saddles');
    final response = await apiHttp.get(url).timeout(_timeout);
    if (response.statusCode == 404) return const [];
    if (response.statusCode != 200) {
      throw Exception('Failed to load saddles of horse $horseId: HTTP ${response.statusCode}');
    }

    final decoded = jsonDecode(response.body);
    final List<dynamic> data = decoded is List
        ? decoded
        : decoded is Map<String, dynamic> && decoded['saddles'] is List
            ? decoded['saddles'] as List<dynamic>
            : const [];
    return data
        .map((json) => Saddle.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Ordnet den Kategorienamen eines Sattels der Backend-Kategorie zu.
  Future<int> _categoryIdOf(Saddle saddle) async {
    final categories = await getSaddleCategories();
    final categoryName = saddle.category.trim().toLowerCase();
    final matching = categories.where((c) => c.name.toLowerCase() == categoryName);
    if (matching.isEmpty) {
      throw Exception('Unbekannte Sattelkategorie "${saddle.category}"');
    }
    return matching.first.id;
  }

  /// Fügt einem Pferd einen Sattel hinzu (`POST /api/horses/{id}/saddles`).
  Future<Saddle> addSaddle(Saddle saddle) async {
    final categoryId = await _categoryIdOf(saddle);
    final url = Uri.parse('$baseUrl/horses/${saddle.horseId}/saddles');
    final response = await apiHttp
        .post(
          url,
          headers: const {'Content-Type': 'application/json'},
          body: jsonEncode({'name': saddle.name, 'categoryId': categoryId}),
        )
        .timeout(_timeout);
    final Saddle savedSaddle;
    if (response.statusCode == 201 || response.statusCode == 200) {
      savedSaddle = Saddle.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    } else if (response.statusCode == 500) {
      // Das Backend speichert den Sattel, scheitert aber beim Erstellen der Antwort
      // (Kategorie nicht geladen). Daher den neuesten Sattel mit diesem Namen nachladen.
      final saddlesOfHorse = await _fetchSaddlesForHorse(saddle.horseId);
      final matching = saddlesOfHorse.where((s) => s.name == saddle.name).toList()
        ..sort((a, b) => (int.tryParse(b.id) ?? 0).compareTo(int.tryParse(a.id) ?? 0));
      if (matching.isEmpty) {
        throw Exception('Failed to add saddle to horse ${saddle.horseId}: HTTP 500');
      }
      savedSaddle = matching.first;
    } else {
      throw Exception('Failed to add saddle to horse ${saddle.horseId}: HTTP ${response.statusCode}');
    }

    _cachedSaddles?.add(savedSaddle);
    return savedSaddle;
  }

  /// Lädt alle Sattelkategorien (`GET /api/horses/saddles/categories`).
  Future<List<SaddleCategory>> getSaddleCategories() async {
    if (_cachedCategories != null) return _cachedCategories!;

    final url = Uri.parse('$baseUrl/horses/saddles/categories');
    final response = await apiHttp.get(url).timeout(_timeout);
    if (response.statusCode != 200) {
      throw Exception('Failed to load saddle categories: HTTP ${response.statusCode}');
    }

    final decoded = jsonDecode(response.body);
    final List<dynamic> data = decoded is List
        ? decoded
        : decoded is Map<String, dynamic> && decoded['categories'] is List
            ? decoded['categories'] as List<dynamic>
            : const [];
    _cachedCategories = data
        .map((json) => SaddleCategory.fromJson(json as Map<String, dynamic>))
        .toList();
    return _cachedCategories!;
  }

  /// Ändert Name und Kategorie eines Sattels (`PATCH /api/horses/{id}/saddles/{saddleId}`).
  /// Das Pferd eines Sattels kann nicht geändert werden.
  Future<Saddle> updateSaddle(Saddle saddle) async {
    final categoryId = await _categoryIdOf(saddle);
    final url = Uri.parse('$baseUrl/horses/${saddle.horseId}/saddles/${saddle.id}');
    final response = await apiHttp
        .patch(
          url,
          headers: const {'Content-Type': 'application/json'},
          body: jsonEncode({'name': saddle.name, 'categoryId': categoryId}),
        )
        .timeout(_timeout);
    if (response.statusCode != 200) {
      throw Exception('Sattel konnte nicht gespeichert werden (HTTP ${response.statusCode}).');
    }

    final updated = Saddle.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    final index = _cachedSaddles?.indexWhere((s) => s.id == saddle.id) ?? -1;
    if (index != -1) _cachedSaddles![index] = updated;
    return updated;
  }

  /// Entfernt einen Sattel (`DELETE /api/horses/{id}/saddles/{saddleId}`). Wurde mit dem
  /// Sattel schon gemessen, wirft das [SaddleInUseException].
  Future<void> deleteSaddle(Saddle saddle) async {
    final url = Uri.parse('$baseUrl/horses/${saddle.horseId}/saddles/${saddle.id}');
    final response = await apiHttp.delete(url).timeout(_timeout);
    if (response.statusCode == 409) throw SaddleInUseException();
    // 404: den Sattel gibt es schon nicht mehr
    if (response.statusCode != 204 && response.statusCode != 404) {
      throw Exception('Sattel konnte nicht gelöscht werden (HTTP ${response.statusCode}).');
    }
    _cachedSaddles?.removeWhere((s) => s.id == saddle.id);
  }

  /// Cache leeren (z.B. beim Logout)
  void clearCache() {
    _cachedSaddles = null;
    _cachedPersonId = null;
  }
}