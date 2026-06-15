/// Zentrale Stelle für "wer ist gerade eingeloggt?".
///
/// Aktuell liefert es eine hartcodierte Demo-userId (1), damit Screens
/// schon jetzt gegen das Backend laufen können ohne dass ein echter Login
/// existiert.
///
/// Sobald JWT/Login implementiert ist, müssen nur die drei Methoden
/// `currentUserId()`, `authHeaders()` und `setToken()` angefasst werden —
/// der Rest der App (alle Screens und Services) bleibt unverändert.
class AuthService {
  // Singleton
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  /// Demo-userId solange noch kein Login da ist. Muss eine Person im Backend
  /// mit dieser Id sein (oder lokaler JSON-Fallback greift).
  static const int demoUserId = 1;

  /// Aktuell gespeicherter JWT-Token. Solange Login fehlt: null.
  String? _token;

  /// Liefert die ID des aktuell eingeloggten Users.
  ///
  /// TODO: sobald JWT live ist:
  ///   1. Token aus persistentem Storage (SharedPreferences) lesen
  ///   2. Mit dem `jwt_decoder` package das Payload dekodieren
  ///   3. den `sub` claim als int returnen
  int? currentUserId() {
    // Solange kein Login: Demo-User zurückgeben.
    if (_token == null) return demoUserId;

    // Platzhalter für späteren Token-Decode:
    // final payload = JwtDecoder.decode(_token!);
    // return int.tryParse(payload['sub']?.toString() ?? '');
    return demoUserId;
  }

  /// Authorization-Header für HTTP-Calls. Aktuell leer.
  /// Sobald Login da ist: `{ 'Authorization': 'Bearer $_token' }`.
  Map<String, String> authHeaders() {
    if (_token == null) return const {};
    return {'Authorization': 'Bearer $_token'};
  }

  /// Wird vom Login-Flow aufgerufen sobald der JWT vom Backend kommt.
  void setToken(String? token) {
    _token = token;
  }

  bool get isLoggedIn => currentUserId() != null;
}
