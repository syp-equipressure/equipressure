import 'package:flutter_test/flutter_test.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/group_member.dart';

void main() {
  group('API DTO mapping', () {
    test('maps backend device identifiers to the app device model', () {
      final device = Device.fromJson({
        'deviceId': 'device-42',
        'categoryId': 3,
      });

      expect(device.id, 'device-42');
      expect(device.name, 'device-42');
      expect(device.number, 'device-42');
    });

    test('maps backend person names and numeric IDs to a group member', () {
      final member = GroupMember.fromJson({
        'id': 42,
        'firstName': 'Anna',
        'lastName': 'Beispiel',
      });

      expect(member.id, '42');
      expect(member.name, 'Anna Beispiel');
    });
  });
}
