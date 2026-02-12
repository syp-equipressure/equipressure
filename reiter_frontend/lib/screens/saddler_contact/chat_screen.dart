import 'dart:async';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:image_picker/image_picker.dart';
import 'package:file_picker/file_picker.dart';
import '../../models/chat.dart';
import '../../models/message.dart';
import '../../models/horse.dart';
import '../../services/chat_service.dart';
import '../../services/horse_service.dart';
import '../../widgets/chat_attachment_menu.dart';
import '../../widgets/horse_selection_dialog.dart';
import '../../widgets/horse_chat_card.dart';
import '../../widgets/chat_image_message.dart';
import '../../widgets/chat_file_message.dart';

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
  final ImagePicker _imagePicker = ImagePicker();

  late List<Message> _messages;
  List<Horse> _horses = [];
  StreamSubscription? _messageSubscription;

  @override
  void initState() {
    super.initState();
    _messages = List.from(widget.chat.messages);
    _loadHorses();
    _listenForNewMessages();
    WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
  }

  Future<void> _loadHorses() async {
    final horses = await _horseService.getHorses();
    setState(() {
      _horses = horses;
    });
  }

  void _listenForNewMessages() {
    _messageSubscription = _chatService.newMessages.listen((message) {
      if (message.chatId == widget.chat.id && !message.isFromMe) {
        setState(() {
          _messages.add(message);
        });
        WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
      }
    });
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

  void _sendMessage() {
    final text = _messageController.text.trim();
    if (text.isEmpty) return;

    final message = Message(
      id: 'msg_${DateTime.now().millisecondsSinceEpoch}',
      chatId: widget.chat.id,
      senderId: 'user1',
      content: text,
      timestamp: DateTime.now(),
      isFromMe: true,
      type: MessageType.text,
    );

    setState(() {
      _messages.add(message);
    });

    _chatService.sendMessage(widget.chat.id, message);
    _messageController.clear();

    WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
  }

  void _showAttachmentMenu() {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => ChatAttachmentMenu(
        onFilesSelected: () {
          Navigator.pop(context);
          _pickFiles();
        },
        onImagesSelected: () {
          Navigator.pop(context);
          _pickImages();
        },
        onProfileSelected: () {
          Navigator.pop(context);
          // Profile sharing not implemented
        },
        onHorsesSelected: () {
          Navigator.pop(context);
          _showHorseSelectionDialog();
        },
      ),
    );
  }

  Future<void> _pickImages() async {
    try {
      final List<XFile> images = await _imagePicker.pickMultiImage(
        imageQuality: 80,
        maxWidth: 1920,
        maxHeight: 1920,
      );

      if (images.isEmpty) return;

      final attachments = images.map((image) {
        return MessageAttachment(
          id: 'att_${DateTime.now().millisecondsSinceEpoch}_${image.name}',
          fileName: image.name,
          filePath: image.path,
          mimeType: 'image/${image.name.split('.').last}',
        );
      }).toList();

      final message = Message(
        id: 'msg_${DateTime.now().millisecondsSinceEpoch}',
        chatId: widget.chat.id,
        senderId: 'user1',
        content: '',
        timestamp: DateTime.now(),
        isFromMe: true,
        type: MessageType.image,
        attachments: attachments,
      );

      setState(() {
        _messages.add(message);
      });

      _chatService.sendMessage(widget.chat.id, message);
      WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
    } catch (e) {
      // Handle error
    }
  }

  Future<void> _pickFiles() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        allowMultiple: true,
        type: FileType.any,
      );

      if (result == null || result.files.isEmpty) return;

      final attachments = result.files.map((file) {
        return MessageAttachment(
          id: 'att_${DateTime.now().millisecondsSinceEpoch}_${file.name}',
          fileName: file.name,
          filePath: file.path ?? '',
          mimeType: _getMimeType(file.name),
          fileSize: file.size,
        );
      }).toList();

      final message = Message(
        id: 'msg_${DateTime.now().millisecondsSinceEpoch}',
        chatId: widget.chat.id,
        senderId: 'user1',
        content: '',
        timestamp: DateTime.now(),
        isFromMe: true,
        type: MessageType.file,
        attachments: attachments,
      );

      setState(() {
        _messages.add(message);
      });

      _chatService.sendMessage(widget.chat.id, message);
      WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
    } catch (e) {
      // Handle error
    }
  }

  String _getMimeType(String fileName) {
    final ext = fileName.toLowerCase().split('.').last;
    switch (ext) {
      case 'pdf':
        return 'application/pdf';
      case 'doc':
      case 'docx':
        return 'application/msword';
      case 'xls':
      case 'xlsx':
        return 'application/vnd.ms-excel';
      case 'ppt':
      case 'pptx':
        return 'application/vnd.ms-powerpoint';
      case 'txt':
        return 'text/plain';
      case 'zip':
        return 'application/zip';
      case 'jpg':
      case 'jpeg':
        return 'image/jpeg';
      case 'png':
        return 'image/png';
      case 'gif':
        return 'image/gif';
      default:
        return 'application/octet-stream';
    }
  }

  void _showHorseSelectionDialog() {
    showDialog(
      context: context,
      builder: (context) => HorseSelectionDialog(
        horses: _horses,
        onHorsesSelected: (selectedHorses) {
          _sendHorsesMessage(selectedHorses);
        },
      ),
    );
  }

  void _sendHorsesMessage(List<Horse> selectedHorses) {
    if (selectedHorses.isEmpty) return;

    final message = Message(
      id: 'msg_${DateTime.now().millisecondsSinceEpoch}',
      chatId: widget.chat.id,
      senderId: 'user1',
      content: '',
      timestamp: DateTime.now(),
      isFromMe: true,
      type: MessageType.horses,
      attachedHorseIds: selectedHorses.map((h) => h.id).toList(),
    );

    setState(() {
      _messages.add(message);
    });

    _chatService.sendMessage(widget.chat.id, message);

    WidgetsBinding.instance.addPostFrameCallback((_) => _scrollToBottom());
  }

  @override
  void dispose() {
    _messageSubscription?.cancel();
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: _buildAppBar(),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              itemCount: _messages.length,
              itemBuilder: (context, index) {
                final message = _messages[index];
                return _buildMessageBubble(message);
              },
            ),
          ),
          _buildMessageInput(),
        ],
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
          _buildSaddlerAvatar(),
          const SizedBox(width: 12),
          Text(
            widget.chat.saddler.name,
            style: const TextStyle(
              color: Colors.black,
              fontWeight: FontWeight.w600,
              fontSize: 18,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSaddlerAvatar() {
    if (widget.chat.saddler.imagePath != null) {
      return CircleAvatar(
        radius: 18,
        backgroundImage: AssetImage(widget.chat.saddler.imagePath!),
      );
    }

    return CircleAvatar(
      radius: 18,
      backgroundColor: Colors.green[50],
      child: Text(
        _getInitials(widget.chat.saddler.name),
        style: TextStyle(
          color: Colors.green[700],
          fontWeight: FontWeight.bold,
          fontSize: 12,
        ),
      ),
    );
  }

  String _getInitials(String name) {
    final parts = name.split(' ');
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }

  Widget _buildMessageBubble(Message message) {
    switch (message.type) {
      case MessageType.horses:
        return _buildHorsesMessage(message);
      case MessageType.image:
        return ChatImageMessage(message: message);
      case MessageType.file:
        return ChatFileMessage(message: message);
      case MessageType.text:
        return _buildTextMessage(message);
      case MessageType.profile:
        throw UnimplementedError();
    }
  }

  Widget _buildTextMessage(Message message) {
    final isFromMe = message.isFromMe;

    return Align(
      alignment: isFromMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 4),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        decoration: BoxDecoration(
          color: isFromMe ? const Color(0xFFD6EAF8) : const Color(0xFFFCE4EC),
          borderRadius: BorderRadius.circular(16),
        ),
        child: Text(
          message.content,
          style: const TextStyle(
            fontSize: 15,
            color: Colors.black87,
          ),
        ),
      ),
    );
  }

  Widget _buildHorsesMessage(Message message) {
    final horseIds = message.attachedHorseIds ?? [];
    final attachedHorses =
        _horses.where((h) => horseIds.contains(h.id)).toList();

    return Align(
      alignment: message.isFromMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 4),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        child: Column(
          crossAxisAlignment: message.isFromMe
              ? CrossAxisAlignment.end
              : CrossAxisAlignment.start,
          children: attachedHorses.map((horse) {
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: HorseChatCard(horse: horse),
            );
          }).toList(),
        ),
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
                      decoration: const InputDecoration(
                        hintText: 'Nachricht ...',
                        hintStyle: TextStyle(color: Colors.grey),
                        border: InputBorder.none,
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
                    onPressed: _showAttachmentMenu,
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
