import 'package:shared_preferences/shared_preferences.dart';

class BackendPersonContextService {
  static const String _personIdKey = 'backend_person_id';

  static Future<String?> loadPersonId() async {
    final preferences = await SharedPreferences.getInstance();
    final personId = preferences.getString(_personIdKey);
    return personId != null && RegExp(r'^[1-9]\d*$').hasMatch(personId)
        ? personId
        : null;
  }

  static Future<bool> savePersonId(String value) async {
    final personId = value.trim();
    if (!RegExp(r'^[1-9]\d*$').hasMatch(personId)) return false;

    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(_personIdKey, personId);
    return true;
  }

  static Future<void> clearPersonId() async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.remove(_personIdKey);
  }
}
