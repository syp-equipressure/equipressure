import 'dart:convert';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'api_config.dart';

/// E-Mail oder Passwort stimmen nicht.
class WrongCredentialsException implements Exception {}

/// Beim Registrieren: zu dieser E-Mail gibt es schon ein Konto.
class EmailAlreadyUsedException implements Exception {}

/// Beim Registrieren: das Backend begrenzt die Versuche pro Adresse.
class TooManyAttemptsException implements Exception {}

/// Zentrale Stelle für "wer ist gerade eingeloggt?".
///
/// Login über Keycloak (E-Mail und Passwort oder Google, Microsoft, GitHub). Das Backend prüft nur das Access Token,
/// das bei jedem Request über [authHeaders] mitgeschickt wird.
class AuthService {
  // Singleton
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  static const String _refreshTokenKey = 'refresh_token';
  static const String _idTokenKey = 'id_token';
  static const List<String> _scopes = ['openid', 'profile', 'email', 'offline_access'];

  final FlutterAppAuth _appAuth = const FlutterAppAuth();
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  String? _accessToken;
  DateTime? _accessTokenExpiry;
  String? _refreshToken;
  String? _idToken;
  int? _personId;
  bool _remember = true;

  bool get isLoggedIn => _accessToken != null;

  /// Beim App-Start: meldet mit dem gespeicherten Refresh Token wieder an (falls vorhanden)
  Future<void> init() async {
    _refreshToken = await _storage.read(key: _refreshTokenKey);
    _idToken = await _storage.read(key: _idTokenKey);
    if (_refreshToken == null) return;

    try {
      await _refresh();
    } catch (_) {
      // Refresh Token abgelaufen oder Keycloak nicht erreichbar -> neu einloggen
      await _clear();
    }
  }

  /// Meldet mit E-Mail und Passwort aus dem Login-Screen an (Direct Access Grant des Clients reiter-app).
  /// Ohne [remember] wird die Anmeldung nicht gespeichert und endet mit dem Schließen der App.
  /// Wirft [WrongCredentialsException], wenn die Daten nicht stimmen.
  Future<void> loginWithPassword(String email, String password, {required bool remember}) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.keycloakIssuer}/protocol/openid-connect/token'),
      body: {
        'client_id': ApiConfig.keycloakClientId,
        'grant_type': 'password',
        'username': email,
        'password': password,
        'scope': _scopes.join(' '),
      },
    ).timeout(const Duration(seconds: 10));

    final json = jsonDecode(response.body) as Map<String, dynamic>;
    if (json['error'] == 'invalid_grant') throw WrongCredentialsException();
    if (response.statusCode != 200) {
      throw Exception('Login failed: HTTP ${response.statusCode}');
    }

    _remember = remember;
    await _save(TokenResponse(
      json['access_token'] as String?,
      json['refresh_token'] as String?,
      DateTime.now().add(Duration(seconds: (json['expires_in'] as num).toInt())),
      json['id_token'] as String?,
      json['token_type'] as String?,
      null,
      null,
    ));
  }

  /// Anmelden über einen Anbieter (google, microsoft, github) im Browser der App.
  Future<void> loginWithProvider(String identityProvider) async {
    final result = await _appAuth.authorizeAndExchangeCode(
      AuthorizationTokenRequest(
        ApiConfig.keycloakClientId,
        ApiConfig.keycloakRedirectUrl,
        issuer: ApiConfig.keycloakIssuer,
        scopes: _scopes,
        additionalParameters: {'kc_idp_hint': identityProvider},
        allowInsecureConnections: ApiConfig.allowInsecureConnections,
      ),
    );
    await _save(result);
  }

  /// Erstellt ein neues Konto über das Backend (dort liegen die Rechte dafür) und meldet es an.
  Future<void> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
  }) async {
    final response = await http.post(
      Uri.parse('${ApiConfig.baseUrl}/account/register'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'password': password,
        'firstName': firstName,
        'lastName': lastName,
      }),
    ).timeout(const Duration(seconds: 15));

    switch (response.statusCode) {
      case 204:
        await loginWithPassword(email, password, remember: true);
      case 409:
        throw EmailAlreadyUsedException();
      case 429:
        throw TooManyAttemptsException();
      default:
        throw Exception('Registration failed: HTTP ${response.statusCode}');
    }
  }

  /// Keycloak-Seite für "Passwort vergessen".
  Uri get resetPasswordUrl => Uri.parse(
        '${ApiConfig.keycloakIssuer}/login-actions/reset-credentials'
        '?client_id=${ApiConfig.keycloakClientId}',
      );

  Future<void> logout() async {
    final idToken = _idToken;
    await _clear();
    if (idToken == null) return;

    try {
      await _appAuth.endSession(
        EndSessionRequest(
          idTokenHint: idToken,
          postLogoutRedirectUrl: ApiConfig.keycloakRedirectUrl,
          issuer: ApiConfig.keycloakIssuer,
          allowInsecureConnections: ApiConfig.allowInsecureConnections,
        ),
      );
    } catch (_) {
      // lokal ist man trotzdem abgemeldet
    }
  }

  /// Liefert die ID der Person des eingeloggten Users (gesetzt nach GET /api/account/me).
  int? currentUserId() => _personId;

  void setPersonId(int? personId) => _personId = personId;

  /// Authorization-Header für HTTP-Calls, leer wenn niemand eingeloggt ist.
  Map<String, String> authHeaders() {
    final token = _accessToken;
    if (token == null) return const {};
    return {'Authorization': 'Bearer $token'};
  }

  /// Wie [authHeaders], erneuert das Token vorher falls es (fast) abgelaufen ist.
  Future<Map<String, String>> freshAuthHeaders() async {
    await freshAccessToken();
    return authHeaders();
  }

  /// Aktuelles Access Token, z.B. für den Chat-WebSocket und Bilder im Chat,
  /// die keinen Authorization-Header mitschicken können
  String? get accessToken => _accessToken;

  /// Wie [accessToken], erneuert das Token vorher falls es (fast) abgelaufen ist.
  Future<String?> freshAccessToken() async {
    final expiry = _accessTokenExpiry;
    if (_refreshToken != null &&
        expiry != null &&
        expiry.isBefore(DateTime.now().add(const Duration(seconds: 30)))) {
      await _refresh();
    }
    return _accessToken;
  }

  Future<void> _refresh() async {
    final result = await _appAuth.token(
      TokenRequest(
        ApiConfig.keycloakClientId,
        ApiConfig.keycloakRedirectUrl,
        issuer: ApiConfig.keycloakIssuer,
        refreshToken: _refreshToken,
        allowInsecureConnections: ApiConfig.allowInsecureConnections,
      ),
    );
    await _save(result);
  }

  Future<void> _save(TokenResponse result) async {
    _accessToken = result.accessToken;
    _accessTokenExpiry = result.accessTokenExpirationDateTime;
    _refreshToken = result.refreshToken ?? _refreshToken;
    _idToken = result.idToken ?? _idToken;
    if (!_remember) return;
    await _storage.write(key: _refreshTokenKey, value: _refreshToken);
    await _storage.write(key: _idTokenKey, value: _idToken);
  }

  Future<void> _clear() async {
    _accessToken = null;
    _accessTokenExpiry = null;
    _refreshToken = null;
    _idToken = null;
    _personId = null;
    _remember = true;
    await _storage.delete(key: _refreshTokenKey);
    await _storage.delete(key: _idTokenKey);
  }
}
