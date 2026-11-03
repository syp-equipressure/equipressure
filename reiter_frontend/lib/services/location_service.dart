import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/services/api_client.dart';

import 'package:reiterappfrontend/services/api_config.dart';

/// Geworfen wenn das Backend einen erwarteten Fehlerstatus liefert.
class LocationApiException implements Exception {
  final int statusCode;
  final String message;

  LocationApiException(this.statusCode, this.message);

  bool get isNotFound => statusCode == 404;
  bool get isBadRequest => statusCode == 400;

  @override
  String toString() => 'LocationApiException($statusCode): $message';
}

/// Spricht den LocationController (`api/locations`) des Backends an.
///
///   GET  /api/locations?length={n}&nameFilter={s}  -> [ AddressDto ]
///   POST /api/locations                            (AddressRequest)  201
///
/// AddressDto: { "address": string?, "cityName": string, "plz": string }
class LocationService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 10);
  static const Map<String, String> _jsonHeaders = {
    'Content-Type': 'application/json',
  };

  // Singleton
  static final LocationService _instance = LocationService._internal();
  factory LocationService() => _instance;
  LocationService._internal();

  static Never _throwForStatus(http.Response r, String action) {
    throw LocationApiException(r.statusCode, 'Failed to $action: HTTP ${r.statusCode}');
  }

  // ---------------------------------------------------------------------------
  // GET /api/locations  -> Liste von AddressDto
  //   [length]      optionales Limit
  //   [nameFilter]  optionaler Stadt-/Namensfilter
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> getCities({
    int? length,
    String? nameFilter,
  }) async {
    final query = <String, String>{};
    if (length != null) query['length'] = length.toString();
    if (nameFilter != null && nameFilter.isNotEmpty) query['nameFilter'] = nameFilter;

    final url = Uri.parse('$baseUrl/locations')
        .replace(queryParameters: query.isEmpty ? null : query);
    final response = await apiHttp.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      final decoded = jsonDecode(response.body);
      if (decoded is List) {
        return decoded.cast<Map<String, dynamic>>();
      }
      return const [];
    }
    if (response.statusCode == 404) {
      return const [];
    }
    _throwForStatus(response, 'load cities');
  }

  static Future<Map<String, dynamic>> getAddressById(int id) async {
    final url = Uri.parse('$baseUrl/locations/$id');
    final response = await apiHttp.get(url).timeout(_timeout);
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load address $id');
  }

  // ---------------------------------------------------------------------------
  // POST /api/locations  (AddressDto)
  //   Validierung im Backend: cityName + plz dürfen nicht leer sein.
  //   201 Created | 400 BadRequest
  // ---------------------------------------------------------------------------
  static Future<void> addAddress({
    String? address,
    required String cityName,
    required String plz,
  }) async {
    final url = Uri.parse('$baseUrl/locations');
    final body = jsonEncode({
      'addressName': address,
      'cityName': cityName,
      'plz': plz,
    });
    final response =
        await apiHttp.post(url, headers: _jsonHeaders, body: body).timeout(_timeout);

    if (response.statusCode == 201) {
      return;
    }
    _throwForStatus(response, 'add address');
  }
}
