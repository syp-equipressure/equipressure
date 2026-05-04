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

  List<Device>? _cachedDevices;

  /// Lädt alle Geräte
  Future<List<Device>> getDevices() async {
    if (_cachedDevices != null) {
      return _cachedDevices!;
    }

    try {
      final url = Uri.parse('$baseUrl/devices');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        _cachedDevices = data.map((json) => Device.fromJson(json as Map<String, dynamic>)).toList();
        return _cachedDevices!;
      } else {
        throw Exception('Failed to load devices: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading devices: $e');
    }
  }

  /// Lädt ein einzelnes Gerät mit Details
  static Future<Map<String, dynamic>> getDeviceDetails(String deviceId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception('Failed to load device details: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading device details: $e');
    }
  }

  /// Lädt alle Geräte einer Person (inkl. Owner-Info)
  static Future<List<Map<String, dynamic>>> getPersonDevices(String personId) async {
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

  /// Lädt Gruppenmitglieder für ein Gerät
  static Future<List<GroupMember>> getGroupMembers(String deviceId) async {
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

  /// Lädt die Geräte des aktuellen Users
  static Future<List<Device>> loadCurrentUserDevices() async {
    try {
      final url = Uri.parse('$baseUrl/users/current/devices');
      final response = await http.get(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((device) => Device.fromJson(device as Map<String, dynamic>)).toList();
      } else {
        throw Exception('Failed to load current user devices: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading current user devices: $e');
    }
  }

  /// Fügt ein neues Gerät hinzu
  static Future<Device> addDevice(Map<String, dynamic> deviceData) async {
    try {
      final url = Uri.parse('$baseUrl/devices');
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(deviceData),
      ).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 201 || response.statusCode == 200) {
        return Device.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
      } else {
        throw Exception('Failed to add device: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error adding device: $e');
    }
  }

  /// Aktualisiert ein Gerät
  static Future<Device> updateDevice(String deviceId, Map<String, dynamic> deviceData) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId');
      final response = await http.patch(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(deviceData),
      ).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode == 200) {
        return Device.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
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
      final response = await http.delete(url).timeout(
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
      final url = Uri.parse('$baseUrl/devices/$deviceId/members');
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'personId': personId}),
      ).timeout(
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
  static Future<void> removeGroupMember(String deviceId, String memberId) async {
    try {
      final url = Uri.parse('$baseUrl/devices/$deviceId/members/$memberId');
      final response = await http.delete(url).timeout(
        const Duration(seconds: 10),
        onTimeout: () => throw Exception('Request timeout'),
      );

      if (response.statusCode != 200 && response.statusCode != 204) {
        throw Exception('Failed to remove group member: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error removing group member: $e');
    }
  }

  /// Cache leeren
  void clearCache() {
    _cachedDevices = null;
  }
}
