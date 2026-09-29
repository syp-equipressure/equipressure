import 'dart:convert';
import 'package:http/http.dart' as http;

/// Geworfen wenn das Backend einen erwarteten Fehlerstatus liefert.
class DeviceApiException implements Exception {
  final int statusCode;
  final String message;

  DeviceApiException(this.statusCode, this.message);

  bool get isNotFound => statusCode == 404;
  bool get isConflict => statusCode == 409;
  bool get isBadRequest => statusCode == 400;
  bool get isUnprocessable => statusCode == 422;

  @override
  String toString() => 'DeviceApiException($statusCode): $message';
}

/// Spricht den DeviceController (`api/devices`) des Backends an.
///
/// Routen 1:1 wie im Backend (Stand dev):
///   GET    /api/devices/{userId}                 -> DeviceListResponse  { "devices": [...] }
///   GET    /api/devices/{deviceId}/owner         -> PersonDto
///   GET    /api/devices/{deviceId}/users         -> PersonListResponse  { "persons": [...] }
///   POST   /api/devices                          (AddDeviceRequest)     201
///   POST   /api/devices/{deviceId}/add/{userId}                         201
///   DELETE /api/devices/{deviceId}/remove/{userId}                      200
class DeviceService {
  static const String baseUrl = 'http://localhost:5200/api';
  static const Duration _timeout = Duration(seconds: 10);
  static const Map<String, String> _jsonHeaders = {
    'Content-Type': 'application/json',
  };

  // Singleton
  static final DeviceService _instance = DeviceService._internal();
  factory DeviceService() => _instance;
  DeviceService._internal();

  // ---------------------------------------------------------------------------
  // Helper
  // ---------------------------------------------------------------------------

  /// Extrahiert die erste Liste aus einer *ListResponse, egal ob der Key
  /// "devices", "persons" o.ä. heißt. Akzeptiert auch eine rohe Liste.
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

  static Never _throwForStatus(http.Response r, String action) {
    throw DeviceApiException(r.statusCode, 'Failed to $action: HTTP ${r.statusCode}');
  }

  // ---------------------------------------------------------------------------
  // 1. GET /api/devices/{userId}  (DeviceListResponse)
  //    Achtung: Backend-Route erwartet die USER-Id (int), nicht die deviceId.
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> getDevicesByUserId(int userId) async {
    final url = Uri.parse('$baseUrl/devices/$userId');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    if (response.statusCode == 404) {
      return const [];
    }
    _throwForStatus(response, 'load devices for user $userId');
  }

  // ---------------------------------------------------------------------------
  // 2. GET /api/devices/{deviceId}/owner  (PersonDto)
  // ---------------------------------------------------------------------------
  static Future<Map<String, dynamic>?> getDeviceOwner(String deviceId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/owner');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    // 404 = Gerät nicht gefunden, 422 = Gerät hat keinen Owner
    if (response.statusCode == 404 || response.statusCode == 422) {
      return null;
    }
    _throwForStatus(response, 'load owner of device $deviceId');
  }

  // ---------------------------------------------------------------------------
  // 3. GET /api/devices/{deviceId}/users  (PersonListResponse)
  //    Das ist der korrekte Endpoint — frühere Versionen riefen fälschlich
  //    /devices/{deviceId}/members auf (existiert im Backend nicht).
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> getDeviceUsers(String deviceId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/users');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    // 404 = Gerät nicht gefunden, 422 = keine User zugewiesen
    if (response.statusCode == 404 || response.statusCode == 422) {
      return const [];
    }
    _throwForStatus(response, 'load users of device $deviceId');
  }

  // ---------------------------------------------------------------------------
  // 4. POST /api/devices  (AddDeviceRequest)
  //    Body: { "deviceId": string, "ownerId": int, "categoryId": int }
  //    201 Created | 400 BadRequest | 404 NotFound
  // ---------------------------------------------------------------------------
  static Future<void> createDevice({
    required String deviceId,
    required int ownerId,
    required int categoryId,
  }) async {
    final url = Uri.parse('$baseUrl/devices');
    final body = jsonEncode({
      'deviceId': deviceId,
      'ownerId': ownerId,
      'categoryId': categoryId,
    });
    final response =
        await http.post(url, headers: _jsonHeaders, body: body).timeout(_timeout);

    if (response.statusCode == 201) {
      return;
    }
    _throwForStatus(response, 'create device $deviceId');
  }

  // ---------------------------------------------------------------------------
  // 5. POST /api/devices/{deviceId}/add/{userId}
  //    201 Created | 404 NotFound | 409 Conflict (zu viele User) | 400
  // ---------------------------------------------------------------------------
  static Future<void> addUserToDevice(String deviceId, int userId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/add/$userId');
    final response = await http.post(url).timeout(_timeout);

    if (response.statusCode == 201) {
      return;
    }
    _throwForStatus(response, 'add user $userId to device $deviceId');
  }

  // ---------------------------------------------------------------------------
  // 6. DELETE /api/devices/{deviceId}/remove/{userId}
  //    200 OK | 404 NotFound | 409 Conflict (zu wenig User / Owner) | 400
  // ---------------------------------------------------------------------------
  static Future<void> removeUserFromDevice(String deviceId, int userId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/remove/$userId');
    final response = await http.delete(url).timeout(_timeout);

    if (response.statusCode == 200 || response.statusCode == 204) {
      return;
    }
    _throwForStatus(response, 'remove user $userId from device $deviceId');
  }

  // ---------------------------------------------------------------------------
  // Bonus: Geräte einer Person über den PersonController.
  //   GET /api/persons/{personId}/devices  (DeviceListResponse)
  // Liegt fachlich näher am PersonController, aber praktisch hier mit dabei.
  // ---------------------------------------------------------------------------
  static Future<List<Map<String, dynamic>>> getPersonDevices(int personId) async {
    final url = Uri.parse('$baseUrl/persons/$personId/devices');
    final response = await http.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    if (response.statusCode == 404) {
      return const [];
    }
    _throwForStatus(response, 'load devices of person $personId');
  }
}
