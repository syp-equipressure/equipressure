import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import '../models/chat.dart';
import '../models/message.dart';
import 'api_client.dart';
import 'api_config.dart';
import 'auth_service.dart';
import 'horse_service.dart';
import 'websocket_service.dart';

/// Eine neue Nachricht, die als Benachrichtigung angezeigt wird.
class ChatNotification {
  final Chat chat;
  final Message message;

  ChatNotification({required this.chat, required this.message});
}

/// Chat mit den Sattler*innen über das Backend.
///
/// Nachrichten werden per REST verschickt, neue, gelesene und hinzugefügte
/// Nachrichten kommen live über den Chat-WebSocket. Einen Login gibt es noch
/// nicht, die Person ist die eingestellte Backend-Person (sonst die Demo-Person).
class ChatService extends ChangeNotifier {
  static final ChatService _instance = ChatService._internal();
  factory ChatService() => _instance;
  ChatService._internal() {
    _webSocket.events.listen(_onEvent);
    _webSocket.connectionStream.listen((_) => notifyListeners());
  }

  static const Duration _timeout = Duration(seconds: 10);

  final WebSocketService _webSocket = WebSocketService();
  final _notificationController =
      StreamController<ChatNotification>.broadcast();

  String? _personId;
  List<Chat> _chats = [];
  final Map<String, List<Message>> _messages = {};
  bool _isLoading = false;
  String? _error;

  /// Der Chat, der gerade geöffnet ist. Seine Nachrichten werden nicht
  /// benachrichtigt, sondern sofort als gelesen markiert.
  String? activeContactId;

  String get _baseUrl => ApiConfig.baseUrl;

  /// Neue Nachrichten anderer Personen, außer im gerade geöffneten Chat
  Stream<ChatNotification> get notifications => _notificationController.stream;

  List<Chat> get chats => List.unmodifiable(_chats);
  bool get isLoading => _isLoading;
  bool get isConnected => _webSocket.isConnected;
  String? get error => _error;
  String? get personId => _personId;
  int get unreadTotal => _chats.fold(0, (sum, chat) => sum + chat.unreadCount);

  List<Message>? messagesWith(String contactId) => _messages[contactId];

  /// Verbindet den WebSocket und lädt die Unterhaltungen. Wird beim Start der
  /// App aufgerufen und erneut, falls sich die Backend-Person geändert hat.
  Future<void> start() async {
    final String personId;
    try {
      personId = await _currentPersonId();
    } on StateError {
      // ohne Backend-Person gibt es keinen Chat
      _error = 'Keine Person ausgewählt.';
      notifyListeners();
      return;
    }
    if (personId != _personId) {
      _personId = personId;
      _chats = [];
      _messages.clear();
      final wsBase = _baseUrl.replaceFirst(RegExp('^http'), 'ws');
      await _webSocket.connect(
        '$wsBase/persons/$personId/chats/ws',
        queryParameters: _accessTokenQuery,
      );
    }
    await refreshChats();
  }

