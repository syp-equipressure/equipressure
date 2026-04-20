import 'package:flutter/services.dart';
import 'dart:convert';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/models/user_data.dart';

class DataService {
  static Future<UserData> loadUserData() async {
    try {
      final jsonString = await rootBundle.loadString('assets/data/personal_data.json');
      final jsonData = jsonDecode(jsonString) as Map<String, dynamic>;
      return UserData.fromJson(jsonData['user']);
    } catch (e) {
      throw Exception('Error loading user data: $e');
    }
  }

  static Future<List<Device>> loadDevices() async {
    try {
      final jsonString = await rootBundle.loadString('assets/data/devices.json');
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
    try {
      final jsonString =
          await rootBundle.loadString('assets/data/device_groups.json');
      final jsonData = jsonDecode(jsonString) as Map<String, dynamic>;
      final groups = jsonData['groups'] as List;

      // Find the group for this device
      final groupData = groups.firstWhere(
        (group) => group['deviceId'] == deviceId,
        orElse: () => {'members': []},
      );

      final membersList = groupData['members'] as List? ?? [];
      return membersList
          .map((member) => GroupMember.fromJson(member as Map<String, dynamic>))
          .toList();
    } catch (e) {
      throw Exception('Error loading group members: $e');
    }
  }
}
