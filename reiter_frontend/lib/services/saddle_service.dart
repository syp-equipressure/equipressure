import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/horse_service.dart';

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
    final response = await http.get(url).timeout(_timeout);
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

  /// Fügt einem Pferd einen Sattel hinzu (`POST /api/horses/{id}/saddles`).
  /// Die Kategorie wird über ihren Namen einer Backend-Kategorie zugeordnet.
  Future<Saddle> addSaddle(Saddle saddle) async {
    final categories = await getSaddleCategories();
    final categoryName = saddle.category.trim().toLowerCase();
    final matching = categories.where((c) => c.name.toLowerCase() == categoryName);
    if (matching.isEmpty) {
      throw Exception('Unbekannte Sattelkategorie "${saddle.category}"');
    }

    final url = Uri.parse('$baseUrl/horses/${saddle.horseId}/saddles');
    final response = await http
        .post(
          url,
          headers: const {'Content-Type': 'application/json'},
          body: jsonEncode({'name': saddle.name, 'categoryId': matching.first.id}),
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
    final response = await http.get(url).timeout(_timeout);
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

  /// Sattel-Änderungen werden vom Backend derzeit nicht unterstützt.
  Future<void> updateSaddle(Saddle saddle) async {
    throw UnsupportedError('The backend does not provide a saddle update endpoint.');
  }

  /// Sattel-Löschung wird vom Backend derzeit nicht unterstützt.
  Future<void> deleteSaddle(String id) async {
    throw UnsupportedError('The backend does not provide a saddle deletion endpoint.');
  }

  /// Cache leeren (z.B. beim Logout)
  void clearCache() {
    _cachedSaddles = null;
    _cachedPersonId = null;
  }
}