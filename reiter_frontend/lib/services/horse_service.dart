import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/horse.dart';

class HorseService {
  static const String baseUrl = 'http://localhost:5200/api';
  static const Duration _timeout = Duration(seconds: 10);

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
      final String response = await rootBundle.loadString('assets/data/horses.json');
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

  /// Findet ein Pferd anhand der ID
  Future<Horse?> getHorseById(String id) async {
    final horses = await getHorses();
    try {
      return horses.firstWhere((h) => h.id == id);
    } catch (e) {
      return null;
    }
  }

  /// Cache leeren (z.B. beim Logout)
  void clearCache() {
    _cachedHorses = null;
  }

  // ===========================================================================
  // Backend-Anbindung (HorseController, api/horses)
  // ---------------------------------------------------------------------------
  // Die obigen Methoden arbeiten weiter auf der lokalen horses.json damit
  // bestehende Screens nicht brechen. Die folgenden sprechen das echte Backend
  // an. Screens können nach und nach darauf umgestellt werden.
  // ===========================================================================

  /// GET /api/horses/{personId}  -> HorseListResponse { "horses": [HorseDto] }
  Future<List<Horse>> fetchHorsesOfPerson(int personId) async {
    final url = Uri.parse('$baseUrl/horses/$personId');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      final decoded = jsonDecode(response.body);
      final list = _extractList(decoded);
      return list.map(_horseFromBackend).toList();
    }
    if (response.statusCode == 404) {
      return const [];
    }
    throw Exception('Failed to load horses of person $personId: HTTP ${response.statusCode}');
  }

  /// GET /api/horses/{id}  -> HorseDto
  Future<Horse?> fetchHorseById(int id) async {
    final url = Uri.parse('$baseUrl/horses/$id');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _horseFromBackend(jsonDecode(response.body) as Map<String, dynamic>);
    }
    if (response.statusCode == 404) {
      return null;
    }
    throw Exception('Failed to load horse $id: HTTP ${response.statusCode}');
  }

  // --- helpers -------------------------------------------------------------

  static List<Map<String, dynamic>> _extractList(dynamic decoded) {
    if (decoded is List) {
      return decoded.cast<Map<String, dynamic>>();
    }
    if (decoded is Map<String, dynamic>) {
      for (final value in decoded.values) {
        if (value is List) {
          return value.cast<Map<String, dynamic>>();
        }
      }
    }
    return const [];
  }

  /// Mappt das Backend-HorseDto auf das Frontend-Horse-Model.
  /// HorseDto: { id:int, name, dateOfBirth:"yyyy-MM-dd", weight:num,
  ///             height:num, gender, breedNames:[..], addressId:int }
  static Horse _horseFromBackend(Map<String, dynamic> json) {
    return Horse(
      id: json['id']?.toString() ?? '',
      name: (json['name'] ?? '') as String,
      breed: _joinBreeds(json['breedNames']),
      birthDate: _isoToDotDate(json['dateOfBirth']),
      weight: json['weight']?.toString() ?? '',
      height: json['height']?.toString() ?? '',
      sex: (json['gender'] ?? 'female').toString(),
    );
  }

  static String _joinBreeds(dynamic breedNames) {
    if (breedNames is List && breedNames.isNotEmpty) {
      return breedNames.map((e) => e.toString()).join(', ');
    }
    return 'Unbekannt';
  }

  /// "2015-06-15" (oder "2015-06-15T00:00:00...") -> "15.06.2015"
  static String _isoToDotDate(dynamic iso) {
    if (iso == null) return '';
    final raw = iso.toString();
    final datePart = raw.contains('T') ? raw.split('T').first : raw;
    final parts = datePart.split('-');
    if (parts.length != 3) return raw;
    return '${parts[2]}.${parts[1]}.${parts[0]}';
  }
}