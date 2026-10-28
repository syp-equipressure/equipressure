import 'message.dart';

/// Die Unterhaltung mit einer Sattler*in.
class Chat {
  /// Person-Id der Sattler*in im Backend
  final String contactId;
  final String contactName;
  final Message? lastMessage;
  final int unreadCount;

  Chat({
    required this.contactId,
    required this.contactName,
    this.lastMessage,
    this.unreadCount = 0,
  });

  factory Chat.fromJson(Map<String, dynamic> json) {
    final contact = json['contact'] as Map<String, dynamic>? ?? const {};
    return Chat(
      contactId: contact['id'].toString(),
      contactName:
          '${contact['firstName'] ?? ''} ${contact['lastName'] ?? ''}'.trim(),
      lastMessage: json['lastMessage'] is Map<String, dynamic>
          ? Message.fromJson(json['lastMessage'])
          : null,
      unreadCount: (json['unreadCount'] as num?)?.toInt() ?? 0,
    );
  }

  Chat copyWith({Message? lastMessage, int? unreadCount}) {
    return Chat(
      contactId: contactId,
      contactName: contactName,
      lastMessage: lastMessage ?? this.lastMessage,
      unreadCount: unreadCount ?? this.unreadCount,
    );
  }

  String get lastMessagePreview =>
      lastMessage?.preview ?? 'Noch keine Nachrichten';

  String get formattedLastMessageTime {
    final time = lastMessage?.sentAt;
    if (time == null) return '';

    final difference = DateTime.now().difference(time);
    if (difference.inMinutes < 1) {
      return 'gerade eben';
    } else if (difference.inMinutes < 60) {
      return 'vor ${difference.inMinutes} Min.';
    } else if (difference.inHours < 24) {
      return 'vor ${difference.inHours} Std.';
    } else if (difference.inDays < 7) {
      return 'vor ${difference.inDays} Tagen';
    } else {
      return '${time.day}.${time.month}.${time.year}';
    }
  }
}
