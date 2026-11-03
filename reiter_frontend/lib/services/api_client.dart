import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/services/auth_service.dart';

/// HTTP-Client für das Backend: schickt bei jedem Request das Keycloak-Token mit,
/// alle Endpoints außer dem Login brauchen eine angemeldete Person.
class ApiClient extends http.BaseClient {
  final http.Client _inner = http.Client();

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    request.headers.addAll(await AuthService().freshAuthHeaders());
    return _inner.send(request);
  }
}

/// Statt `http.get(...)` usw. für alle Aufrufe ans Backend verwenden
final http.Client apiHttp = ApiClient();
