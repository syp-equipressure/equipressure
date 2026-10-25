import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '../../models/chat.dart';
import '../../models/horse.dart';
import '../../models/measurement.dart';
import '../../models/message.dart';
import '../../services/chat_service.dart';
import '../../services/horse_service.dart';
import '../../widgets/chat_attachment_menu.dart';
import '../../widgets/horse_selection_dialog.dart';
import '../../widgets/measurement_selection_dialog.dart';

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
          'ein Pferd bzw. eine Messung über die Büroklammer.',
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
      title: Row(
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
            decoration: BoxDecoration(
              color:
                  isFromMe ? const Color(0xFFD6EAF8) : const Color(0xFFFCE4EC),
              borderRadius: BorderRadius.circular(16),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (message.type != MessageType.text) _buildShareCard(message),
                if (message.text != null && message.text!.isNotEmpty)
                  Text(
                    message.text!,
                    style: const TextStyle(fontSize: 15, color: Colors.black87),
                  ),
                const SizedBox(height: 4),
                Text(
                  _formatMeta(message, isFromMe),
                  style: TextStyle(fontSize: 11, color: Colors.grey[600]),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  String _formatMeta(Message message, bool isFromMe) {
    final time = TimeOfDay.fromDateTime(message.sentAt);
    final text =
        '${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}';
    return isFromMe && message.readAt != null ? '$text · Gelesen' : text;
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
          color: const Color(0xFFF3E5F5),
          borderRadius: BorderRadius.circular(999),
        ),
        child: Text(label, style: TextStyle(fontSize: 11, color: Colors.grey[700])),
      ),
    );
  }

  Widget _buildShareCard(Message message) {
    final horse = message.horse;
    final measurement = message.measurement;
    final date = measurement?.date;

    return Container(
      margin: const EdgeInsets.only(bottom: 6),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.7),
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
                  const ColorFilter.mode(Color(0xFF7C4DFF), BlendMode.srcIn),
            )
          else
            const Icon(Icons.analytics_outlined, color: Color(0xFF7C4DFF)),
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
        ],
      ),
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
                    tooltip: 'Pferd oder Messung teilen',
                    onPressed: _isSending ? null : _showAttachmentMenu,
                  ),
                  IconButton(
                    icon: Icon(Icons.send, color: Colors.purple[400]),
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
