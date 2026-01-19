import 'message.dart';
import 'saddler.dart';

class Chat {
  final String id;
  final Saddler saddler;
  final List<Message> messages;
  final DateTime lastMessageTime;
  final String lastMessagePreview;

  Chat({
    required this.id,
    required this.saddler,
    required this.messages,
    required this.lastMessageTime,
    required this.lastMessagePreview,
  });

  factory Chat.fromJson(Map<String, dynamic> json) {
    final messages = (json['messages'] as List<dynamic>?)
            ?.map((m) => Message.fromJson(m))
            .toList() ??
        [];

    return Chat(
      id: json['id'] ?? '',
      saddler: Saddler.fromJson(json['saddler']),
      messages: messages,
      lastMessageTime: DateTime.parse(json['lastMessageTime']),
      lastMessagePreview: json['lastMessagePreview'] ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'saddler': saddler.toJson(),
      'messages': messages.map((m) => m.toJson()).toList(),
      'lastMessageTime': lastMessageTime.toIso8601String(),
      'lastMessagePreview': lastMessagePreview,
    };
  }

  String get formattedLastMessageTime {
    final now = DateTime.now();
    final difference = now.difference(lastMessageTime);

    if (difference.inMinutes < 60) {
      return 'vor ${difference.inMinutes} Minuten';
    } else if (difference.inHours < 24) {
      return 'vor ${difference.inHours} Stunden';
    } else if (difference.inDays < 7) {
      return 'vor ${difference.inDays} Tagen';
    } else {
      return '${lastMessageTime.day}.${lastMessageTime.month}.${lastMessageTime.year}';
    }
  }
}
