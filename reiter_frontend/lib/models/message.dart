/// Eine Chat-Nachricht aus dem Backend: ein Text, ein geteiltes Pferd oder eine
/// geteilte Messung.
class Message {
  final String id;
  final String senderId;
  final String recipientId;
  final MessageType type;
  final String? text;
  final DateTime sentAt;
  final DateTime? readAt;

  /// Wann die Sattler*in das geteilte Pferd bzw. die Messung hinzugefügt hat.
  final DateTime? acceptedAt;
  final SharedHorse? horse;
  final SharedMeasurement? measurement;

  /// Bild oder Datei, der Inhalt wird über [ChatService.fileUrl] geladen.
  final ChatFile? file;

  Message({
    required this.id,
    required this.senderId,
    required this.recipientId,
    required this.type,
    required this.sentAt,
    this.text,
    this.readAt,
    this.acceptedAt,
    this.horse,
    this.measurement,
    this.file,
  });

  factory Message.fromJson(Map<String, dynamic> json) {
    final horse = json['horse'];
    final measurement = json['measurement'];
    final file = json['file'];
    return Message(
      id: json['id'].toString(),
      senderId: json['senderId'].toString(),
      recipientId: json['recipientId'].toString(),
      type: MessageType.fromJson(json['type']),
      text: json['text'] as String?,
      sentAt: _parseDate(json['sentAt']) ?? DateTime.now(),
      readAt: _parseDate(json['readAt']),
      acceptedAt: _parseDate(json['acceptedAt']),
      horse: horse is Map<String, dynamic> ? SharedHorse.fromJson(horse) : null,
      measurement: measurement is Map<String, dynamic>
          ? SharedMeasurement.fromJson(measurement)
          : null,
      file: file is Map<String, dynamic> ? ChatFile.fromJson(file) : null,
    );
  }

  Message copyWith({DateTime? readAt}) {
    return Message(
      id: id,
      senderId: senderId,
      recipientId: recipientId,
      type: type,
      text: text,
      sentAt: sentAt,
      readAt: readAt ?? this.readAt,
      acceptedAt: acceptedAt,
      horse: horse,
      measurement: measurement,
      file: file,
    );
  }

  /// Kurzer Text für die Chatliste und Benachrichtigungen.
  String get preview {
    switch (type) {
      case MessageType.horse:
        return horse == null ? 'Geteiltes Pferd gelöscht' : 'Pferd geteilt: ${horse!.name}';
      case MessageType.measurement:
        return measurement == null
            ? 'Geteilte Messung gelöscht'
            : 'Messung geteilt: ${measurement!.horseName}';
      case MessageType.file:
        return file?.isImage == true ? 'Bild' : 'Datei: ${file?.fileName ?? ''}';
      case MessageType.text:
        return text ?? '';
    }
  }

  static DateTime? _parseDate(dynamic value) =>
      value is String ? DateTime.tryParse(value)?.toLocal() : null;
}

enum MessageType {
  text('Text'),
  horse('Horse'),
  measurement('Measurement'),
  file('File');

  const MessageType(this.json);

  /// Name im Backend
  final String json;

  static MessageType fromJson(dynamic value) => MessageType.values.firstWhere(
        (type) => type.json == value,
        orElse: () => MessageType.text,
      );
}

class ChatFile {
  final String fileName;
  final String contentType;
  final int size;

  ChatFile({
    required this.fileName,
    required this.contentType,
    required this.size,
  });

  factory ChatFile.fromJson(Map<String, dynamic> json) => ChatFile(
        fileName: json['fileName'] ?? 'Datei',
        contentType: json['contentType'] ?? 'application/octet-stream',
        size: (json['size'] as num?)?.toInt() ?? 0,
      );

  bool get isImage => contentType.startsWith('image/');

  String get sizeFormatted {
    if (size < 1024) return '$size B';
    if (size < 1024 * 1024) return '${(size / 1024).round()} KB';
    return '${(size / (1024 * 1024)).toStringAsFixed(1).replaceAll('.', ',')} MB';
  }
}

class SharedHorse {
  final String id;
  final String name;

  SharedHorse({required this.id, required this.name});

  factory SharedHorse.fromJson(Map<String, dynamic> json) =>
      SharedHorse(id: json['id'].toString(), name: json['name'] ?? '');
}

class SharedMeasurement {
  final String id;
  final String name;
  final DateTime? date;
  final String horseId;
  final String horseName;

  SharedMeasurement({
    required this.id,
    required this.name,
    required this.date,
    required this.horseId,
    required this.horseName,
  });

  factory SharedMeasurement.fromJson(Map<String, dynamic> json) =>
      SharedMeasurement(
        id: json['id'].toString(),
        name: json['name'] ?? '',
        date: DateTime.tryParse(json['date'] ?? ''),
        horseId: json['horseId'].toString(),
        horseName: json['horseName'] ?? '',
      );
}
