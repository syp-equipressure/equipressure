import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/services/api_client.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';

/// Wo der eingeloggte User im Onboarding steht
enum AccountState { noProfile, noDevice, ready }

/// Name und E-Mail aus dem Konto (Google/Microsoft/GitHub), zum Vorausfüllen des Profils
class AccountIdentity {
  final String firstName;
  final String lastName;
  final String email;

  const AccountIdentity(this.firstName, this.lastName, this.email);
}

/// Inhalt des QR-Codes auf dem Satteldruckmessgerät: `Seriennummer;Kategorie-Id`,
/// z.B. "EP0707;2". Ohne Kategorie wird die Basic-Version angenommen.
class DeviceQrCode {
  // TODO: Kategorie fix vom Gerät übernehmen, sobald die Geräte vorab im Backend angelegt werden
  static const int defaultCategoryId = 1;

  final String serialNumber;
  final int categoryId;

  const DeviceQrCode(this.serialNumber, this.categoryId);

  static DeviceQrCode? tryParse(String raw) {
    final parts = raw.trim().split(';');
    final serialNumber = parts.first.trim();
    if (serialNumber.isEmpty) return null;
    final categoryId = parts.length > 1
        ? int.tryParse(parts[1].trim())
        : defaultCategoryId;
    if (categoryId == null || categoryId <= 0) return null;
    return DeviceQrCode(serialNumber, categoryId);
  }
}

/// Geworfen wenn das Backend einen Fehlerstatus liefert, z.B. 409 wenn das Gerät schon vergeben ist
class AccountApiException implements Exception {
  final int statusCode;
  final String message;

  AccountApiException(this.statusCode, this.message);

  bool get isConflict => statusCode == 409;
  bool get isBadRequest => statusCode == 400;

  @override
  String toString() => 'AccountApiException($statusCode): $message';
}

/// Person des eingeloggten Users: Profil anlegen und Gerät claimen nach dem ersten Login
class AccountService {
  static const Duration _timeout = Duration(seconds: 10);

  /// Lädt die Person des eingeloggten Users und sagt, ob das Onboarding abgeschlossen ist
  static Future<AccountState> loadState() async {
    final response = await apiHttp.get(_url('/account/me')).timeout(_timeout);

    if (response.statusCode == 404) return AccountState.noProfile;
    final me = _decode(response, 'load account') as Map<String, dynamic>;
    await _setPerson(me);
    final roles = (me['roles'] as List<dynamic>? ?? const []);
    return roles.isEmpty ? AccountState.noDevice : AccountState.ready;
  }

  static Future<AccountIdentity> loadIdentity() async {
    final response = await apiHttp
        .get(_url('/account/identity'))
        .timeout(_timeout);
    final identity = _decode(response, 'load identity') as Map<String, dynamic>;
    return AccountIdentity(
      identity['firstName'] as String? ?? '',
      identity['lastName'] as String? ?? '',
      identity['email'] as String? ?? '',
    );
  }

  static Future<void> createAccount({
    required String firstName,
    required String lastName,
    required DateTime dateOfBirth,
    required double height,
    required double weight,
    required String street,
    required String plz,
    required String city,
  }) async {
    final body = jsonEncode({
      'firstName': firstName,
      'lastName': lastName,
      'dateOfBirth': _formatDate(dateOfBirth),
      'height': height,
      'weight': weight,
      'address': {'addressName': street, 'cityName': city, 'plz': plz},
    });
    final response = await apiHttp
        .post(_url('/account'), headers: _jsonHeaders, body: body)
        .timeout(_timeout);
    await _setPerson(
      _decode(response, 'create account') as Map<String, dynamic>,
    );
  }

  /// Claimt das Gerät: man wird Besitzer:in und bekommt die Rolle der Gerätekategorie
  static Future<void> claimDevice(DeviceQrCode code) async {
    final serialNumber = Uri.encodeComponent(code.serialNumber);
    final response = await apiHttp
        .post(
          _url('/devices/$serialNumber/claim'),
          headers: _jsonHeaders,
          body: jsonEncode({'categoryId': code.categoryId}),
        )
        .timeout(_timeout);
    _decode(response, 'claim device');
  }

  static Uri _url(String path) => Uri.parse('${ApiConfig.baseUrl}$path');

  static const Map<String, String> _jsonHeaders = {
    'Content-Type': 'application/json',
  };

  static Object? _decode(http.Response response, String action) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return response.body.isEmpty ? null : jsonDecode(response.body);
    }
    throw AccountApiException(
      response.statusCode,
      'Failed to $action: ${response.body}',
    );
  }

  /// Alle bestehenden Screens laden ihre Daten für diese Person
  static Future<void> _setPerson(Map<String, dynamic> me) async {
    final id = me['id'] as int;
    AuthService().setPersonId(id);
    await BackendPersonContextService.savePersonId(id.toString());
  }

  static String _formatDate(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';
}
