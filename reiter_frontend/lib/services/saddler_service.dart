import 'dart:convert';
import 'package:reiterappfrontend/services/api_client.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/services/geocoding_service.dart';
import '../models/saddler.dart';

class SaddlerService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 5);

  static final SaddlerService _instance = SaddlerService._internal();
  factory SaddlerService() => _instance;
  SaddlerService._internal();

  List<Saddler>? _cachedSaddlers;
  int? _cachedPersonId;
  Set<String> _favoriteIds = {};

  Future<int> _currentPersonId() async {
    final personId = await BackendPersonContextService.loadPersonId() ??
        AuthService().currentUserId()?.toString();
    final parsedId = int.tryParse(personId ?? '');
    if (parsedId == null) throw StateError('No backend person is selected.');
    return parsedId;
  }

  Future<Set<String>> _fetchFavoriteIds(int personId) async {
    final url = Uri.parse('$baseUrl/persons/$personId/favourites');
    final response = await apiHttp.get(url).timeout(_timeout);
    if (response.statusCode == 404) return {};
    if (response.statusCode != 200) {
      throw Exception('Failed to load favorites: HTTP ${response.statusCode}');
    }
    return _extractList(jsonDecode(response.body))
        .map((saddler) => saddler['id'].toString())
        .toSet();
  }

  /// Lädt Sattler und Favoriten aus dem Backend.
  Future<List<Saddler>> getSaddlers() async {
    final personId = await _currentPersonId();
    if (_cachedSaddlers != null && _cachedPersonId == personId) {
      return _cachedSaddlers!;
    }

    final results = await Future.wait([
      fetchSaddlersFromBackend(),
      _fetchFavoriteIds(personId),
    ]);
    _favoriteIds = results[1] as Set<String>;
    _cachedPersonId = personId;
    _cachedSaddlers = await Future.wait((results[0] as List<Saddler>).map((saddler) async {
      final location = await GeocodingService().geocode(
        street: saddler.street,
        postalCode: saddler.postalCode,
        city: saddler.city,
      );
      return saddler.copyWith(
        isFavorite: _favoriteIds.contains(saddler.id),
        latitude: location?.latitude,
        longitude: location?.longitude,
      );
    }));
    return _cachedSaddlers!;
  }

  Future<List<Saddler>> fetchSaddlersFromBackend() async {
    final url = Uri.parse('$baseUrl/persons/saddlers');
    final response = await apiHttp.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      final decoded = jsonDecode(response.body);
      final list = _extractList(decoded);
      return list.map(_saddlerFromBackend).toList();
    }
    if (response.statusCode == 404) return const [];
    throw Exception('Failed to load saddlers: HTTP ${response.statusCode}');
  }

  Future<List<Saddler>> getSaddlersWithLocation() async {
    final saddlers = await getSaddlers();
    return saddlers.where((s) => s.hasLocation).toList();
  }

  Future<List<Saddler>> getFavorites() async {
    final saddlers = await getSaddlers();
    return saddlers.where((s) => s.isFavorite).toList();
  }

  Future<Saddler?> getSaddlerById(String id) async {
    final saddlers = await getSaddlers();
    try {
      return saddlers.firstWhere((s) => s.id == id);
    } catch (e) {
      return null;
    }
  }

  Future<void> toggleFavorite(String saddlerId) async {
    final personId = await _currentPersonId();
    final saddlerIdNum = int.tryParse(saddlerId);
    if (saddlerIdNum == null) {
      throw FormatException('Invalid saddler ID: $saddlerId');
    }
    final isFavNow = !_favoriteIds.contains(saddlerId);
    final url = Uri.parse('$baseUrl/persons/$personId/favourites/$saddlerIdNum');
    final response = isFavNow
        ? await apiHttp.put(url).timeout(_timeout)
        : await apiHttp.delete(url).timeout(_timeout);
    if (response.statusCode != 200 && response.statusCode != 204) {
      throw Exception('Failed to update favorite: HTTP ${response.statusCode}');
    }

    if (isFavNow) {
      _favoriteIds.add(saddlerId);
    } else {
      _favoriteIds.remove(saddlerId);
    }
    if (_cachedSaddlers != null) {
      _cachedSaddlers = _cachedSaddlers!.map((saddler) {
        return saddler.id == saddlerId
            ? saddler.copyWith(isFavorite: isFavNow)
            : saddler;
      }).toList();
    }
  }

  bool isFavorite(String saddlerId) {
    return _favoriteIds.contains(saddlerId);
  }

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

  static Saddler _saddlerFromBackend(Map<String, dynamic> json) {
    final id = (json['id'] ?? '').toString();
    final name = '${json['firstName'] ?? ''} ${json['lastName'] ?? ''}'.trim();
    final city = (json['cityName'] ?? '').toString();
    final postalCode = (json['plz'] ?? '').toString();
    final street = json['addressName']?.toString();

    return Saddler(
      id: id,
      name: name.isNotEmpty ? name : 'Sattler',
      imagePath: null,
      street: street?.isNotEmpty == true ? street : null,
      postalCode: postalCode.isNotEmpty ? postalCode : null,
      city: city.isNotEmpty ? city : null,
      website: json['websiteLink']?.toString(),
      description: json['description']?.toString(),
    );
  }

  void clearCache() {
    _cachedSaddlers = null;
    _cachedPersonId = null;
    _favoriteIds = {};
  }
}
