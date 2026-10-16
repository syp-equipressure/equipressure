import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';

class HorseService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 5);

  static final HorseService _instance = HorseService._internal();
  factory HorseService() => _instance;
  HorseService._internal();

  List<Horse>? _cachedHorses;
  int? _cachedPersonId;

  /// Lädt die Pferde einer Person aus dem Backend.
  Future<List<Horse>> getHorses() async {
    final personIdStr = await BackendPersonContextService.loadPersonId() ?? '1';
    final personId = int.tryParse(personIdStr) ?? 1;
    if (_cachedHorses != null && _cachedPersonId == personId) {
      return _cachedHorses!;
    }

    _cachedHorses = await fetchHorsesOfPerson(personId);
    _cachedPersonId = personId;
    return _cachedHorses!;
  }

  /// Fügt ein neues Pferd hinzu
  Future<void> addHorse(Horse horse) async {
    _cachedHorses ??= [];
    _cachedHorses!.add(horse);

    try {
      final personIdStr = await BackendPersonContextService.loadPersonId() ?? '1';
      final personId = int.tryParse(personIdStr) ?? 1;
      await createHorseOnBackend(horse, personId);
    } catch (_) {
      // Ignoriere Netzwerkfehler beim Offline-Modus
    }
  }

  /// Aktualisiert ein Pferd
  Future<void> updateHorse(Horse horse) async {
    if (_cachedHorses == null) return;
    
    final index = _cachedHorses!.indexWhere((h) => h.id == horse.id);
    if (index != -1) {
      _cachedHorses![index] = horse;
    }
  }

  /// Löscht ein Pferd
  Future<void> deleteHorse(String id) async {
    if (_cachedHorses == null) return;
    _cachedHorses!.removeWhere((h) => h.id == id);
  }

  /// Findet ein Pferd anhand der ID
  Future<Horse?> getHorseById(String id) async {
    final parsedId = int.tryParse(id);
    if (parsedId != null) {
      try {
        final backendHorse = await fetchHorseById(parsedId);
        if (backendHorse != null) return backendHorse;
      } catch (_) {}
    }

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
    _cachedPersonId = null;
  }

  // ===========================================================================
  // Backend-Anbindung (HorseController & PersonController)
  // ===========================================================================

  /// GET /api/persons/{personId}/horses -> HorseListResponse { "horses": [HorseDto] }
  Future<List<Horse>> fetchHorsesOfPerson(int personId) async {
    final url = Uri.parse('$baseUrl/persons/$personId/horses');
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

  /// GET /api/horses/{id} -> HorseDto
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

  /// GET /api/horses/{id}/riders -> owner and visible riders.
  Future<List<Map<String, dynamic>>> fetchRidersOfHorse(int id) async {
    return _fetchHorsePeople('$baseUrl/horses/$id/riders');
  }

  /// GET /api/horses/{id}/riders/hidden -> hidden riders.
  Future<List<Map<String, dynamic>>> fetchHiddenRidersOfHorse(int id) async {
    return _fetchHorsePeople('$baseUrl/horses/$id/riders/hidden');
  }

  Future<List<Map<String, dynamic>>> _fetchHorsePeople(String url) async {
    final response = await http.get(Uri.parse(url)).timeout(_timeout);
    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    if (response.statusCode == 404) return const [];
    throw Exception('Failed to load horse riders: HTTP ${response.statusCode}');
  }

  /// POST /api/horses -> AddHorseRequest
  Future<Horse?> createHorseOnBackend(Horse horse, int ownerId) async {
    final url = Uri.parse('$baseUrl/horses');
    final body = jsonEncode({
      'name': horse.name,
      'dateOfBirth': _dotToIsoDate(horse.birthDate),
      'weight': double.tryParse(horse.weight) ?? 500,
      'height': double.tryParse(horse.height) ?? 160,
      'gender': horse.sex.toLowerCase().contains('hengst') || horse.sex.toLowerCase().contains('male')
          ? 'male'
          : horse.sex.toLowerCase().contains('wallach')
              ? 'gelding'
              : 'female',
      'breedNames': horse.breed.isNotEmpty ? [horse.breed] : ['Unbekannt'],
      'ownerId': ownerId,
    });

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: body,
    ).timeout(_timeout);

    if (response.statusCode == 201 || response.statusCode == 200) {
      return _horseFromBackend(jsonDecode(response.body) as Map<String, dynamic>);
    }
    return null;
  }

  Future<int> currentPersonId() async {
    final personId = await BackendPersonContextService.loadPersonId() ??
        AuthService().currentUserId()?.toString();
    final parsedId = int.tryParse(personId ?? '');
    if (parsedId == null) throw StateError('No backend person is selected.');
    return parsedId;
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

  static String _isoToDotDate(dynamic iso) {
    if (iso == null) return '';
    final raw = iso.toString();
    final datePart = raw.contains('T') ? raw.split('T').first : raw;
    final parts = datePart.split('-');
    if (parts.length != 3) return raw;
    return '${parts[2]}.${parts[1]}.${parts[0]}';
  }

  static String _dotToIsoDate(String dotDate) {
    final parts = dotDate.split('.');
    if (parts.length != 3) return DateTime.now().toIso8601String().split('T').first;
    return '${parts[2]}-${parts[1].padLeft(2, '0')}-${parts[0].padLeft(2, '0')}';
  }
}
