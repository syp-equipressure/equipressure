import 'dart:async';
import 'dart:convert';
import 'package:web_socket_channel/web_socket_channel.dart';
import '../models/message.dart';

/// WebSocket service for real-time chat communication for later when we
/// want to implement it in backend (￣y▽,￣)╭
///
/// Usage:
/// 1. Call connect() with your backend WebSocket URL
/// 2. Listen to messageStream for incoming messages
/// 3. Use sendMessage() to send messages
/// 4. Call disconnect() when done
///
/// Example:
/// ```dart
/// final wsService = WebSocketService();
/// await wsService.connect('ws://your-backend-url/chat');
/// wsService.messageStream.listen((message) => print('Received: $message'));
/// wsService.sendMessage(message);
/// ```
class WebSocketService {
  static final WebSocketService _instance = WebSocketService._internal();
  factory WebSocketService() => _instance;
  WebSocketService._internal();

  WebSocketChannel? _channel;
  final _messageController = StreamController<Message>.broadcast();
  final _connectionController = StreamController<ConnectionState>.broadcast();

  bool _isConnected = false;
  String? _currentUrl;
  Timer? _reconnectTimer;
  int _reconnectAttempts = 0;
  static const int _maxReconnectAttempts = 5;

  /// Stream of incoming messages
  Stream<Message> get messageStream => _messageController.stream;

  /// Stream of connection state changes
  Stream<ConnectionState> get connectionStream => _connectionController.stream;

  /// Whether currently connected
  bool get isConnected => _isConnected;

  /// Connect to WebSocket server
  ///
  /// [url] - WebSocket URL (e.g., 'ws://localhost:8080/chat' or 'wss://api.example.com/chat')
  /// [token] - Optional auth token to send on connection
  Future<bool> connect(String url, {String? token}) async {
    if (_isConnected && _currentUrl == url) {
      return true;
    }

    try {
      disconnect();
      _currentUrl = url;

      // Add auth token to URL if provided
      final uri = token != null
          ? Uri.parse('$url?token=$token')
          : Uri.parse(url);

      _channel = WebSocketChannel.connect(uri);

      // Wait for connection to establish
      await _channel!.ready;

      _isConnected = true;
      _reconnectAttempts = 0;
      _connectionController.add(ConnectionState.connected);

      // Listen for incoming messages
      _channel!.stream.listen(
        _onMessage,
        onError: _onError,
        onDone: _onDone,
      );

      return true;
    } catch (e) {
      _isConnected = false;
      _connectionController.add(ConnectionState.error);
      return false;
    }
  }

  /// Send a message through WebSocket
  void sendMessage(Message message) {
    if (!_isConnected || _channel == null) {
      return;
    }

    final jsonData = jsonEncode(message.toJson());
    _channel!.sink.add(jsonData);
  }

  /// Send raw JSON data
  void sendRaw(Map<String, dynamic> data) {
    if (!_isConnected || _channel == null) {
      return;
    }

    _channel!.sink.add(jsonEncode(data));
  }

  /// Disconnect from WebSocket server
  void disconnect() {
    _reconnectTimer?.cancel();
    _channel?.sink.close();
    _channel = null;
    _isConnected = false;
    _connectionController.add(ConnectionState.disconnected);
  }

  void _onMessage(dynamic data) {
    try {
      final jsonData = jsonDecode(data as String);

      // Handle different message types from backend
      if (jsonData['type'] == 'message') {
        final message = Message.fromJson(jsonData['payload']);
        _messageController.add(message);
      } else if (jsonData['type'] == 'messages') {
        // Batch of messages (e.g., chat history)
        final messages = (jsonData['payload'] as List)
            .map((m) => Message.fromJson(m))
            .toList();
        for (final message in messages) {
          _messageController.add(message);
        }
      }
    } catch (e) {
      // Handle parse error
    }
  }

  void _onError(dynamic error) {
    _isConnected = false;
    _connectionController.add(ConnectionState.error);
    _attemptReconnect();
  }

  void _onDone() {
    _isConnected = false;
    _connectionController.add(ConnectionState.disconnected);
    _attemptReconnect();
  }

  void _attemptReconnect() {
    if (_reconnectAttempts >= _maxReconnectAttempts || _currentUrl == null) {
      return;
    }

    _reconnectTimer?.cancel();
    _reconnectAttempts++;

    // Exponential backoff: 1s, 2s, 4s, 8s, 16s
    final delay = Duration(seconds: 1 << (_reconnectAttempts - 1));

    _connectionController.add(ConnectionState.reconnecting);

    _reconnectTimer = Timer(delay, () {
      if (_currentUrl != null) {
        connect(_currentUrl!);
      }
    });
  }

  /// Dispose resources
  void dispose() {
    disconnect();
    _messageController.close();
    _connectionController.close();
  }
}

enum ConnectionState {
  disconnected,
  connecting,
  connected,
  reconnecting,
  error,
}
