import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/models/user_data.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';
import 'package:reiterappfrontend/services/device_service.dart';
import 'package:reiterappfrontend/services/person_service.dart';

class DataService {
  static Future<UserData> loadUserData([String? personId]) async {
    return PersonService.loadUserData(await _resolvePersonId(personId));
  }

  static Future<List<Device>> loadDevices() async {
    return DeviceService().getDevices(await _resolvePersonId(null));
  }

  static Future<String> _resolvePersonId(String? personId) async {
    final resolvedPersonId = personId ??
        await BackendPersonContextService.loadPersonId() ??
        AuthService().currentUserId()?.toString();
    if (resolvedPersonId == null) {
      throw StateError('No backend person is selected.');
    }
    return resolvedPersonId;
  }

  static Future<List<GroupMember>> loadGroupMembers(String deviceId) async {
    return DeviceService.getGroupMembers(deviceId);
  }

  /// Fetch person profile data
  static Future<Map<String, dynamic>> loadPersonProfileData(
    String personId,
  ) async {
    return PersonService.loadPersonProfileData(personId);
  }

  /// Fetch person's location
  static Future<Map<String, dynamic>> loadPersonLocation(
    String personId,
  ) async {
    return PersonService.loadPersonLocation(personId);
  }

  /// Fetch person's devices
  static Future<List<Map<String, dynamic>>> loadPersonDevices(
    String personId,
  ) async {
    return PersonService.loadPersonDevices(personId);
  }

  /// Fetch person's horses
  static Future<List<Map<String, dynamic>>> loadPersonHorses(
    String personId,
  ) async {
    return PersonService.loadPersonHorses(personId);
  }
}
