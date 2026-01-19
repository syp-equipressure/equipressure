import 'dart:async';
import 'dart:convert';
import 'package:flutter/services.dart';
import '../models/chat.dart';
import '../models/message.dart';
import 'local_storage_service.dart';
import 'websocket_service.dart';

class ChatService {
  static final ChatService _instance = ChatService._internal();
  factory ChatService() => _instance;
  ChatService._internal();

  final LocalStorageService _localStorage = LocalStorageService();
  final WebSocketService _webSocket = WebSocketService();

  List<Chat>? _cachedChats;
  final _chatUpdateController = StreamController<List<Chat>>.broadcast();
  final _newMessageController = StreamController<Message>.broadcast();

  StreamSubscription? _wsSubscription;

  Stream<List<Chat>> get chatUpdates => _chatUpdateController.stream;
  Stream<Message> get newMessages => _newMessageController.stream;
  Stream<ConnectionState> get connectionState => _webSocket.connectionStream;
  bool get isConnected => _webSocket.isConnected;

  Future<void> initialize() async {
    _wsSubscription = _webSocket.messageStream.listen(_onWebSocketMessage);
  }

  Future<bool> connectToServer(String url, {String? token}) async {
    return _webSocket.connect(url, token: token);
  }

  void disconnectFromServer() {
    _webSocket.disconnect();
  }

  Future<List<Chat>> getChats() async {
    if (_cachedChats != null) {
      return _cachedChats!;
    }

    final localChats = await _localStorage.loadChats();

    if (localChats.isNotEmpty) {
      _cachedChats = localChats;
      _cachedChats!.sort((a, b) => b.lastMessageTime.compareTo(a.lastMessageTime));
      return _cachedChats!;
    }

    try {
      final String response =
          await rootBundle.loadString('assets/data/chat.json');
      final Map<String, dynamic> data = json.decode(response);
      final List<dynamic> chatsJson = data['chats'];

      _cachedChats = chatsJson.map((json) => Chat.fromJson(json)).toList();
      _cachedChats!.sort((a, b) => b.lastMessageTime.compareTo(a.lastMessageTime));

      await _localStorage.saveChats(_cachedChats!);

      return _cachedChats!;
    } catch (e) {
      return [];
    }
  }

  Future<Chat?> getChatById(String chatId) async {
    final chats = await getChats();
    try {
      return chats.firstWhere((chat) => chat.id == chatId);
    } catch (e) {
      return null;
    }
  }

  Future<void> addChat(Chat chat) async {
    if (_cachedChats == null) {
      await getChats();
    }
    _cachedChats!.insert(0, chat);
    await _localStorage.saveChats(_cachedChats!);
    _chatUpdateController.add(_cachedChats!);
  }

  Future<void> sendMessage(String chatId, Message message) async {
    if (_cachedChats == null) {
      await getChats();
    }

    final chatIndex = _cachedChats!.indexWhere((c) => c.id == chatId);
    if (chatIndex == -1) return;

    final chat = _cachedChats![chatIndex];
    final updatedMessages = [...chat.messages, message];

    _cachedChats![chatIndex] = Chat(
      id: chat.id,
      saddler: chat.saddler,
      messages: updatedMessages,
      lastMessageTime: message.timestamp,
      lastMessagePreview: _getMessagePreview(message),
    );

    _cachedChats!.sort((a, b) => b.lastMessageTime.compareTo(a.lastMessageTime));

    await _localStorage.addMessage(chatId, message);
    await _localStorage.updateChatMetadata(chatId, message);

    if (_webSocket.isConnected) {
      _webSocket.sendMessage(message);
    }

    _chatUpdateController.add(_cachedChats!);
  }

  void _onWebSocketMessage(Message message) {
    if (_cachedChats == null) return;

    final chatIndex = _cachedChats!.indexWhere((c) => c.id == message.chatId);
    if (chatIndex == -1) return;

    final chat = _cachedChats![chatIndex];
    final updatedMessages = [...chat.messages, message];

    _cachedChats![chatIndex] = Chat(
      id: chat.id,
      saddler: chat.saddler,
      messages: updatedMessages,
      lastMessageTime: message.timestamp,
      lastMessagePreview: _getMessagePreview(message),
    );

    _cachedChats!.sort((a, b) => b.lastMessageTime.compareTo(a.lastMessageTime));

    _localStorage.addMessage(message.chatId, message);
    _localStorage.updateChatMetadata(message.chatId, message);

    _newMessageController.add(message);
    _chatUpdateController.add(_cachedChats!);
  }

  String _getMessagePreview(Message message) {
    switch (message.type) {
      case MessageType.horses:
        return 'Pferde geteilt';
      case MessageType.image:
        final count = message.attachments?.length ?? 1;
        return count == 1 ? 'Bild' : '$count Bilder';
      case MessageType.file:
        final count = message.attachments?.length ?? 1;
        return count == 1 ? 'Datei' : '$count Dateien';
      case MessageType.text:
        return message.content;
    }
  }

  void clearCache() {
    _cachedChats = null;
  }

  Future<void> clearAllData() async {
    _cachedChats = null;
    await _localStorage.clearAllData();
  }

  void dispose() {
    _wsSubscription?.cancel();
    _webSocket.dispose();
    _chatUpdateController.close();
    _newMessageController.close();
  }

  bool get hasChats => _cachedChats != null && _cachedChats!.isNotEmpty;
}
