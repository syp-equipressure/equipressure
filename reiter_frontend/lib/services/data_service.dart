import 'package:flutter/services.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/models/user_data.dart';
import 'package:reiterappfrontend/services/person_service.dart';

class DataService {
  static const String baseUrl = 'http://localhost:5200';
  static const String apiVersion = '/api/v1';

  static Future<UserData> loadUserData() async {
    try {
      final jsonString = await rootBundle.loadString(
        'assets/data/personal_data.json',
      );
      final jsonData = jsonDecode(jsonString) as Map<String, dynamic>;
      return UserData.fromJson(jsonData['user']);
    } catch (e) {
      throw Exception('Error loading user data: $e');
    }
  }

  static Future<List<Device>> loadDevices() async {
    try {
      final jsonString = await rootBundle.loadString(
        'assets/data/devices.json',
      );
      final jsonData = jsonDecode(jsonString) as Map<String, dynamic>;
      final devicesList = jsonData['devices'] as List;
      return devicesList
          .map((device) => Device.fromJson(device as Map<String, dynamic>))
          .toList();
    } catch (e) {
      throw Exception('Error loading devices: $e');
    }
  }

  static Future<List<GroupMember>> loadGroupMembers(String deviceId) async {
    return PersonService.loadGroupMembers(deviceId);
  }

  /// Fetch all persons (Users with roles: Equestrian, Saddler, etc.)
  static Future<List<Map<String, dynamic>>> loadPersons() async {
    try {
      final url = Uri.parse('$baseUrl$apiVersion/persons');
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () {
              throw Exception('Request timeout');
            },
          );

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body) as List;
        return jsonData.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load persons: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading persons: $e');
    }
  }

  /// Fetch person profile data
  static Future<Map<String, dynamic>> loadPersonProfileData(
    String personId,
  ) async {
    try {
      final url = Uri.parse(
        '$baseUrl$apiVersion/persons/$personId/profile-data',
      );
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () {
              throw Exception('Request timeout');
            },
          );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception(
          'Failed to load person profile data: ${response.statusCode}',
        );
      }
    } catch (e) {
      throw Exception('Error loading person profile data: $e');
    }
  }

  /// Fetch person's location
  static Future<Map<String, dynamic>> loadPersonLocation(
    String personId,
  ) async {
    try {
      final url = Uri.parse('$baseUrl$apiVersion/persons/$personId/location');
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () {
              throw Exception('Request timeout');
            },
          );

      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      } else {
        throw Exception(
          'Failed to load person location: ${response.statusCode}',
        );
      }
    } catch (e) {
      throw Exception('Error loading person location: $e');
    }
  }

  /// Fetch person's devices
  static Future<List<Map<String, dynamic>>> loadPersonDevices(
    String personId,
  ) async {
    try {
      final url = Uri.parse('$baseUrl$apiVersion/persons/$personId/devices');
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () {
              throw Exception('Request timeout');
            },
          );

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body) as List;
        return jsonData.cast<Map<String, dynamic>>();
      } else {
        throw Exception(
          'Failed to load person devices: ${response.statusCode}',
        );
      }
    } catch (e) {
      throw Exception('Error loading person devices: $e');
    }
  }

  /// Fetch person's horses
  static Future<List<Map<String, dynamic>>> loadPersonHorses(
    String personId,
  ) async {
    try {
      final url = Uri.parse('$baseUrl$apiVersion/persons/$personId/horses');
      final response = await http
          .get(url)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () {
              throw Exception('Request timeout');
            },
          );

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body) as List;
        return jsonData.cast<Map<String, dynamic>>();
      } else {
        throw Exception('Failed to load person horses: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error loading person horses: $e');
    }
  }
}
