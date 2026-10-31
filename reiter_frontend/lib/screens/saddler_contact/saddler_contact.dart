import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/find_saddler/find_saddler.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart' show CustomAppBar;
import '../../widgets/person_avatar.dart';
import '../../widgets/sidenav.dart';
import '../../models/chat.dart';
import '../../services/chat_service.dart';
import 'chat_screen.dart';

class SaddlerContact extends StatefulWidget {
  const SaddlerContact({super.key});

  @override
  State<SaddlerContact> createState() => _SaddlerContactState();
}

class _SaddlerContactState extends State<SaddlerContact> {
  final ChatService _chatService = ChatService();

  List<Chat> get _chats => _chatService.chats;

  @override
  void initState() {
    super.initState();
    _loadChats();
  }

  // start() lädt neu und verbindet erneut, falls die Backend-Person gewechselt hat
  Future<void> _loadChats() => _chatService.start();

  void _navigateToSaddlerMap() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => const FindSaddler(),
      ),
    ).then((_) => _loadChats()); 
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: _chatService,
      builder: (context, _) => Scaffold(
        backgroundColor: AppColors.surface,
        drawer: const SideNav(),
        appBar: const CustomAppBar(
          title: 'EquiPressure',
        ),
        body: _chatService.isLoading && _chats.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : _buildBody(),
        floatingActionButton: _chats.isNotEmpty
            ? FloatingActionButton(
                backgroundColor: AppColors.primary,
                onPressed: _navigateToSaddlerMap,
                child: const Icon(Icons.add, color: AppColors.text),
              )
            : null,
      ),
    );
  }

  Widget _buildBody() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 24, 20, 16),
          child: const Center(
          child: Text(
            'Sattler*innen',
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.bold,
            ),
          ),
        ), ),
        if (_chatService.error != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 4),
            child: Text(
              _chatService.error!,
              style: TextStyle(color: AppColors.error, fontSize: 13),
            ),
          ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _chatService.refreshChats,
            child: _chats.isEmpty ? _buildEmptyState() : _buildChatList(),
          ),
        ),
      ],
    );
  }

  Widget _buildEmptyState() {
    // scrollbar, damit Ziehen zum Aktualisieren auch ohne Chats geht
    return ListView(
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 40),
          child: Center(child: _buildEmptyPrompt()),
        ),
      ],
    );
  }

  Widget _buildEmptyPrompt() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(40),
        child: GestureDetector(
          onTap: _navigateToSaddlerMap,
          child: CustomPaint(
            painter: DashedBorderPainter(
              color: AppColors.border,
              strokeWidth: 1.5,
              gap: 5,
            ),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 48, vertical: 40),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.location_on_outlined,
                    size: 40,
                    color: AppColors.textMuted,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Füge eine*n Sattler*in hinzu\num zu chatten',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: AppColors.textMuted,
                      fontSize: 14,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildChatList() {
    return ListView.builder(
      itemCount: _chats.length,
      itemBuilder: (context, index) {
        final chat = _chats[index];
        return _ChatListItem(
          chat: chat,
          onTap: () async {
            await Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => ChatScreen(chat: chat),
              ),
            );
            _loadChats();
          },
        );
      },
    );
  }
}

class _ChatListItem extends StatelessWidget {
  final Chat chat;
  final VoidCallback onTap;

  const _ChatListItem({
    required this.chat,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: AppColors.purple100,
              width: 1,
            ),
          ),
        ),
        child: Row(
          children: [
            _buildAvatar(),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    chat.contactName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    chat.lastMessagePreview,
                    style: TextStyle(
                      color: chat.unreadCount > 0 ? AppColors.text : AppColors.textSoft,
                      fontWeight: chat.unreadCount > 0 ? FontWeight.w600 : FontWeight.normal,
                      fontSize: 14,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  chat.formattedLastMessageTime,
                  style: TextStyle(
                    color: AppColors.textMuted,
                    fontSize: 12,
                  ),
                ),
                if (chat.unreadCount > 0) ...[
                  const SizedBox(height: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
                    decoration: BoxDecoration(
                      color: AppColors.primary,
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Text(
                      '${chat.unreadCount}',
                      style: const TextStyle(
                        color: AppColors.surface,
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAvatar() {
    return PersonAvatar(
      personId: chat.contactId,
      imageUpdatedAt: chat.contactImageUpdatedAt,
      name: chat.contactName,
    );
  }
}

class DashedBorderPainter extends CustomPainter {
  final Color color;
  final double strokeWidth;
  final double gap;

  DashedBorderPainter({
    required this.color,
    this.strokeWidth = 1,
    this.gap = 5,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = strokeWidth
      ..style = PaintingStyle.stroke;

    final path = Path()
      ..addRRect(RRect.fromRectAndRadius(
        Rect.fromLTWH(0, 0, size.width, size.height),
        const Radius.circular(12),
      ));

    final dashPath = Path();
    for (final metric in path.computeMetrics()) {
      double distance = 0;
      while (distance < metric.length) {
        dashPath.addPath(
          metric.extractPath(distance, distance + gap),
          Offset.zero,
        );
        distance += gap * 2;
      }
    }

    canvas.drawPath(dashPath, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}