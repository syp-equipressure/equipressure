class ApiConfig {
  /// Backend, auf Android (Emulator und Handy per USB) über localhost weitergeleitet.
  /// Dafür nach jedem Verbinden bzw. Emulator-Start:
  ///   adb reverse tcp:5200 tcp:5200
  ///   adb reverse tcp:8180 tcp:8180
  static const String baseUrl = 'http://localhost:5200/api';

  /// Keycloak (siehe backend/keycloak), ebenfalls über localhost, damit die Weiterleitung
  /// von Google/Microsoft/GitHub und der Issuer im Token passen
  static const String keycloakIssuer = 'http://localhost:8180/realms/equipressure';
  static const String keycloakClientId = 'reiter-app';
  static const String keycloakRedirectUrl = 'at.equipressure.reiter:/oauthredirect';
}
