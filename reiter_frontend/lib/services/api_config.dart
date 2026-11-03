/// Ohne Angabe verwendet die App die lokale Entwicklung, für den Server beim Bauen setzen:
///   flutter build apk --dart-define=API_URL=https://api.equipressure.com/api
///     --dart-define=KEYCLOAK_ISSUER=https://auth.equipressure.com/realms/equipressure
class ApiConfig {
  /// Backend, auf Android (Emulator und Handy per USB) über localhost weitergeleitet.
  /// Dafür nach jedem Verbinden bzw. Emulator-Start:
  ///   adb reverse tcp:5200 tcp:5200
  ///   adb reverse tcp:8180 tcp:8180
  static const String baseUrl = String.fromEnvironment(
    'API_URL',
    defaultValue: 'http://localhost:5200/api',
  );

  /// Keycloak (siehe backend/keycloak), ebenfalls über localhost, damit die Weiterleitung
  /// von Google/Microsoft/GitHub und der Issuer im Token passen
  static const String keycloakIssuer = String.fromEnvironment(
    'KEYCLOAK_ISSUER',
    defaultValue: 'http://localhost:8180/realms/equipressure',
  );

  /// Nur das lokale Keycloak läuft ohne HTTPS
  static final bool allowInsecureConnections = keycloakIssuer.startsWith('http://');

  static const String keycloakClientId = 'reiter-app';
  static const String keycloakRedirectUrl = 'at.equipressure.reiter:/oauthredirect';
}