  /// Lädt die Unterhaltungen. Nur Personen, mit denen schon geschrieben wurde,
  /// ein neuer Chat wird über das Profil der Sattler*in gestartet.
  Future<void> refreshChats() async {
    final personId = _personId;
    if (personId == null) return;

    _isLoading = true;
    notifyListeners();
    try {
      final response = await apiHttp
          .get(Uri.parse('$_baseUrl/persons/$personId/chats'))
          .timeout(_timeout);
      _chats = _decodeList(response, 'conversations')
          .map((json) => Chat.fromJson(json))
          .toList();
      _error = null;
    } catch (e) {
      _error = 'Nachrichten konnten nicht geladen werden.';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Der Chat mit einer Sattler*in, auch wenn es noch keine Nachrichten gibt.
  Chat chatWith(String contactId, String contactName) {
    for (final chat in _chats) {
      if (chat.contactId == contactId) return chat;
    }
    return Chat(contactId: contactId, contactName: contactName);
  }

  Future<List<Message>> loadMessages(String contactId) async {
    final personId = _personId ?? await _currentPersonId();
    final response = await apiHttp
        .get(Uri.parse('$_baseUrl/persons/$personId/chats/$contactId'))
        .timeout(_timeout);
    final messages = _decodeList(response, 'messages')
        .map((json) => Message.fromJson(json))
        .toList();
    _messages[contactId] = messages;
    notifyListeners();
    return messages;
  }

  Future<Message> sendText(String contactId, String text) =>
      _send(contactId, {'type': MessageType.text.json, 'text': text});

  Future<Message> shareHorse(String contactId, String horseId) => _send(
        contactId,
        {'type': MessageType.horse.json, 'horseId': int.parse(horseId)},
      );

  Future<Message> shareMeasurement(String contactId, String measurementId) =>
      _send(contactId, {
        'type': MessageType.measurement.json,
        'measurementGroupId': int.parse(measurementId),
      });

  /// Höchstgröße einer Datei, mehr nimmt das Backend nicht an
  static const int maxFileSize = 10 * 1024 * 1024;

  /// Schickt ein Bild oder eine Datei, optional mit Text.
  Future<Message> sendFile(
    String contactId, {
    required Uint8List bytes,
    required String fileName,
    String? text,
  }) async {
    final personId = _personId ?? await _currentPersonId();
    final request = http.MultipartRequest(
      'POST',
      Uri.parse('$_baseUrl/persons/$personId/chats/$contactId/files'),
    )..files.add(http.MultipartFile.fromBytes('file', bytes, filename: fileName));
    if (text != null && text.isNotEmpty) request.fields['text'] = text;

    final response = await http.Response.fromStream(
      await apiHttp.send(request).timeout(const Duration(seconds: 60)),
    );
    if (response.statusCode != 201) {
      throw Exception(_problemDetail(response) ??
          'Datei konnte nicht gesendet werden (HTTP ${response.statusCode}).');
    }

    final message = Message.fromJson(jsonDecode(response.body));
    _upsert(message);
    return message;
  }

  /// Adresse des Inhalts einer gesendeten Datei. Sender*in und Empfänger*in
  /// dürfen sie abrufen, daher geht notfalls auch die Id aus der Nachricht.
  /// Das Token steht in der Adresse, weil Bilder und Downloads keinen Header mitschicken.
  String fileUrl(Message message) {
    final personId = _personId ?? message.recipientId;
    final url = Uri.parse('$_baseUrl/persons/$personId/chats/messages/${message.id}/file');
    final token = AuthService().accessToken;
    return (token == null ? url : url.replace(queryParameters: {'access_token': token})).toString();
  }

  /// Beim WebSocket kann kein Authorization-Header mitgeschickt werden (im Browser)
  static Future<Map<String, String>> _accessTokenQuery() async {
    final token = await AuthService().freshAccessToken();
    return token == null ? const {} : {'access_token': token};
  }

  /// Markiert alle Nachrichten der Sattler*in als gelesen.
  Future<void> markAsRead(String contactId) async {
    final personId = _personId;
    final index = _chats.indexWhere((chat) => chat.contactId == contactId);
    if (personId == null || index < 0 || _chats[index].unreadCount == 0) return;

    _chats[index] = _chats[index].copyWith(unreadCount: 0);
    notifyListeners();
    try {
      await apiHttp
          .put(Uri.parse('$_baseUrl/persons/$personId/chats/$contactId/read'))
          .timeout(_timeout);
    } catch (_) {
      // beim nächsten Öffnen wird es erneut versucht
    }
  }

  Future<Message> _send(String contactId, Map<String, dynamic> body) async {
    final personId = _personId ?? await _currentPersonId();
    final response = await apiHttp
        .post(
          Uri.parse('$_baseUrl/persons/$personId/chats/$contactId'),
          headers: const {'Content-Type': 'application/json'},
          body: jsonEncode(body),
        )
        .timeout(_timeout);
    if (response.statusCode != 201) {
      throw Exception(_problemDetail(response) ??
          'Nachricht konnte nicht gesendet werden (HTTP ${response.statusCode}).');
    }

    final message = Message.fromJson(jsonDecode(response.body));
    // der WebSocket liefert die Nachricht ebenfalls, doppelt einfügen schadet nicht
    _upsert(message);
    return message;
  }

  void _onEvent(Map<String, dynamic> event) {
    final type = event['type'];
    if (type == 'message' || type == 'accepted') {
      final message = Message.fromJson(event['message']);
      final isNew = _upsert(message);
      if (type == 'message' && isNew && message.recipientId == _personId) {
        _onIncoming(message);
      }
    } else if (type == 'read') {
      final readerId = event['readerId'].toString();
      if (readerId == _personId) {
        // auf einem anderen Gerät gelesen
        final contactId = event['contactId'].toString();
        _updateChat(contactId, (chat) => chat.copyWith(unreadCount: 0));
      } else {
        _markSentAsRead(readerId);
      }
      notifyListeners();
    }
  }

  void _onIncoming(Message message) {
    final contactId = message.senderId;
    if (activeContactId == contactId) {
      markAsRead(contactId);
      return;
    }

    final index = _chats.indexWhere((chat) => chat.contactId == contactId);
    if (index >= 0) {
      _notificationController.add(
        ChatNotification(chat: _chats[index], message: message),
      );
      return;
    }
    // erste Nachricht einer neuen Person, die Liste braucht ihren Namen
    refreshChats().then((_) {
      final chat = _chats.where((chat) => chat.contactId == contactId);
      if (chat.isNotEmpty) {
        _notificationController
            .add(ChatNotification(chat: chat.first, message: message));
      }
    });
  }

  /// Fügt eine Nachricht ein oder ersetzt sie, true wenn sie neu war.
  bool _upsert(Message message) {
    final contactId =
        message.senderId == _personId ? message.recipientId : message.senderId;

    final loaded = _messages[contactId];
    final index = _chats.indexWhere((chat) => chat.contactId == contactId);
    final chat = index >= 0 ? _chats[index] : null;
    final isNew = !(loaded?.any((m) => m.id == message.id) ?? false) &&
        chat?.lastMessage?.id != message.id;

    if (loaded != null) {
      final position = loaded.indexWhere((m) => m.id == message.id);
      if (position < 0) {
        loaded.add(message);
      } else {
        loaded[position] = message;
      }
    }

    if (chat != null) {
      final isLast = isNew || chat.lastMessage?.id == message.id;
      _chats[index] = Chat(
        contactId: chat.contactId,
        contactName: chat.contactName,
        lastMessage: isLast ? message : chat.lastMessage,
        unreadCount: isNew && message.recipientId == _personId
            ? chat.unreadCount + 1
            : chat.unreadCount,
      );
      _sortChats();
    } else if (message.senderId == _personId) {
      refreshChats();
    }

    notifyListeners();
    return isNew;
  }

  void _markSentAsRead(String readerId) {
    final now = DateTime.now();
    final loaded = _messages[readerId];
    if (loaded != null) {
      for (var i = 0; i < loaded.length; i++) {
        if (loaded[i].recipientId == readerId && loaded[i].readAt == null) {
          loaded[i] = loaded[i].copyWith(readAt: now);
        }
      }
    }
    _updateChat(readerId, (chat) {
      final last = chat.lastMessage;
      return last != null && last.recipientId == readerId && last.readAt == null
          ? chat.copyWith(lastMessage: last.copyWith(readAt: now))
          : chat;
    });
  }

  void _updateChat(String contactId, Chat Function(Chat chat) update) {
    final index = _chats.indexWhere((chat) => chat.contactId == contactId);
    if (index >= 0) _chats[index] = update(_chats[index]);
  }

  void _sortChats() {
    _chats.sort((a, b) {
      final left = a.lastMessage?.sentAt;
      final right = b.lastMessage?.sentAt;
      if (left == null && right == null) return 0;
      if (left == null) return 1;
      if (right == null) return -1;
      return right.compareTo(left);
    });
  }

  /// Wirft wie die anderen Services einen [StateError], wenn keine
  /// Backend-Person ausgewählt ist.
  static Future<String> _currentPersonId() async =>
      (await HorseService().currentPersonId()).toString();

  static List<Map<String, dynamic>> _decodeList(
    http.Response response,
    String key,
  ) {
    if (response.statusCode != 200) {
      throw Exception('HTTP ${response.statusCode}');
    }
    final json = jsonDecode(response.body);
    final list = json is Map<String, dynamic> ? json[key] : json;
    return list is List ? list.whereType<Map<String, dynamic>>().toList() : [];
  }

  static String? _problemDetail(http.Response response) {
    try {
      final json = jsonDecode(response.body);
      final detail = json is Map<String, dynamic> ? json['detail'] : null;
      return detail is String && detail.isNotEmpty ? detail : null;
    } catch (_) {
      return null;
    }
  }
}
