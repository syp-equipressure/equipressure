import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/chat.dart';
import '../models/message.dart';
import '../models/saddler.dart';

/// Service for persisting chat data locally.
/// Uses SharedPreferences for simple key-value storage.
class LocalStorageService {
  static final LocalStorageService _instance = LocalStorageService._internal();
  factory LocalStorageService() => _instance;
  LocalStorageService._internal();

  static const String _chatsKey = 'chats_data';
  static const String _messagesKeyPrefix = 'messages_';

  SharedPreferences? _prefs;

  Future<SharedPreferences> get _preferences async {
    _prefs ??= await SharedPreferences.getInstance();
    return _prefs!;
  }

  /// Save all chats to local storage
  Future<void> saveChats(List<Chat> chats) async {
    final prefs = await _preferences;

    final chatsJson = chats.map((chat) => {
      'id': chat.id,
      'saddler': chat.saddler.toJson(),
      'lastMessageTime': chat.lastMessageTime.toIso8601String(),
      'lastMessagePreview': chat.lastMessagePreview,
    }).toList();

    await prefs.setString(_chatsKey, jsonEncode(chatsJson));

    // Save messages for each chat separately
    for (final chat in chats) {
      await saveMessages(chat.id, chat.messages);
    }
  }

  /// Load all chats from local storage
  Future<List<Chat>> loadChats() async {
    final prefs = await _preferences;
    final chatsString = prefs.getString(_chatsKey);

    if (chatsString == null) {
      return [];
    }

    try {
      final List<dynamic> chatsJson = jsonDecode(chatsString);
      final List<Chat> chats = [];

      for (final chatJson in chatsJson) {
        final messages = await loadMessages(chatJson['id']);

        chats.add(Chat(
          id: chatJson['id'],
          saddler: Saddler.fromJson(chatJson['saddler']),
          messages: messages,
          lastMessageTime: DateTime.parse(chatJson['lastMessageTime']),
          lastMessagePreview: chatJson['lastMessagePreview'],
        ));
      }

      return chats;
    } catch (e) {
      return [];
    }
  }

  /// Save messages for a specific chat
  Future<void> saveMessages(String chatId, List<Message> messages) async {
    final prefs = await _preferences;
    final messagesJson = messages.map((m) => m.toJson()).toList();
    await prefs.setString('$_messagesKeyPrefix$chatId', jsonEncode(messagesJson));
  }

  /// Load messages for a specific chat
  Future<List<Message>> loadMessages(String chatId) async {
    final prefs = await _preferences;
    final messagesString = prefs.getString('$_messagesKeyPrefix$chatId');

    if (messagesString == null) {
      return [];
    }

    try {
      final List<dynamic> messagesJson = jsonDecode(messagesString);
      return messagesJson.map((m) => Message.fromJson(m)).toList();
    } catch (e) {
      return [];
    }
  }

  /// Add a single message to a chat
  Future<void> addMessage(String chatId, Message message) async {
    final messages = await loadMessages(chatId);
    messages.add(message);
    await saveMessages(chatId, messages);
  }

  /// Update chat metadata (last message, etc.)
  Future<void> updateChatMetadata(String chatId, Message lastMessage) async {
    final chats = await loadChats();
    final chatIndex = chats.indexWhere((c) => c.id == chatId);

    if (chatIndex != -1) {
      final chat = chats[chatIndex];
      chats[chatIndex] = Chat(
        id: chat.id,
        saddler: chat.saddler,
        messages: chat.messages,
        lastMessageTime: lastMessage.timestamp,
        lastMessagePreview: lastMessage.type == MessageType.horses
            ? 'Pferde geteilt'
            : lastMessage.content,
      );
      await saveChats(chats);
    }
  }

  /// Clear all local chat data
  Future<void> clearAllData() async {
    final prefs = await _preferences;
    final keys = prefs.getKeys();

    for (final key in keys) {
      if (key == _chatsKey || key.startsWith(_messagesKeyPrefix)) {
        await prefs.remove(key);
      }
    }
  }

  /// Check if local data exists
  Future<bool> hasLocalData() async {
    final prefs = await _preferences;
    return prefs.containsKey(_chatsKey);
  }
}
