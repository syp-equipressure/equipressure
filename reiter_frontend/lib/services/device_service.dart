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

  final Map<String, List<Device>> _cachedDevicesByUser = {};

  /// Lädt die Geräte eines Users
  Future<List<Device>> getDevices(String userId) async {
    final cachedDevices = _cachedDevicesByUser[userId];
    if (cachedDevices != null) {
      return cachedDevices;
    }

    try {
      final url = Uri.parse('$baseUrl/devices/$userId');
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode == 200) {
        final data = _extractDeviceList(jsonDecode(response.body));
        final devices = data
            .map((json) => Device.fromJson(json as Map<String, dynamic>))
            .toList();
        _cachedDevicesByUser[userId] = devices;
        return devices;
      } else if (response.statusCode == 404) {
        return [];
      } else {
        throw Exception('Failed to load devices: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading devices: $e');
    }
  }

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

  static List<dynamic> _extractDeviceList(dynamic decoded) {
    if (decoded is List) return decoded;
    if (decoded is Map<String, dynamic> && decoded['devices'] is List) {
      return decoded['devices'] as List<dynamic>;
    }
    if (decoded is Map<String, dynamic>) {
      for (final value in decoded.values) {
        if (value is List) return value;
      }
    }
    throw const FormatException('Invalid device list response');
  }

  /// Lädt ein einzelnes Gerät mit Details
  static Future<Map<String, dynamic>> getDeviceDetails(String deviceId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId');
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception(
          'Failed to load device details: ${response.statusCode}',
        );
      }
    } catch (e) {
      throw Exception('Error loading device details: $e');
    }
  }

  /// Lädt alle Geräte einer Person (inkl. Owner-Info)
  static Future<List<Map<String, dynamic>>> getPersonDevices(
    String personId,
  ) async {
    final devices = await DeviceService().getDevices(personId);
    return devices.map((device) => device.toJson()).toList();
  }

  /// Lädt Gruppenmitglieder für ein Gerät
  static Future<List<GroupMember>> getGroupMembers(String deviceId) async {
    try {
      final usersUrl = Uri.parse('$baseUrl/devices/$deviceId/users');
      final ownerUrl = Uri.parse('$baseUrl/devices/$deviceId/owner');
      final responses = await Future.wait([
        http
            .get(usersUrl)
            .timeout(
              const Duration(seconds: 10),
              onTimeout: () => throw Exception('Request timeout'),
            ),
        http
            .get(ownerUrl)
            .timeout(
              const Duration(seconds: 10),
              onTimeout: () => throw Exception('Request timeout'),
            ),
      ]);
      final usersResponse = responses[0];
      final ownerResponse = responses[1];

      if (usersResponse.statusCode != 200) {
        throw Exception(
          'Failed to load device users: ${usersResponse.statusCode}',
        );
      }
      if (ownerResponse.statusCode != 200) {
        throw Exception(
          'Failed to load device owner: ${ownerResponse.statusCode}',
        );
      }

      final owner = jsonDecode(ownerResponse.body) as Map<String, dynamic>;
      final ownerId = (owner['id'] ?? owner['person_id'] ?? owner['personId'])
          .toString();
      final decodedUsers = jsonDecode(usersResponse.body);
      final List<dynamic> users;
      if (decodedUsers is List) {
        users = decodedUsers;
      } else if (decodedUsers is Map<String, dynamic>) {
        users =
            (decodedUsers['persons'] ??
                    decodedUsers['users'] ??
                    decodedUsers['members'] ??
                    [])
                as List<dynamic>;
      } else {
        throw const FormatException('Invalid device users response');
      }

      final members = users.map((json) {
        final member = GroupMember.fromJson(json as Map<String, dynamic>);
        return GroupMember(
          id: member.id,
          name: member.name,
          imageUrl: member.imageUrl,
          isCurrentUser: member.isCurrentUser,
          isOwner: member.id == ownerId,
        );
      }).toList();
      if (!members.any((member) => member.id == ownerId)) {
        members.add(
          GroupMember(
            id: ownerId,
            name: GroupMember.fromJson(owner).name,
            imageUrl: (owner['imageUrl'] ?? owner['image_url']) as String?,
            isCurrentUser: false,
            isOwner: true,
          ),
        );
      }
      return members;
    } catch (e) {
      throw Exception('Error loading group members: $e');
    }
  }
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

  /// Lädt die Geräte des aktuellen Users
  static Future<List<Device>> loadUserDevices(String userId) {
    return DeviceService().getDevices(userId);
  }

  /// Fügt ein neues Gerät hinzu.
  static Future<Device> addDevice({
    required String deviceId,
    required int ownerId,
    required int categoryId,
  }) async {
    required String deviceId,
    required int ownerId,
    required int categoryId,
  }) async {
    try {
      final url = Uri.parse('$baseUrl/devices');
      final response = await http
          .post(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({
              'deviceId': deviceId,
              'ownerId': ownerId,
              'categoryId': categoryId,
            }),
          )
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode == 201 || response.statusCode == 200) {
        DeviceService().clearCache();
        return;
      }
      _throwForStatus(response, 'create device $deviceId');
    } catch (e) {
      throw Exception('Error adding device: $e');
    }
    }
    _throwForStatus(response, 'create device $deviceId');
  }

  /// Fügt ein Mitglied zu einer Device-Gruppe hinzu
  static Future<void> addGroupMember(String deviceId, String personId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId/add/$personId');
      final response = await http
          .post(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode != 201 && response.statusCode != 200) {
        throw Exception('Failed to add group member: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error adding group member: $e');
    }
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

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    if (response.statusCode == 404) {
      return const [];
    }
    _throwForStatus(response, 'load devices of person $personId');
  }

  /// Entfernt ein Mitglied aus einer Device-Gruppe
  static Future<void> removeGroupMember(
    String deviceId,
    String memberId,
  ) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId/remove/$memberId');
      final response = await http
          .delete(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode != 200 && response.statusCode != 204) {
        throw Exception(
          'Failed to remove group member: ${response.statusCode}',
        );
      }
    } catch (e) {
      throw Exception('Error removing group member: $e');
    }
  }

  static Future<void> changeDeviceOwner(String deviceId, String userId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId/owner/$userId');
      final response = await http
          .patch(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode != 200) {
        throw Exception(
          'Failed to change device owner: ${response.statusCode}',
        );
      }
    } catch (e) {
      throw Exception('Error changing device owner: $e');
    }
  }

  /// Cache leeren
  void clearCache() {
    _cachedDevicesByUser.clear();
  }
}

  }
}
