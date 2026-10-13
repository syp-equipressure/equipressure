import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/group_member.dart';

class DeviceService {
  static const String baseUrl = 'http://localhost:5200/api';

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

  static List<dynamic> _extractDeviceList(dynamic decoded) {
    if (decoded is List) return decoded;
    if (decoded is Map<String, dynamic> && decoded['devices'] is List) {
      return decoded['devices'] as List<dynamic>;
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

  /// Lädt die Geräte des aktuellen Users
  static Future<List<Device>> loadUserDevices(String userId) {
    return DeviceService().getDevices(userId);
  }

  /// Fügt ein neues Gerät hinzu
  static Future<Device> addDevice(Map<String, dynamic> deviceData) async {
    try {
      final url = Uri.parse('$baseUrl/devices');
      final response = await http
          .post(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode(deviceData),
          )
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode == 201 || response.statusCode == 200) {
        return Device.fromJson(
          jsonDecode(response.body) as Map<String, dynamic>,
        );
      } else {
        throw Exception('Failed to add device: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error adding device: $e');
    }
  }

  /// Aktualisiert ein Gerät
  static Future<Device> updateDevice(
    String deviceId,
    Map<String, dynamic> deviceData,
  ) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId');
      final response = await http
          .patch(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode(deviceData),
          )
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode == 200) {
        return Device.fromJson(
          jsonDecode(response.body) as Map<String, dynamic>,
        );
      } else {
        throw Exception('Failed to update device: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error updating device: $e');
    }
  }

  /// Löscht ein Gerät
  static Future<void> deleteDevice(String deviceId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId');
      final response = await http
          .delete(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw Exception('Request timeout'),
          );

      if (response.statusCode != 200 && response.statusCode != 204) {
        throw Exception('Failed to delete device: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error deleting device: $e');
    }
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

      if (response.statusCode != 200 && response.statusCode != 201) {
        throw Exception('Failed to add group member: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error adding group member: $e');
    }
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
