import 'dart:typed_data';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:image_picker/image_picker.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../models/chat.dart';
import '../../models/horse.dart';
import '../../models/measurement.dart';
import '../../models/message.dart';
import '../../models/saddler.dart';
import '../../services/chat_service.dart';
import '../../services/horse_service.dart';
import '../../services/measurement_service.dart';
import '../../services/saddler_service.dart';
import '../find_saddler/saddler_profile_screen.dart';
import '../horses/horse_history_measurement.dart';
import '../../widgets/chat_attachment_menu.dart';
import '../../widgets/horse_selection_dialog.dart';
import '../../widgets/measurement_selection_dialog.dart';

/// Farben des Chats, gleich wie im Sattler-Frontend (helles Theme)
class _ChatColors {
  static const accent = Color(0xFF6955AE);
  static const outgoing = Color(0xFFF4F2FB);
  static const outgoingBorder = Color(0xFFEAE6F7);
  static const incoming = Colors.white;
  static const incomingBorder = Color(0xFFDDE1E7);
  static const card = Color(0xFFFAFAFC);
  static const textStrong = Color(0xFF18212F);
  static const text = Color(0xFF293342);
  static const textSoft = Color(0xFF5F6B7A);
  static const textMuted = Color(0xFF8993A0);
}

class ChatScreen extends StatefulWidget {
  final Chat chat;

  const ChatScreen({super.key, required this.chat});

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _messageController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  final ChatService _chatService = ChatService();
  final HorseService _horseService = HorseService();

  List<Horse> _horses = [];
  bool _isLoading = true;
  bool _isSending = false;
  String? _error;
  int _messageCount = 0;

  String get _contactId => widget.chat.contactId;

  List<Message> get _messages => _chatService.messagesWith(_contactId) ?? [];

  @override
  void initState() {
    super.initState();
    _chatService.activeContactId = _contactId;
    _chatService.addListener(_onChatChanged);
    _loadMessages();
    _loadHorses();
  }

