import 'dart:convert';
import 'package:reiterappfrontend/services/api_client.dart';
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

  /// Sattel-Anlage wird vom Backend derzeit nicht unterstützt.
  Future<void> addSaddle(Saddle saddle) async {
    throw UnsupportedError('The backend does not provide a saddle creation endpoint.');
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