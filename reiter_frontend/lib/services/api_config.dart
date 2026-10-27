import 'package:flutter/foundation.dart';

class ApiConfig {
  /// Dynamische Basis-URL:
  /// - Android-Emulator: http://10.0.2.2:5200/api (10.0.2.2 verweist auf den Host-PC)
  /// - Web / Windows Desktop / iOS Simulator: http://localhost:5200/api
  static String get baseUrl {
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5200/api';
    }
    return 'http://localhost:5200/api';
  }

  /// Keycloak (siehe backend/keycloak). Auch im Android-Emulator über localhost, damit die Weiterleitung
  /// von Google/Microsoft/GitHub und der Issuer im Token passen. Dafür einmal pro Emulator-Start:
  ///   adb reverse tcp:8180 tcp:8180
  static const String keycloakIssuer = 'http://localhost:8180/realms/equipressure';
  static const String keycloakClientId = 'reiter-app';
  static const String keycloakRedirectUrl = 'at.equipressure.reiter:/oauthredirect';
}