  Future<void> _loadMessages() async {
    try {
      await _chatService.loadMessages(_contactId);
      await _chatService.markAsRead(_contactId);
      if (!mounted) return;
      setState(() => _isLoading = false);
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _error = 'Nachrichten konnten nicht geladen werden.';
      });
    }
  }

  Future<void> _loadHorses() async {
    try {
      final horses = await _horseService.getHorses();
      if (!mounted) return;
      setState(() => _horses = horses);
    } catch (_) {
      // ohne Pferde gibt es nur nichts zu teilen
    }
  }

  void _onChatChanged() {
    if (!mounted) return;
    setState(() {});
    if (_messages.length != _messageCount) {
      _messageCount = _messages.length;
      WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
    }
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      _scrollController.animateTo(
        _scrollController.position.maxScrollExtent,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOut,
      );
    }
  }

  Future<void> _send(Future<Message> Function() send) async {
    setState(() {
      _isSending = true;
      _error = null;
    });
    try {
      await send();
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  Future<void> _sendMessage() async {
    final text = _messageController.text.trim();
    if (text.isEmpty || _isSending) return;

    await _send(() => _chatService.sendText(_contactId, text));
    if (_error == null) _messageController.clear();
  }

  void _showAttachmentMenu() {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => ChatAttachmentMenu(
        onImagesSelected: () {
          Navigator.pop(context);
          _pickImages();
        },
        onFilesSelected: () {
          Navigator.pop(context);
          _pickFiles();
        },
        onHorsesSelected: () {
          Navigator.pop(context);
          _showHorseSelectionDialog();
        },
        onMeasurementSelected: () {
          Navigator.pop(context);
          _showMeasurementSelectionDialog();
        },
      ),
    );
  }

  Future<void> _pickImages() async {
    try {
      final images = await ImagePicker().pickMultiImage(
        imageQuality: 80,
        maxWidth: 1920,
        maxHeight: 1920,
      );
      final files = <({String name, Uint8List bytes})>[];
      for (final image in images) {
        files.add((name: image.name, bytes: await image.readAsBytes()));
      }
      await _sendFiles(files);
    } catch (_) {
      if (mounted) setState(() => _error = 'Bilder konnten nicht geladen werden.');
    }
  }

  Future<void> _pickFiles() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        allowMultiple: true,
        withData: true,
      );
      if (result == null) return;
      await _sendFiles([
        for (final file in result.files)
          if (file.bytes != null) (name: file.name, bytes: file.bytes!),
      ]);
    } catch (_) {
      if (mounted) setState(() => _error = 'Dateien konnten nicht geladen werden.');
    }
  }

  /// Schickt die Dateien nacheinander, der eingegebene Text geht mit der ersten mit.
  Future<void> _sendFiles(List<({String name, Uint8List bytes})> files) async {
    if (files.isEmpty) return;
    for (final file in files) {
      if (file.bytes.length > ChatService.maxFileSize) {
        setState(() => _error = '${file.name} ist größer als 10 MB.');
        return;
      }
    }

    final text = _messageController.text.trim();
    await _send(() async {
      late Message last;
      for (var i = 0; i < files.length; i++) {
        last = await _chatService.sendFile(
          _contactId,
          bytes: files[i].bytes,
          fileName: files[i].name,
          text: i == 0 ? text : null,
        );
      }
      return last;
    });
    if (_error == null) _messageController.clear();
  }

  Future<void> _openFile(Message message) async {
    final uri = Uri.parse(_chatService.fileUrl(message));
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication) && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Die Datei konnte nicht geöffnet werden.')),
      );
    }
  }

  /// Großansicht eines Bildes, zoombar. Lässt es sich nicht laden, kann es
  /// im Browser bzw. einer anderen App geöffnet werden.
  void _showImage(Message message) {
    Navigator.push(
      context,
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (context) => Scaffold(
          backgroundColor: Colors.black,
          appBar: AppBar(
            backgroundColor: Colors.black,
            foregroundColor: Colors.white,
            title: Text(
              message.file?.fileName ?? 'Bild',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 16),
            ),
            actions: [
              IconButton(
                icon: const Icon(Icons.open_in_new),
                tooltip: 'Im Browser öffnen',
                onPressed: () => _openFile(message),
              ),
            ],
          ),
          body: InteractiveViewer(
            maxScale: 5,
            child: Center(
              child: Image.network(
                _chatService.fileUrl(message),
                fit: BoxFit.contain,
                loadingBuilder: (context, child, progress) => progress == null
                    ? child
                    : const CircularProgressIndicator(color: Colors.white),
                errorBuilder: (context, error, stackTrace) => _buildImageError(message),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildImageError(Message message) {
    return TextButton.icon(
      onPressed: () => _openFile(message),
      icon: const Icon(Icons.open_in_new),
      label: const Text('Bild öffnen'),
    );
  }

  void _showHorseSelectionDialog() {
    showDialog(
      context: context,
      builder: (context) => HorseSelectionDialog(
        horses: _horses,
        onHorsesSelected: (selectedHorses) => _send(() async {
          late Message last;
          for (final horse in selectedHorses) {
            last = await _chatService.shareHorse(_contactId, horse.id);
          }
          return last;
        }),
      ),
    );
  }

  void _showMeasurementSelectionDialog() {
    showDialog(
      context: context,
      builder: (context) => MeasurementSelectionDialog(
        horses: _horses,
        onMeasurementSelected: (Measurement measurement) => _send(
          () => _chatService.shareMeasurement(_contactId, measurement.id),
        ),
      ),
    );
  }

  /// Öffnet eine geteilte Messung, z.B. eine, die die Sattler*in geschickt hat.
  Future<void> _openMeasurement(SharedMeasurement shared) async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      final results = await Future.wait([
        _horseService.getHorseById(shared.horseId),
        MeasurementService().getMeasurementById(shared.id, horseId: shared.horseId),
      ]);
      final horse = results[0] as Horse?;
      final measurement = results[1] as Measurement?;
      if (!mounted) return;
      if (horse == null || measurement == null) {
        messenger.showSnackBar(
          const SnackBar(content: Text('Die Messung gibt es nicht mehr.')),
        );
        return;
      }
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) =>
              MeasurementDetailScreen(measurement: measurement, horse: horse),
        ),
      );
    } catch (_) {
      messenger.showSnackBar(
        const SnackBar(content: Text('Die Messung konnte nicht geladen werden.')),
      );
    }
  }

  @override
  void dispose() {
    if (_chatService.activeContactId == _contactId) {
      _chatService.activeContactId = null;
    }
    _chatService.removeListener(_onChatChanged);
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final messages = _messages;
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: _buildAppBar(),
      body: Column(
        children: [
          if (!_chatService.isConnected)
            Container(
              width: double.infinity,
              color: Colors.amber[50],
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
              child: Text(
                'Live-Verbindung wird aufgebaut …',
                style: TextStyle(color: Colors.amber[900], fontSize: 12),
              ),
            ),
          Expanded(
            child: _isLoading && messages.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : messages.isEmpty
                    ? _buildEmptyState()
                    : ListView.builder(
                        controller: _scrollController,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 8,
                        ),
                        itemCount: messages.length,
                        itemBuilder: (context, index) => _buildMessage(
                          messages[index],
                          index == 0 ? null : messages[index - 1],
                        ),
                      ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
              child: Text(
                _error!,
                style: TextStyle(color: Colors.red[700], fontSize: 13),
              ),
            ),
          _buildMessageInput(),
        ],
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Text(
          'Noch keine Nachrichten.\nSchreib ${widget.chat.contactName} oder teile '
          'Bilder, Dateien, Pferde oder Messungen über die Büroklammer.',
          textAlign: TextAlign.center,
          style: TextStyle(color: Colors.grey[500]),
        ),
      ),
    );
  }

  PreferredSizeWidget _buildAppBar() {
    return AppBar(
      backgroundColor: Colors.white,
      elevation: 1,
      shadowColor: Colors.black.withValues(alpha: 0.1),
      leading: IconButton(
        icon: const Icon(Icons.arrow_back, color: Colors.black),
        onPressed: () => Navigator.pop(context),
      ),
      // ein Tipp auf Name oder Bild öffnet das Profil der Sattler*in
      title: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: _openProfile,
        child: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: Colors.green[50],
              child: Text(
                _getInitials(widget.chat.contactName),
                style: TextStyle(
                  color: Colors.green[700],
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                widget.chat.contactName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: Colors.black,
                  fontWeight: FontWeight.w600,
                  fontSize: 18,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openProfile() async {
    final messenger = ScaffoldMessenger.of(context);
    Saddler? saddler;
    try {
      saddler = await SaddlerService().getSaddlerById(_contactId);
    } catch (_) {
      saddler = null;
    }
    if (!mounted) return;
    if (saddler == null) {
      messenger.showSnackBar(
        const SnackBar(content: Text('Das Profil konnte nicht geladen werden.')),
      );
      return;
    }
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => SaddlerProfileScreen(saddler: saddler!, openedFromChat: true),
      ),
    );
  }

  String _getInitials(String name) {
    final parts = name.split(' ').where((part) => part.isNotEmpty).toList();
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return parts.isNotEmpty ? parts[0][0].toUpperCase() : '?';
  }

  Widget _buildMessage(Message message, Message? previous) {
    final isFromMe = message.senderId != _contactId;
    final showDate = previous == null ||
        !DateUtils.isSameDay(previous.sentAt, message.sentAt);

    return Column(
      children: [
        if (showDate) _buildDateDivider(message.sentAt),
        Align(
          alignment: isFromMe ? Alignment.centerRight : Alignment.centerLeft,
          child: Container(
            margin: const EdgeInsets.symmetric(vertical: 4),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            constraints: BoxConstraints(
              maxWidth: MediaQuery.of(context).size.width * 0.75,
            ),
            // wie im Sattler-Frontend: auf der Seite des Absenders unten eine kleine Ecke
            decoration: BoxDecoration(
              color: isFromMe ? _ChatColors.outgoing : _ChatColors.incoming,
              border: Border.all(
                color: isFromMe
                    ? _ChatColors.outgoingBorder
                    : _ChatColors.incomingBorder,
              ),
              borderRadius: BorderRadius.only(
                topLeft: const Radius.circular(14),
                topRight: const Radius.circular(14),
                bottomLeft: Radius.circular(isFromMe ? 14 : 4),
                bottomRight: Radius.circular(isFromMe ? 4 : 14),
              ),
            ),
            child: Column(
              crossAxisAlignment: isFromMe
                  ? CrossAxisAlignment.end
                  : CrossAxisAlignment.start,
              children: [
                if (message.file != null)
                  _buildFile(message)
                else if (message.type != MessageType.text)
                  _buildShareCard(message),
                if (message.text != null && message.text!.isNotEmpty)
                  Text(
                    message.text!,
                    style: TextStyle(
                      fontSize: 15,
                      color: isFromMe ? _ChatColors.textStrong : _ChatColors.text,
                    ),
                  ),
                const SizedBox(height: 4),
                _buildMeta(message, isFromMe),
              ],
            ),
          ),
        ),
      ],
    );
  }

  /// Uhrzeit, bei eigenen Nachrichten mit Häkchen wie bei WhatsApp/Signal:
  /// eines wenn gesendet, zwei farbige wenn gelesen.
  Widget _buildMeta(Message message, bool isFromMe) {
    final time = TimeOfDay.fromDateTime(message.sentAt);
    final text =
        '${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}';
    final isRead = message.readAt != null;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          text,
          style: TextStyle(
            fontSize: 11,
            color: isFromMe ? _ChatColors.textSoft : _ChatColors.textMuted,
          ),
        ),
        if (isFromMe) ...[
          const SizedBox(width: 3),
          Icon(
            isRead ? Icons.done_all : Icons.done,
            size: 15,
            color: isRead ? _ChatColors.accent : _ChatColors.textSoft,
            semanticLabel: isRead ? 'Gelesen' : 'Gesendet',
          ),
        ],
      ],
    );
  }

  Widget _buildDateDivider(DateTime date) {
    final now = DateTime.now();
    final label = DateUtils.isSameDay(date, now)
        ? 'Heute'
        : DateUtils.isSameDay(date, now.subtract(const Duration(days: 1)))
            ? 'Gestern'
            : '${date.day}.${date.month}.${date.year}';
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
        decoration: BoxDecoration(
          color: _ChatColors.outgoing,
          borderRadius: BorderRadius.circular(999),
        ),
        child: Text(
          label,
          style: const TextStyle(fontSize: 11, color: _ChatColors.textSoft),
        ),
      ),
    );
  }

  Widget _buildFile(Message message) {
    final file = message.file!;
    if (file.isImage) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: GestureDetector(
          onTap: () => _showImage(message),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(10),
            child: Image.network(
              _chatService.fileUrl(message),
              height: 200,
              fit: BoxFit.cover,
              loadingBuilder: (context, child, progress) => progress == null
                  ? child
                  : const SizedBox(
                      height: 200,
                      width: 200,
                      child: Center(child: CircularProgressIndicator()),
                    ),
              errorBuilder: (context, error, stackTrace) => SizedBox(
                height: 80,
                width: 160,
                child: Center(child: _buildImageError(message)),
              ),
            ),
          ),
        ),
      );
    }

    return InkWell(
      borderRadius: BorderRadius.circular(12),
      onTap: () => _openFile(message),
      child: Container(
        margin: const EdgeInsets.only(bottom: 6),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: _ChatColors.card,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.insert_drive_file_outlined, color: _ChatColors.accent),
            const SizedBox(width: 12),
            Flexible(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    file.fileName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                  Text(
                    file.sizeFormatted,
                    style: TextStyle(fontSize: 12, color: Colors.grey[700]),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Icon(Icons.download, size: 20, color: Colors.grey[600]),
          ],
        ),
      ),
    );
  }

  Widget _buildShareCard(Message message) {
    final horse = message.horse;
    final measurement = message.measurement;
    final date = measurement?.date;

    // das Backend entfernt den Verweis, wenn das Pferd bzw. die Messung gelöscht wird
    if (horse == null && measurement == null) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Text(
          message.type == MessageType.horse
              ? 'Das geteilte Pferd wurde gelöscht.'
              : 'Die geteilte Messung wurde gelöscht.',
          style: TextStyle(
            fontSize: 13,
            fontStyle: FontStyle.italic,
            color: Colors.grey[600],
          ),
        ),
      );
    }

    final card = Container(
      margin: const EdgeInsets.only(bottom: 6),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: _ChatColors.card,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          if (horse != null)
            SvgPicture.asset(
              'assets/icon/horseIcon.svg',
              width: 26,
              height: 26,
              colorFilter:
                  const ColorFilter.mode(_ChatColors.accent, BlendMode.srcIn),
            )
          else
            const Icon(Icons.analytics_outlined, color: _ChatColors.accent),
          const SizedBox(width: 12),
          Flexible(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  horse != null ? 'Pferd geteilt' : 'Messung geteilt',
                  style: TextStyle(fontSize: 11, color: Colors.grey[600]),
                ),
                Text(
                  horse?.name ?? measurement?.horseName ?? '',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 15,
                  ),
                ),
                Text(
                  horse != null
                      ? 'mit allen Messungen'
                      : date != null
                          ? 'Messung vom ${date.day}.${date.month}.${date.year}'
                          : measurement?.name ?? '',
                  style: TextStyle(fontSize: 12, color: Colors.grey[700]),
                ),
                if (message.acceptedAt != null) ...[
                  const SizedBox(height: 4),
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.check_circle, size: 14, color: Colors.green[700]),
                      const SizedBox(width: 4),
                      Text(
                        'Hinzugefügt',
                        style: TextStyle(fontSize: 12, color: Colors.green[700]),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
          if (measurement != null) ...[
            const SizedBox(width: 8),
            Icon(Icons.chevron_right, color: Colors.grey[600]),
          ],
        ],
      ),
    );

    // eine geteilte Messung lässt sich öffnen, egal wer sie geschickt hat
    if (measurement == null) return card;
    return InkWell(
      borderRadius: BorderRadius.circular(12),
      onTap: () => _openMeasurement(measurement),
      child: card,
    );
  }

  Widget _buildMessageInput() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.grey.withValues(alpha: 0.1),
            blurRadius: 4,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: Row(
        children: [
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                color: Colors.grey[100],
                borderRadius: BorderRadius.circular(24),
                border: Border.all(color: Colors.grey[300]!),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _messageController,
                      maxLength: 2000,
                      decoration: const InputDecoration(
                        hintText: 'Nachricht ...',
                        hintStyle: TextStyle(color: Colors.grey),
                        border: InputBorder.none,
                        counterText: '',
                        contentPadding: EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 12,
                        ),
                      ),
                      onSubmitted: (_) => _sendMessage(),
                    ),
                  ),
                  IconButton(
                    icon: Icon(
                      FontAwesomeIcons.paperclip,
                      color: Colors.grey[600],
                      size: 20,
                    ),
                    tooltip: 'Anhang senden',
                    onPressed: _isSending ? null : _showAttachmentMenu,
                  ),
                  IconButton(
                    icon: const Icon(Icons.send, color: _ChatColors.accent),
                    tooltip: 'Senden',
                    onPressed: _isSending ? null : _sendMessage,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
