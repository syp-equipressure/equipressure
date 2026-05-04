import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/models/person.dart';

/// Geworfen wenn das Backend einen erwarteten Fehlerstatus liefert
/// (400/404/409). Schaut so aus dass aufrufende Screens unterscheiden können
/// ob "nicht gefunden", "konflikt" oder "ungültig".
class PersonApiException implements Exception {
  final int statusCode;
  final String message;

  PersonApiException(this.statusCode, this.message);

  bool get isNotFound => statusCode == 404;
  bool get isConflict => statusCode == 409;
  bool get isBadRequest => statusCode == 400;

  @override
  String toString() => 'PersonApiException($statusCode): $message';
}

class PersonService {
  static const String baseUrl = 'http://localhost:5200/api';
  static const Duration _timeout = Duration(seconds: 10);
  static const Map<String, String> _jsonHeaders = {
    'Content-Type': 'application/json',
  };

  // Singleton
  static final PersonService _instance = PersonService._internal();
  factory PersonService() => _instance;
  PersonService._internal();

  List<Person>? _cachedPersons;

  // ---------------------------------------------------------------------------
  // Helper
  // ---------------------------------------------------------------------------

  /// Extrahiert die Liste aus einer *ListResponse. Backend liefert z.B.
  /// `{ "persons": [...] }`, `{ "horses": [...] }`, `{ "devices": [...] }`,
  /// `{ "saddlers": [...] }`. Welcher Key genau verwendet wird hängt von den
  /// DTO-Records ab — daher pragmatisch: ersten List-Wert im Objekt nehmen.
  /// Falls Backend doch raw List liefert, ebenfalls korrekt parsen.
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
      return const [];
    }
    return const [];
  }

  static Never _throwForStatus(http.Response r, String action) {
    throw PersonApiException(
      r.statusCode,
      'Failed to $action: HTTP ${r.statusCode}',
    );
  }

  // ---------------------------------------------------------------------------
  // 1. GET /persons/equestrians/{id}
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>> getEquestrian(String personId) async {
    final url = Uri.parse('$baseUrl/persons/equestrians/$personId');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load equestrian');
  }

  // ---------------------------------------------------------------------------
  // 2. GET /persons/equestrians/{equestrianId}/saddlers/{saddlerId}
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>> getSaddler(
    String equestrianId,
    String saddlerId,
  ) async {
    final url = Uri.parse(
      '$baseUrl/persons/equestrians/$equestrianId/saddlers/$saddlerId',
    );
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load saddler');
  }

  // ---------------------------------------------------------------------------
  // 3. GET /persons/{id}/profile-data  (NameDataDto)
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>> loadPersonProfileData(
    String personId,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId/profile-data');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load person profile data');
  }

  // ---------------------------------------------------------------------------
  // 4. GET /persons/{id}/location  (AddressDto)
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>> loadPersonLocation(
    String personId,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId/location');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load person location');
  }

  // ---------------------------------------------------------------------------
  // 5. GET /persons/{id}/contacts  (PersonListResponse)
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> loadPersonContacts(
    String personId,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId/contacts');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    _throwForStatus(response, 'load person contacts');
  }

  // ---------------------------------------------------------------------------
  // 6. GET /persons/{id}/favourites  (PersonListResponse)
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> loadPersonFavourites(
    String personId,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId/favourites');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    _throwForStatus(response, 'load person favourites');
  }

  // ---------------------------------------------------------------------------
  // 7. GET /persons/{id}/horses  (HorseListResponse)
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> loadPersonHorses(
    String personId,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId/horses');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    _throwForStatus(response, 'load person horses');
  }

  // ---------------------------------------------------------------------------
  // 8. GET /persons/{id}/devices  (DeviceListResponse)
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> loadPersonDevices(
    String personId,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId/devices');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    _throwForStatus(response, 'load person devices');
  }

  // ---------------------------------------------------------------------------
  // 9. GET /persons/equestrians/{equestrianId}/saddlers/locations
  //    (SaddlersListResponse)
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> getSaddlersWithLocations(
    String equestrianId,
  ) async {
    final url = Uri.parse(
      '$baseUrl/persons/equestrians/$equestrianId/saddlers/locations',
    );
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    _throwForStatus(response, 'load saddlers with locations');
  }

  // ---------------------------------------------------------------------------
  // 10. POST /persons  (AddPersonRequest)
  //     Backend liefert 201 Created OHNE body, 400 oder 409.
  // ---------------------------------------------------------------------------
  static Future<void> addPersonData(Map<String, dynamic> personData) async {
    final url = Uri.parse('$baseUrl/persons');
    final response = await http
        .post(url, headers: _jsonHeaders, body: jsonEncode(personData))
        .timeout(_timeout);

    if (response.statusCode == 201) {
      return;
    }
    _throwForStatus(response, 'add person');
  }

  // ---------------------------------------------------------------------------
  // 11. PATCH /persons/{id}  (UpdatePersonRequest)
  //     Backend prüft: id (route) == request.Id (body)! Body muss also id
  //     enthalten. 204 No Content bei Erfolg, 400/404/409 bei Fehler.
  // ---------------------------------------------------------------------------
  static Future<void> updatePerson(
    String personId,
    Map<String, dynamic> personData,
  ) async {
    final url = Uri.parse('$baseUrl/persons/$personId');

    // Sicherstellen dass id im body mit der route id matched, sonst 400.
    final body = Map<String, dynamic>.from(personData);
    body['id'] = int.tryParse(personId) ?? personId;

    final response = await http
        .patch(url, headers: _jsonHeaders, body: jsonEncode(body))
        .timeout(_timeout);

    if (response.statusCode == 204) {
      return;
    }
    _throwForStatus(response, 'update person');
  }

  // ---------------------------------------------------------------------------
  // 12. DELETE /persons/{id}
  //     Backend liefert 204 oder 404.
  // ---------------------------------------------------------------------------
  static Future<void> deletePerson(String personId) async {
    final url = Uri.parse('$baseUrl/persons/$personId');
    final response = await http.delete(url).timeout(_timeout);

    if (response.statusCode == 204) {
      return;
    }
    _throwForStatus(response, 'delete person');
  }

  // ---------------------------------------------------------------------------
  // Hilfsmethoden für Screens — diese Endpoints sind NICHT im PersonController
  // dokumentiert und gehören vermutlich in eigene Services. Vorerst hier
  // belassen damit existierende Screens nicht brechen.
  // TODO: in passende Services verschieben sobald Backend-Endpoints klar sind.
  // ---------------------------------------------------------------------------

  static Future<List<GroupMember>> loadGroupMembers(String deviceId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/members');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      final jsonData = jsonDecode(response.body) as Map<String, dynamic>;
      final membersList = jsonData['members'] as List? ?? [];
      return membersList
          .map((m) => GroupMember.fromJson(m as Map<String, dynamic>))
          .toList();
    }
    if (response.statusCode == 404) {
      return [];
    }
    _throwForStatus(response, 'load group members');
  }

  static Future<Map<String, dynamic>> loadUserData() async {
    final url = Uri.parse('$baseUrl/users/current');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load user data');
  }

  static Future<List<Map<String, dynamic>>> loadDevices() async {
    final url = Uri.parse('$baseUrl/users/current/devices');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    _throwForStatus(response, 'load devices');
  }

  // ---------------------------------------------------------------------------
  // Instanzmethoden für bestehende Screens.
  // Achtung: GET /persons (Liste aller Personen) existiert NICHT im aktuellen
  // PersonController. `getPersons` und `getNextId` werden daher fehlschlagen
  // sobald das Backend live ist. Sind hier nur weil sie von new_measurement.dart
  // verwendet werden.
  // TODO: mit Backend abklären wie eine Personenliste geladen werden soll.
  // ---------------------------------------------------------------------------

  Future<List<Person>> getPersons() async {
    if (_cachedPersons != null) {
      return _cachedPersons!;
    }

    final url = Uri.parse('$baseUrl/persons');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(response.body);
      _cachedPersons = data
          .map((json) => Person.fromJson(json as Map<String, dynamic>))
          .toList();
      return _cachedPersons!;
    }
    _throwForStatus(response, 'load persons');
  }

  Future<int> getNextId() async {
    final persons = await getPersons();
    if (persons.isEmpty) return 1;
    return persons.map((p) => p.id).reduce((a, b) => a > b ? a : b) + 1;
  }

  Future<void> addPerson(Person person) async {
    await addPersonData(person.toJson());
    _cachedPersons = null; // Cache invalidieren
  }

  void clearCache() {
    _cachedPersons = null;
  }
}
