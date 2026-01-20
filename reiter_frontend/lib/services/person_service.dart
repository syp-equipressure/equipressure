import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:reiterappfrontend/models/person.dart';

class PersonService {
  static final PersonService _instance = PersonService._internal();
  factory PersonService() => _instance;
  PersonService._internal();

  List<Person>? _cachedPersons;

  /// Lädt Personen aus dem JSON (später aus der Datenbank)
  Future<List<Person>> getPersons() async {
    if (_cachedPersons != null) {
      return _cachedPersons!;
    }

    try {
      final String response = await rootBundle.loadString('assets/data/user.json');
      final List<dynamic> data = json.decode(response);
      _cachedPersons = data.map((json) => Person.fromJson(json)).toList();
      return _cachedPersons!;
    } catch (e) {
      return [];
    }
  }

  /// Gibt nur sichtbare Personen zurück
  Future<List<Person>> getVisiblePersons() async {
    final persons = await getPersons();
    return persons.where((p) => !p.hidden).toList();
  }

  /// Fügt eine neue Person hinzu (später zur Datenbank)
  Future<void> addPerson(Person person) async {
    _cachedPersons ??= [];
    _cachedPersons!.add(person);
    // TODO: Später hier zur Datenbank hinzufügen
  }

  /// Aktualisiert eine Person (später in der Datenbank)
  Future<void> updatePerson(Person person) async {
    if (_cachedPersons == null) return;
    
    final index = _cachedPersons!.indexWhere((p) => p.id == person.id);
    if (index != -1) {
      _cachedPersons![index] = person;
      // TODO: Später hier in der Datenbank aktualisieren
    }
  }

  /// Löscht eine Person (später aus der Datenbank)
  Future<void> deletePerson(int id) async {
    if (_cachedPersons == null) return;
    
    _cachedPersons!.removeWhere((p) => p.id == id);
    // TODO: Später hier aus der Datenbank löschen
  }

  /// Gibt die nächste verfügbare ID zurück
  Future<int> getNextId() async {
    final persons = await getPersons();
    if (persons.isEmpty) return 1;
    return persons.map((p) => p.id).reduce((a, b) => a > b ? a : b) + 1;
  }

  /// Cache leeren (z.B. beim Logout)
  void clearCache() {
    _cachedPersons = null;
  }
}