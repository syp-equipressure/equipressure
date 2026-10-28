import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'api_config.dart';

/// Zentrale Stelle für "wer ist gerade eingeloggt?".
///
/// Login über Keycloak (Google, Microsoft, GitHub). Das Backend prüft nur das Access Token,
/// das bei jedem Request über [authHeaders] mitgeschickt wird.
class AuthService {
  // Singleton
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  static const String _refreshTokenKey = 'refresh_token';
  static const String _idTokenKey = 'id_token';

  final FlutterAppAuth _appAuth = const FlutterAppAuth();
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  String? _accessToken;
  DateTime? _accessTokenExpiry;
  String? _refreshToken;
  String? _idToken;
  int? _personId;

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

  /// Öffnet den Login im Browser. Mit [identityProvider] (google, microsoft, github) geht es direkt
  /// zum Anbieter, ohne die Keycloak-Seite.
  Future<void> login({String? identityProvider}) async {
    final result = await _appAuth.authorizeAndExchangeCode(
      AuthorizationTokenRequest(
        ApiConfig.keycloakClientId,
        ApiConfig.keycloakRedirectUrl,
        issuer: ApiConfig.keycloakIssuer,
        scopes: const ['openid', 'profile', 'email', 'offline_access'],
        additionalParameters: identityProvider != null
            ? {'kc_idp_hint': identityProvider}
            : null,
        // lokales Keycloak läuft ohne HTTPS
        allowInsecureConnections: true,
      ),
    );
    await _save(result);
  }

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
          allowInsecureConnections: true,
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
        allowInsecureConnections: true,
      ),
    );
    await _save(result);
  }

  Future<void> _save(TokenResponse result) async {
    _accessToken = result.accessToken;
    _accessTokenExpiry = result.accessTokenExpirationDateTime;
    _refreshToken = result.refreshToken ?? _refreshToken;
    _idToken = result.idToken ?? _idToken;
    await _storage.write(key: _refreshTokenKey, value: _refreshToken);
    await _storage.write(key: _idTokenKey, value: _idToken);
  }

  Future<void> _clear() async {
    _accessToken = null;
    _accessTokenExpiry = null;
    _refreshToken = null;
    _idToken = null;
    _personId = null;
    await _storage.delete(key: _refreshTokenKey);
    await _storage.delete(key: _idTokenKey);
  }
}
