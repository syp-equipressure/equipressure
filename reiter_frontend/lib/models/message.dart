class Message {
  final String id;
  final String chatId;
  final String senderId;
  final String content;
  final DateTime timestamp;
  final bool isFromMe;
  final MessageType type;
  final List<String>? attachedHorseIds;
  final List<MessageAttachment>? attachments;

  Message({
    required this.id,
    required this.chatId,
    required this.senderId,
    required this.content,
    required this.timestamp,
    required this.isFromMe,
    this.type = MessageType.text,
    this.attachedHorseIds,
    this.attachments,
  });

  factory Message.fromJson(Map<String, dynamic> json) {
    return Message(
      id: json['id'] ?? '',
      chatId: json['chatId'] ?? '',
      senderId: json['senderId'] ?? '',
      content: json['content'] ?? '',
      timestamp: DateTime.parse(json['timestamp']),
      isFromMe: json['isFromMe'] ?? false,
      type: MessageType.values.firstWhere(
        (e) => e.name == json['type'],
        orElse: () => MessageType.text,
      ),
      attachedHorseIds: json['attachedHorseIds'] != null
          ? List<String>.from(json['attachedHorseIds'])
          : null,
      attachments: json['attachments'] != null
          ? (json['attachments'] as List)
              .map((a) => MessageAttachment.fromJson(a))
              .toList()
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'chatId': chatId,
      'senderId': senderId,
      'content': content,
      'timestamp': timestamp.toIso8601String(),
      'isFromMe': isFromMe,
      'type': type.name,
      'attachedHorseIds': attachedHorseIds,
      'attachments': attachments?.map((a) => a.toJson()).toList(),
    };
  }
}

enum MessageType {
  text,
  horses,
  image,
  file,
}

class MessageAttachment {
  final String id;
  final String fileName;
  final String filePath;
  final String? mimeType;
  final int? fileSize;

  MessageAttachment({
    required this.id,
    required this.fileName,
    required this.filePath,
    this.mimeType,
    this.fileSize,
  });

  factory MessageAttachment.fromJson(Map<String, dynamic> json) {
    return MessageAttachment(
      id: json['id'] ?? '',
      fileName: json['fileName'] ?? '',
      filePath: json['filePath'] ?? '',
      mimeType: json['mimeType'],
      fileSize: json['fileSize'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fileName': fileName,
      'filePath': filePath,
      'mimeType': mimeType,
      'fileSize': fileSize,
    };
  }

  bool get isImage {
    final ext = fileName.toLowerCase();
    return ext.endsWith('.jpg') ||
        ext.endsWith('.jpeg') ||
        ext.endsWith('.png') ||
        ext.endsWith('.gif') ||
        ext.endsWith('.webp');
  }

  String get fileSizeFormatted {
    if (fileSize == null) return '';
    if (fileSize! < 1024) return '$fileSize B';
    if (fileSize! < 1024 * 1024) return '${(fileSize! / 1024).toStringAsFixed(1)} KB';
    return '${(fileSize! / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
}
