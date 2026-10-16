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
}
