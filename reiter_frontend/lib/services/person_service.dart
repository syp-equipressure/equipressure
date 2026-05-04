import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/models/person.dart';

class PersonService {
  static const String baseUrl = 'http://localhost:5200/api';

  // Singleton
  static final PersonService _instance = PersonService._internal();
  factory PersonService() => _instance;
  PersonService._internal();

  List<Person>? _cachedPersons;

  // 1. Person (Equestrian) abrufen
  static Future<Map<String, dynamic>> getEquestrian(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/equestrians/$personId');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to load equestrian: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading equestrian: $e');
    }
  }

  // 1.2 Person (Saddler) abrufen
  static Future<Map<String, dynamic>> getSaddler(String equestrianId, String saddlerId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/equestrians/$equestrianId/saddlers/$saddlerId');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to load saddler: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading saddler: $e');
    }
  }

  // 2. Person abrufen (minimale Daten)
  static Future<Map<String, dynamic>> loadPersonProfileData(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/profile-data');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to load person profile data: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person profile data: $e');
    }
  }

  // 3. Adresse einer Person abrufen
  static Future<Map<String, dynamic>> loadPersonLocation(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/location');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to load person location: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person location: $e');
    }
  }

  // 4. Kontakte einer Person abrufen
  static Future<List<Map<String, dynamic>>> loadPersonContacts(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/contacts');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load person contacts: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person contacts: $e');
    }
  }

  // 5. Favoriten einer Person abrufen
  static Future<List<Map<String, dynamic>>> loadPersonFavourites(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/favourites');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load person favourites: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person favourites: $e');
    }
  }

  // 6. Alle Sattler mit Adresse
  static Future<List<Map<String, dynamic>>> getSaddlersWithLocations(String equestrianId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/equestrians/$equestrianId/saddlers/locations');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load saddlers with locations: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading saddlers with locations: $e');
    }
  }

  // 7. Alle Pferde einer Person abrufen
  static Future<List<Map<String, dynamic>>> loadPersonHorses(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/horses');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load person horses: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person horses: $e');
    }
  }

  // 8. Alle Geräte einer Person abrufen
  static Future<List<Map<String, dynamic>>> loadPersonDevices(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/devices');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load person devices: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person devices: $e');
    }
  }

  // 9. Neue Person anlegen
  static Future<Map<String, dynamic>> addPersonData(Map<String, dynamic> personData) async {
    try {
      final url = Uri.parse('$baseUrl/persons');
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(personData),
      ).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 201 || response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to add person: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error adding person: $e');
    }
  }

  // 10. Person aktualisieren
  static Future<Map<String, dynamic>> updatePerson(String personId, Map<String, dynamic> personData) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId');
      final response = await http.patch(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(personData),
      ).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to update person: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error updating person: $e');
    }
  }

  // 11. Person löschen
  static Future<void> deletePerson(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId');
      final response = await http.delete(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode != 200 && response.statusCode != 204) {
        throw Exception('Failed to delete person: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error deleting person: $e');
    }
  }

  // 12. Alle Pferde einer Person abrufen (mit Standort)
  static Future<List<Map<String, dynamic>>> loadPersonHorsesWithLocations(String personId) async {
    try {
      final url = Uri.parse('$baseUrl/persons/$personId/horses/locations');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load person horses with locations: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person horses with locations: $e');
    }
  }

  // 13. Location hinzufügen
  static Future<Map<String, dynamic>> addLocation(Map<String, dynamic> locationData) async {
    try {
      final url = Uri.parse('$baseUrl/locations');
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(locationData),
      ).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 201 || response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to add location: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error adding location: $e');
    }
  }

  // Zusätzliche Hilfsmethoden für die Screens
  
  // Lädt Gruppenmitglieder für ein Gerät (delegiert zu DeviceService)
  static Future<List<GroupMember>> loadGroupMembers(String deviceId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId/members');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body) as Map<String, dynamic>;
        final membersList = jsonData['members'] as List? ?? [];
        return membersList
            .map((member) => GroupMember.fromJson(member as Map<String, dynamic>))
            .toList();
      } else if (response.statusCode == 404) {
        return [];
      } else {
        throw Exception('Failed to load group members: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading group members: $e');
    }
  }

  // Lädt aktuelle User Data
  static Future<Map<String, dynamic>> loadUserData() async {
    try {
      final url = Uri.parse('$baseUrl/users/current');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to load user data: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading user data: $e');
    }
  }

  // Lädt Geräte des aktuellen Users
  static Future<List<Map<String, dynamic>>> loadDevices() async {
    try {
      final url = Uri.parse('$baseUrl/users/current/devices');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as List;
        return data.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load devices: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading devices: $e');
    }
  }

  // Instanzmethoden für bestehende Screens

  /// Lädt Personen aus der API
  Future<List<Person>> getPersons() async {
    if (_cachedPersons != null) {
      return _cachedPersons!;
    }

    try {
      final url = Uri.parse('$baseUrl/persons');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        _cachedPersons = data.map((json) => Person.fromJson(json as Map<String, dynamic>)).toList();
        return _cachedPersons!;
      } else {
        throw Exception('Failed to load persons: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading persons: $e');
    }
  }

  /// Gibt die nächste verfügbare Person ID zurück
  Future<int> getNextId() async {
    final persons = await getPersons();
    if (persons.isEmpty) return 1;
    return persons.map((p) => p.id).reduce((a, b) => a > b ? a : b) + 1;
  }

  /// Fügt eine Person hinzu
  Future<void> addPerson(Person person) async {
    try {
      await addPersonData(person.toJson());
      _cachedPersons = null; // Cache invalidieren
    } catch (e) {
      throw Exception('Error adding person: $e');
    }
  }

  /// Cache leeren
  void clearCache() {
    _cachedPersons = null;
  }
}