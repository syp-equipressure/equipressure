import 'dart:async';
import 'dart:convert';
import 'package:web_socket_channel/web_socket_channel.dart';

/// Hält eine WebSocket-Verbindung offen und baut sie nach einem Abbruch
/// automatisch wieder auf (1 s, 2 s, 4 s … höchstens 30 s Pause).
///
/// Eingehende Nachrichten werden als JSON-Objekte über [events] geliefert.
class WebSocketService {
  static final WebSocketService _instance = WebSocketService._internal();
  factory WebSocketService() => _instance;
  WebSocketService._internal();

  static const Duration _maxReconnectDelay = Duration(seconds: 30);

  WebSocketChannel? _channel;
  StreamSubscription? _subscription;
  final _eventController = StreamController<Map<String, dynamic>>.broadcast();
  final _connectionController = StreamController<ConnectionState>.broadcast();

  String? _currentUrl;
  Future<Map<String, String>> Function()? _queryParameters;
  Timer? _reconnectTimer;
  Duration _reconnectDelay = const Duration(seconds: 1);
  bool _isConnected = false;

  /// Eingehende Ereignisse des Servers
  Stream<Map<String, dynamic>> get events => _eventController.stream;

  /// Änderungen des Verbindungszustands
  Stream<ConnectionState> get connectionStream => _connectionController.stream;

  bool get isConnected => _isConnected;

  /// Verbindet mit [url], eine bestehende Verbindung zu einer anderen URL wird
  /// vorher getrennt. [queryParameters] wird bei jedem Verbindungsaufbau neu
  /// geholt, z.B. für ein Access Token, das zwischendurch abläuft.
  Future<void> connect(
    String url, {
    Future<Map<String, String>> Function()? queryParameters,
  }) async {
    if (_currentUrl == url && (_isConnected || _reconnectTimer != null)) return;

    disconnect();
    _currentUrl = url;
    _queryParameters = queryParameters;
    await _open();
  }

  void disconnect() {
    _currentUrl = null;
    _queryParameters = null;
    _reconnectTimer?.cancel();
    _reconnectTimer = null;
    _subscription?.cancel();
    _subscription = null;
    _channel?.sink.close();
    _channel = null;
    _setConnected(false);
  }

  Future<void> _open() async {
    final url = _currentUrl;
    if (url == null) return;

    _connectionController.add(ConnectionState.connecting);
    try {
      var uri = Uri.parse(url);
      final queryParameters = await _queryParameters?.call();
      if (queryParameters != null && queryParameters.isNotEmpty) {
        uri = uri.replace(
          queryParameters: {...uri.queryParameters, ...queryParameters},
        );
      }
      final channel = WebSocketChannel.connect(uri);
      await channel.ready;
      if (_currentUrl != url) {
        channel.sink.close();
        return;
      }

      _channel = channel;
      _reconnectDelay = const Duration(seconds: 1);
      _setConnected(true);
      _subscription = channel.stream.listen(
        _onData,
        onError: (_) => _onClosed(),
        onDone: _onClosed,
      );
    } catch (_) {
      _onClosed();
    }
  }

  void _onData(dynamic data) {
    if (data is! String) return;
    try {
      final json = jsonDecode(data);
      if (json is Map<String, dynamic>) _eventController.add(json);
    } catch (_) {
      // keine gültige JSON-Nachricht
    }
  }

  void _onClosed() {
    _subscription?.cancel();
    _subscription = null;
    _channel = null;
    _setConnected(false);
    if (_currentUrl == null) return;

    _connectionController.add(ConnectionState.reconnecting);
    _reconnectTimer?.cancel();
    _reconnectTimer = Timer(_reconnectDelay, () {
      _reconnectTimer = null;
      _open();
    });
    final next = _reconnectDelay * 2;
    _reconnectDelay = next > _maxReconnectDelay ? _maxReconnectDelay : next;
  }

  void _setConnected(bool connected) {
    if (_isConnected == connected) return;
    _isConnected = connected;
    _connectionController.add(
      connected ? ConnectionState.connected : ConnectionState.disconnected,
    );
  }
}

enum ConnectionState {
  disconnected,
  connecting,
  connected,
  reconnecting,
}
