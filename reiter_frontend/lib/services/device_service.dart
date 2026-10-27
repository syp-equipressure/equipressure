import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/services/api_client.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/group_member.dart';

import 'package:reiterappfrontend/services/api_config.dart';

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

/// Spricht den DeviceController (`api/devices`) & PersonController (`api/persons/{id}/devices`) des Backends an.
class DeviceService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 10);

  // Singleton
  static final DeviceService _instance = DeviceService._internal();
  factory DeviceService() => _instance;
  DeviceService._internal();

  final Map<String, List<Device>> _cachedDevicesByUser = {};

  static Never _throwForStatus(http.Response r, String action) {
    throw DeviceApiException(
      r.statusCode,
      'Failed to $action: HTTP ${r.statusCode}',
    );
  }

  /// Lädt die Geräte eines Users (`GET /api/persons/{userId}/devices` oder `GET /api/devices/{userId}`)
  Future<List<Device>> getDevices(String userId) async {
    final cachedDevices = _cachedDevicesByUser[userId];
    if (cachedDevices != null) {
      return cachedDevices;
    }

    try {
      final url = Uri.parse('$baseUrl/persons/$userId/devices');
      final response = await apiHttp.get(url).timeout(_timeout);

      if (response.statusCode == 200) {
        final data = _extractList(jsonDecode(response.body));
        final devices = data.map((json) => Device.fromJson(json)).toList();
        _cachedDevicesByUser[userId] = devices;
        return devices;
      } else if (response.statusCode == 404) {
        return [];
      }
    } catch (_) {
      // Fallback
    }

    return const [];
  }

  /// Lädt ein einzelnes Gerät mit Details
  static Future<Map<String, dynamic>> getDeviceDetails(String deviceId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId');
    final response = await apiHttp.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    _throwForStatus(response, 'load device details for $deviceId');
  }

  /// Lädt den Eigentümer eines Geräts (`GET /api/devices/{deviceId}/owner`)
  static Future<Map<String, dynamic>?> getDeviceOwner(String deviceId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/owner');
    final response = await apiHttp.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    if (response.statusCode == 404 || response.statusCode == 422) {
      return null;
    }
    _throwForStatus(response, 'load owner of device $deviceId');
  }

  /// Lädt die Benutzer eines Geräts (`GET /api/devices/{deviceId}/users`)
  static Future<List<Map<String, dynamic>>> getDeviceUsers(String deviceId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/users');
    final response = await apiHttp.get(url).timeout(_timeout);

    if (response.statusCode == 200) {
      return _extractList(jsonDecode(response.body));
    }
    if (response.statusCode == 404 || response.statusCode == 422) {
      return const [];
    }
    _throwForStatus(response, 'load users of device $deviceId');
  }

  /// Lädt Gruppenmitglieder für ein Gerät
  static Future<List<GroupMember>> getGroupMembers(String deviceId) async {
    try {
      final users = await getDeviceUsers(deviceId);
      final owner = await getDeviceOwner(deviceId);
      final ownerId = owner?['id']?.toString();

      final members = users.map((json) {
        final member = GroupMember.fromJson(json);
        return GroupMember(
          id: member.id,
          name: member.name,
          imageUrl: member.imageUrl,
          isCurrentUser: member.isCurrentUser,
          isOwner: member.id == ownerId,
        );
      }).toList();

      if (owner != null && ownerId != null && !members.any((m) => m.id == ownerId)) {
        final ownerMember = GroupMember.fromJson(owner);
        members.add(
          GroupMember(
            id: ownerId,
            name: ownerMember.name,
            imageUrl: ownerMember.imageUrl,
            isCurrentUser: false,
            isOwner: true,
          ),
        );
      }
      return members;
    } catch (e) {
      return const [];
    }
  }

  /// Fügt ein neues Gerät hinzu (`POST /api/devices`)
  static Future<Device?> addDevice({
    required String deviceId,
    required int ownerId,
    required int categoryId,
  }) async {
    final url = Uri.parse('$baseUrl/devices');
    final response = await apiHttp
        .post(
          url,
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({
            'deviceId': deviceId,
            'ownerId': ownerId,
            'categoryId': categoryId,
          }),
        )
        .timeout(_timeout);

    if (response.statusCode == 201 || response.statusCode == 200) {
      DeviceService().clearCache();
      return Device.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    _throwForStatus(response, 'create device $deviceId');
  }

  /// Fügt einen Nutzer zu einem Gerät hinzu (`POST /api/devices/{deviceId}/users/{userId}`)
  static Future<void> addUserToDevice(String deviceId, int userId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/users/$userId');
    final response = await apiHttp.post(url).timeout(_timeout);

    if (response.statusCode == 201 || response.statusCode == 200 || response.statusCode == 204) {
      return;
    }
    _throwForStatus(response, 'add user $userId to device $deviceId');
  }

  /// Entfernt einen Nutzer von einem Gerät (`DELETE /api/devices/{deviceId}/users/{userId}`)
  static Future<void> removeUserFromDevice(String deviceId, int userId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/users/$userId');
    final response = await apiHttp.delete(url).timeout(_timeout);

    if (response.statusCode == 200 || response.statusCode == 204) {
      return;
    }
    _throwForStatus(response, 'remove user $userId from device $deviceId');
  }

  /// Ändert den Eigentümer eines Geräts (`PATCH /api/devices/{deviceId}/owner/{userId}`)
  static Future<void> changeDeviceOwner(String deviceId, int userId) async {
    final url = Uri.parse('$baseUrl/devices/$deviceId/owner/$userId');
    final response = await apiHttp.patch(url).timeout(_timeout);

    if (response.statusCode == 200 || response.statusCode == 204) {
      return;
    }
    _throwForStatus(response, 'change owner of device $deviceId to $userId');
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

  void clearCache() {
    _cachedDevicesByUser.clear();
  }
}
