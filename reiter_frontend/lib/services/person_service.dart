import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/person.dart';
import 'package:reiterappfrontend/models/user_data.dart';
import 'package:reiterappfrontend/services/device_service.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';

import 'package:reiterappfrontend/services/api_config.dart';

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
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 10);
  static const Map<String, String> _jsonHeaders = {
    'Content-Type': 'application/json',
  };

  // Singleton
  static final PersonService _instance = PersonService._internal();
  factory PersonService() => _instance;
  PersonService._internal();

  List<Person>? _cachedPersons;
  String? _cachedPersonContext;

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
  // 1. GET /persons/{id}
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>> getEquestrian(String personId) async {
    return _fetchPersonDetails(personId);
  }

  // ---------------------------------------------------------------------------
  // 2. GET /persons/{equestrianId}/contacts + GET /persons/{saddlerId}
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>> getSaddler(
    String equestrianId,
    String saddlerId,
  ) async {
    final contacts = await loadPersonContacts(equestrianId);
    if (!contacts.any((contact) => contact['id'].toString() == saddlerId)) {
      throw PersonApiException(404, 'Saddler $saddlerId is not a contact of $equestrianId');
    }
    return _fetchPersonDetails(saddlerId);
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

  static Future<Map<String, dynamic>> updatePersonLocation(
    String personId, {
    String? addressName,
    required String cityName,
    required String plz,
  }) async {
    final url = Uri.parse('$baseUrl/persons/$personId/location');
    final response = await http
        .put(
          url,
          headers: _jsonHeaders,
          body: jsonEncode({
            'addressName': addressName,
            'cityName': cityName,
            'plz': plz,
          }),
        )
        .timeout(_timeout);
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'update person location');
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
  // 9. GET /persons/{equestrianId}/contacts (ContactListResponse)
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> getSaddlersWithLocations(
    String equestrianId,
  ) async {
    return loadPersonContacts(equestrianId);
  }

  // ---------------------------------------------------------------------------
  // 10. POST /persons (AddPersonRequest)
  // ---------------------------------------------------------------------------
  static Future<Person> addPersonData(Map<String, dynamic> personData) async {
    final url = Uri.parse('$baseUrl/persons');
    final response = await http
        .post(url, headers: _jsonHeaders, body: jsonEncode(personData))
        .timeout(_timeout);

    if (response.statusCode == 201) {
      return _personFromBackend(
        jsonDecode(response.body) as Map<String, dynamic>,
      );
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

  static Future<void> addContact(String personId, String contactId) async {
    final url = Uri.parse('$baseUrl/persons/$personId/contacts/$contactId');
    final response = await http.put(url).timeout(_timeout);
    if (response.statusCode == 204) {
      PersonService().clearCache();
      return;
    }
    _throwForStatus(response, 'add contact');
  }

  static Future<void> removeContact(String personId, String contactId) async {
    final url = Uri.parse('$baseUrl/persons/$personId/contacts/$contactId');
    final response = await http.delete(url).timeout(_timeout);
    if (response.statusCode == 204) {
      PersonService().clearCache();
      return;
    }
    _throwForStatus(response, 'remove contact');
  }

  static Future<UserData> loadUserData(String personId) async {
    final responses = await Future.wait([
      http.get(Uri.parse('$baseUrl/persons/$personId')).timeout(_timeout),
      http
          .get(Uri.parse('$baseUrl/persons/$personId/profile-data'))
          .timeout(_timeout),
      http
          .get(Uri.parse('$baseUrl/persons/$personId/location'))
          .timeout(_timeout),
    ]);
    for (var index = 0; index < responses.length; index++) {
      if (responses[index].statusCode != 200) {
        _throwForStatus(responses[index], 'load person profile data');
      }
    }

    final details = jsonDecode(responses[0].body) as Map<String, dynamic>;
    final name = jsonDecode(responses[1].body) as Map<String, dynamic>;
    final location = jsonDecode(responses[2].body) as Map<String, dynamic>;
    final firstName = name['firstName'] ?? name['firstname'];
    final lastName = name['lastName'] ?? name['lastname'];
    if (firstName == null || lastName == null) {
      throw const FormatException('Backend person profile has no full name');
    }

    final addressName = location['addressName']?.toString();
    final city = location['cityName']?.toString() ?? '';
    final postalCode = (location['plz'] ?? location['PLZ'])?.toString() ?? '';
    return UserData(
      id: (details['id'] ?? personId).toString(),
      name: '$firstName $lastName',
      imageUrl: '',
      birthDate: 'Nicht verfügbar',
      address: [
        addressName,
        '$postalCode $city',
      ].where((part) => part != null && part.trim().isNotEmpty).join(', '),
      email: details['email']?.toString() ?? 'Nicht hinterlegt',
      height: '${details['height'] ?? '–'} cm',
      weight: '${details['weight'] ?? '–'} kg',
    );
  }

  static Future<List<Device>> loadDevices(String personId) async {
    return DeviceService().getDevices(personId);
  }

  // ---------------------------------------------------------------------------
  // Instanzmethoden für bestehende Screens.
  // ---------------------------------------------------------------------------

  // The backend has no global person list, so use the selected person and their
  // contacts, hydrating each entry through the existing person-detail route.
  Future<List<Person>> getPersons() async {
    final personId = await BackendPersonContextService.loadPersonId() ??
        AuthService().currentUserId()?.toString();
    if (personId == null) throw StateError('No backend person is selected.');
    if (_cachedPersons != null && _cachedPersonContext == personId) {
      return _cachedPersons!;
    }

    final contacts = await loadPersonContacts(personId);
    final personIds = <String>{
      personId,
      ...contacts.map((contact) => contact['id'].toString()),
    };
    final details = await Future.wait(
      personIds.map(_fetchPersonDetails),
    );
    _cachedPersons = details.map(_personFromBackend).toList();
    _cachedPersonContext = personId;
    return _cachedPersons!;
  }

  Future<Person> addPerson(Person person) async {
    final dateOfBirth = person.dob;
    if (dateOfBirth == null) {
      throw ArgumentError('A date of birth is required to create a person.');
    }

    final createdPerson = await addPersonData({
      'firstName': person.firstname,
      'lastName': person.lastname,
      'height': person.heightCm,
      'weight': person.weightKg,
      'dateOfBirth': dateOfBirth.toIso8601String().split('T').first,
      'email': person.email.trim().isEmpty ? null : person.email.trim(),
      'websiteLink': person.websiteLink,
      'description': person.description,
      'address': {
        'addressName': person.street.trim().isEmpty ? null : person.street.trim(),
        'cityName': person.city,
        'plz': person.plz.toString(),
      },
      'role': 'Equestrian',
    });

    if (_cachedPersons != null &&
        !_cachedPersons!.any((cached) => cached.id == createdPerson.id)) {
      _cachedPersons!.add(createdPerson);
    }
    return createdPerson;
  }

  void clearCache() {
    _cachedPersons = null;
    _cachedPersonContext = null;
  }

  static Future<Map<String, dynamic>> _fetchPersonDetails(String personId) async {
    final url = Uri.parse('$baseUrl/persons/$personId');
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load person $personId');
  }

  static Person _personFromBackend(Map<String, dynamic> json) {
    final address = json['address'] as Map<String, dynamic>? ?? const {};
    final dateOfBirth = json['dateOfBirth']?.toString();
    final height = json['height'];
    final weight = json['weight'];
    final postalCode = (address['plz'] ?? '').toString();

    return Person(
      id: int.tryParse(json['id']?.toString() ?? '') ?? 0,
      firstname: (json['firstName'] ?? '').toString(),
      lastname: (json['lastName'] ?? '').toString(),
      heightCm: height is num ? height.round() : int.tryParse('$height') ?? 0,
      weightKg: weight is num ? weight.round() : int.tryParse('$weight') ?? 0,
      plz: int.tryParse(postalCode) ?? 0,
      city: (address['cityName'] ?? '').toString(),
      street: (address['addressName'] ?? '').toString(),
      housenumber: 0,
      email: (json['email'] ?? '').toString(),
      websiteLink: json['websiteLink']?.toString(),
      description: json['description']?.toString(),
      dob: dateOfBirth == null ? null : DateTime.tryParse(dateOfBirth),
    );
  }
}
