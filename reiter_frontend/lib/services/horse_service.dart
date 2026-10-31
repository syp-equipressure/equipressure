import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/services/api_client.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';
import 'package:reiterappfrontend/services/geocoding_service.dart';

class HorseService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 5);

  static final HorseService _instance = HorseService._internal();
  factory HorseService() => _instance;
  HorseService._internal();

  List<Horse>? _cachedHorses;
  int? _cachedPersonId;
  List<Breed>? _cachedBreeds;

  /// Lädt alle Rassen nach Namen sortiert (`GET /api/breeds`).
  Future<List<Breed>> getBreeds() async {
    if (_cachedBreeds != null) return _cachedBreeds!;

    final url = Uri.parse('$baseUrl/breeds');
    final response = await apiHttp.get(url).timeout(_timeout);
    if (response.statusCode != 200) {
      throw Exception('Rassen konnten nicht geladen werden (HTTP ${response.statusCode}).');
    }
    _cachedBreeds = _extractList(jsonDecode(response.body))
        .map((json) => Breed(id: (json['id'] as num).toInt(), name: json['name'].toString()))
        .toList();
    return _cachedBreeds!;
  }

  /// Lädt die Pferde einer Person aus dem Backend. Gibt eine Kopie des Caches zurück,
  /// sonst landet ein neues Pferd doppelt in der Liste des Aufrufers (Cache + eigenes add).
  Future<List<Horse>> getHorses() async {
    final personId = await currentPersonId();
    if (_cachedHorses == null || _cachedPersonId != personId) {
      final horses = await fetchHorsesOfPerson(personId);
      _cachedHorses = await Future.wait(horses.map(_withStableLocation));
      _cachedPersonId = personId;
    }
    return List.of(_cachedHorses!);
  }

  /// Das Backend speichert keine Koordinaten, daher wird die Stalladresse geocodiert.
  static Future<Horse> _withStableLocation(Horse horse) async {
    if (horse.hasStableLocation) return horse;
    final location = await GeocodingService().geocode(
      street: horse.stableStreet,
      postalCode: horse.stablePostalCode,
      city: horse.stableCity,
    );
    if (location == null) return horse;
    return horse.copyWith(
      stableLatitude: location.latitude,
      stableLongitude: location.longitude,
    );
  }

  /// Fügt ein neues Pferd hinzu
  Future<Horse> addHorse(Horse horse) async {
    final personId = await currentPersonId();
    final savedHorse = await _withStableLocation(await createHorseOnBackend(horse, personId));
    if (_cachedPersonId == personId && _cachedHorses != null) {
      _cachedHorses!.add(savedHorse);
    }
    return savedHorse;
  }

  /// Speichert die geänderten Werte eines Pferdes im Backend (PATCH /api/horses/{id}).
  /// Ein neu ausgewähltes Bild wird mit hochgeladen.
  Future<Horse> updateHorse(Horse horse) async {
    final url = Uri.parse('$baseUrl/horses/${horse.id}');
    final response = await apiHttp
        .patch(
          url,
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode(await _horseRequestBody(horse)),
        )
        .timeout(_timeout);
    if (response.statusCode != 200) {
      throw Exception(
        'Pferd konnte nicht gespeichert werden (HTTP ${response.statusCode}).',
      );
    }

    final saved = _horseFromBackend(jsonDecode(response.body) as Map<String, dynamic>);
    final updated = await _withStableLocation(
      saved.copyWith(image: horse.image, assetImage: horse.assetImage),
    );
    if (horse.image != null) await uploadImage(updated, horse.image!);
    final index = _cachedHorses?.indexWhere((h) => h.id == horse.id) ?? -1;
    if (index != -1) _cachedHorses![index] = updated;
    return updated;
  }

  /// Speichert das Bild eines Pferdes im Backend (PUT /api/horses/{id}/image).
  Future<Horse> uploadImage(Horse horse, File image) async {
    final request = http.MultipartRequest('PUT', Uri.parse('$baseUrl/horses/${horse.id}/image'))
      ..files.add(await http.MultipartFile.fromPath('file', image.path));

    final response = await http.Response.fromStream(
      await apiHttp.send(request).timeout(const Duration(seconds: 60)),
    );
    if (response.statusCode != 204) {
      throw Exception('Bild konnte nicht gespeichert werden (HTTP ${response.statusCode}).');
    }

    final updated = horse.copyWith(image: image);
    final index = _cachedHorses?.indexWhere((h) => h.id == horse.id) ?? -1;
    if (index != -1) _cachedHorses![index] = updated;
    return updated;
  }

  /// Adresse des Bildes eines Pferdes, null ohne Bild im Backend.
  /// Das Token steht in der Adresse, weil Bilder keinen Header mitschicken.
  String? imageUrl(Horse horse) {
    final updatedAt = horse.imageUpdatedAt;
    if (updatedAt == null) return null;
    final token = AuthService().accessToken;
    return Uri.parse('$baseUrl/horses/${horse.id}/image').replace(queryParameters: {
      'v': updatedAt,
      if (token != null) 'access_token': token,
    }).toString();
  }

  /// Löscht ein Pferd samt Sätteln und Messungen im Backend (DELETE /api/horses/{id}).
  Future<void> deleteHorse(String id) async {
    final url = Uri.parse('$baseUrl/horses/$id');
    final response = await apiHttp.delete(url).timeout(_timeout);
    // 404: das Pferd gibt es schon nicht mehr
    if (response.statusCode != 204 && response.statusCode != 404) {
      throw Exception(
        'Pferd konnte nicht gelöscht werden (HTTP ${response.statusCode}).',
      );
    }
    _cachedHorses?.removeWhere((h) => h.id == id);
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
    final response = await apiHttp.get(url).timeout(_timeout);

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
    final response = await apiHttp.get(url).timeout(_timeout);

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
    final response = await apiHttp.get(Uri.parse(url)).timeout(_timeout);
    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    if (response.statusCode == 404) return const [];
    throw Exception('Failed to load horse riders: HTTP ${response.statusCode}');
  }

  /// POST /api/horses -> AddHorseRequest
  Future<Horse> createHorseOnBackend(Horse horse, int ownerId) async {
    final url = Uri.parse('$baseUrl/horses');
    final body = jsonEncode({...await _horseRequestBody(horse), 'ownerId': ownerId});

    final response = await apiHttp.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: body,
    ).timeout(_timeout);

    if (response.statusCode == 201 || response.statusCode == 200) {
      return _horseFromBackend(jsonDecode(response.body) as Map<String, dynamic>);
    }
    throw Exception(
      'Pferd konnte nicht gespeichert werden (HTTP ${response.statusCode}): ${response.body}',
    );
  }

  /// Die Werte eines Pferdes, wie sie das Backend beim Anlegen und Ändern erwartet
  Future<Map<String, dynamic>> _horseRequestBody(Horse horse) async {
    final cityName = horse.stableCity?.trim() ?? '';
    final postalCode = horse.stablePostalCode?.trim() ?? '';
    if (cityName.isEmpty || postalCode.isEmpty) {
      throw ArgumentError('Stallort und Postleitzahl müssen angegeben werden.');
    }
    final street = [horse.stableStreet?.trim(), horse.stableHouseNumber?.trim()]
        .where((part) => part != null && part.isNotEmpty)
        .join(' ');

    return {
      'name': horse.name,
      'dateOfBirth': _dotToIsoDate(horse.birthDate),
      'weight': double.tryParse(horse.weight),
      'height': double.tryParse(horse.height),
      'gender': horse.sex.toLowerCase() == 'male' ? 'Male' : 'Female',
      'address': {
        'addressName': street.isEmpty ? null : street,
        'cityName': cityName,
        'plz': postalCode,
      },
      'breedIds': await _breedIdsForNames(horse.breed),
    };
  }

  /// Die Rassen sind im Pferd als Namen gespeichert ("Haflinger, Noriker")
  Future<List<int>> _breedIdsForNames(String breedNames) async {
    final names = breedNames
        .split(',')
        .map((name) => name.trim().toLowerCase())
        .where((name) => name.isNotEmpty)
        .toSet();
    if (names.isEmpty) {
      throw ArgumentError('Bitte eine Pferderasse auswählen.');
    }

    final idsByName = {
      for (final breed in await getBreeds()) breed.name.toLowerCase(): breed.id,
    };
    final unknownNames = names.where((name) => !idsByName.containsKey(name));
    if (unknownNames.isNotEmpty) {
      throw ArgumentError('Unbekannte Rasse: ${unknownNames.join(', ')}');
    }
    return names.map((name) => idsByName[name]!).toList();
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
    final address = json['address'] as Map<String, dynamic>?;
    return Horse(
      id: json['id']?.toString() ?? '',
      name: (json['name'] ?? '') as String,
      breed: _joinBreeds(json['breedNames']),
      birthDate: _isoToDotDate(json['dateOfBirth']),
      weight: json['weight']?.toString() ?? '',
      height: json['height']?.toString() ?? '',
      sex: (json['gender'] ?? 'female').toString(),
      imageUpdatedAt: json['imageUpdatedAt']?.toString(),
      stableCity: address?['cityName']?.toString(),
      stablePostalCode: address?['plz']?.toString(),
      stableStreet: address?['addressName']?.toString(),
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
