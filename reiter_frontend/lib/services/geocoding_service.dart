import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Wandelt Adressen in Koordinaten um, weil das Backend nur Adressen speichert.
///
/// Nutzt OpenStreetMap Nominatim (max. 1 Anfrage pro Sekunde laut Nutzungsbedingungen).
/// Ergebnisse werden dauerhaft in den SharedPreferences zwischengespeichert,
/// dadurch wird jede Adresse nur einmal abgefragt.
class GeocodingService {
  static const String _cacheKey = 'geocoding_cache';
  static const Duration _timeout = Duration(seconds: 10);
  static const Duration _minRequestInterval = Duration(milliseconds: 1100);

  static final GeocodingService _instance = GeocodingService._internal();
  factory GeocodingService() => _instance;
  GeocodingService._internal();

  Map<String, List<double>?>? _cache;
  Future<void> _requestQueue = Future.value();

  /// Liefert die Koordinaten einer Adresse oder null, wenn sie nicht gefunden wird.
  /// Findet Nominatim die Straße nicht, wird nur mit PLZ und Ort gesucht.
  Future<LatLng?> geocode({
    String? street,
    String? postalCode,
    String? city,
  }) async {
    final hasStreet = street?.trim().isNotEmpty == true;
    final hasPlace = postalCode?.trim().isNotEmpty == true || city?.trim().isNotEmpty == true;
    if (!hasPlace) return null;

    if (hasStreet) {
      final exact = await _lookup(street: street, postalCode: postalCode, city: city);
      if (exact != null) return exact;
    }
    return _lookup(postalCode: postalCode, city: city);
  }

  Future<LatLng?> _lookup({String? street, String? postalCode, String? city}) async {
    final parameters = <String, String>{
      if (street?.trim().isNotEmpty == true) 'street': street!.trim(),
      if (postalCode?.trim().isNotEmpty == true) 'postalcode': postalCode!.trim(),
      if (city?.trim().isNotEmpty == true) 'city': city!.trim(),
    };
    final key = parameters.entries.map((e) => '${e.key}=${e.value.toLowerCase()}').join('&');

    final cache = await _loadCache();
    if (cache.containsKey(key)) {
      final cached = cache[key];
      return cached == null ? null : LatLng(cached[0], cached[1]);
    }

    final result = await _throttled(() => _request(parameters));
    // Netzwerkfehler werden nicht gecacht, damit es beim nächsten Mal erneut versucht wird
    if (result.$2) {
      cache[key] = result.$1 == null ? null : [result.$1!.latitude, result.$1!.longitude];
      await _saveCache(cache);
    }
    return result.$1;
  }

  /// Liefert (Koordinaten, Antwort gültig).
  Future<(LatLng?, bool)> _request(Map<String, String> parameters) async {
    final url = Uri.https('nominatim.openstreetmap.org', '/search', {
      'format': 'json',
      'limit': '1',
      'country': 'Austria',
      ...parameters,
    });
    try {
      // Im Browser darf der User-Agent nicht gesetzt werden
      final response = await http
          .get(url, headers: kIsWeb ? null : const {'User-Agent': 'EquiPressure/1.0 (com.equipressure.app)'})
          .timeout(_timeout);
      if (response.statusCode != 200) return (null, false);

      final decoded = jsonDecode(response.body);
      if (decoded is! List || decoded.isEmpty) return (null, true);
      final first = decoded.first as Map<String, dynamic>;
      final lat = double.tryParse(first['lat']?.toString() ?? '');
      final lon = double.tryParse(first['lon']?.toString() ?? '');
      return (lat == null || lon == null ? null : LatLng(lat, lon), true);
    } catch (_) {
      return (null, false);
    }
  }

  /// Führt Anfragen nacheinander mit mindestens [_minRequestInterval] Abstand aus.
  Future<T> _throttled<T>(Future<T> Function() request) {
    final run = _requestQueue.then((_) => request());
    _requestQueue = run.then((_) => Future.delayed(_minRequestInterval), onError: (_) {
      return Future.delayed(_minRequestInterval);
    });
    return run;
  }

  Future<Map<String, List<double>?>> _loadCache() async {
    if (_cache != null) return _cache!;
    try {
      final preferences = await SharedPreferences.getInstance();
      final raw = preferences.getString(_cacheKey);
      final decoded = raw == null ? const {} : jsonDecode(raw) as Map<String, dynamic>;
      _cache = decoded.map((key, value) => MapEntry(
            key,
            value is List ? value.map((e) => (e as num).toDouble()).toList() : null,
          ));
    } catch (_) {
      _cache = {};
    }
    return _cache!;
  }

  Future<void> _saveCache(Map<String, List<double>?> cache) async {
    try {
      final preferences = await SharedPreferences.getInstance();
      await preferences.setString(_cacheKey, jsonEncode(cache));
    } catch (_) {}
  }
}
