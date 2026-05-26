import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/find_saddler/find_saddler.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart' show CustomAppBar;
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
  List<Chat> _chats = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadChats();
  }

  Future<void> _loadChats() async {
    final chats = await _chatService.getChats();
    setState(() {
      _chats = chats;
      _isLoading = false;
    });
  }

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
    return Scaffold(
      backgroundColor: Colors.white,
      drawer: const SideNav(),
      appBar: const CustomAppBar(
        title: 'EquiPressure',
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _buildBody(),
      floatingActionButton: _chats.isNotEmpty
          ? FloatingActionButton(
              backgroundColor: const Color.fromARGB(255, 178, 149, 230),
              onPressed: _navigateToSaddlerMap,
              child: const Icon(Icons.add, color: Color.fromARGB(255, 83, 83, 83)),
            )
          : null,
    );
  }

  Widget _buildBody() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 24, 20, 16),
          child: const Center(
          child: const Text(
            'Sattler*innen',
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.bold,
            ),
          ),
        ), ),
        Expanded(
          child: _chats.isEmpty ? _buildEmptyState() : _buildChatList(),
        ),
      ],
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(40),
        child: GestureDetector(
          onTap: _navigateToSaddlerMap,
          child: CustomPaint(
            painter: DashedBorderPainter(
              color: Colors.grey[300]!,
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
                    color: Colors.grey[400],
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Füge eine*n Sattler*in hinzu\num zu chatten',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: Colors.grey[500],
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
              color: Colors.purple[100]!,
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
                    chat.saddler.name,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    chat.lastMessagePreview,
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontSize: 14,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
            Text(
              chat.formattedLastMessageTime,
              style: TextStyle(
                color: Colors.grey[400],
                fontSize: 12,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAvatar() {
    if (chat.saddler.imagePath != null) {
      return CircleAvatar(
        radius: 28,
        backgroundImage: AssetImage(chat.saddler.imagePath!),
      );
    }

    return CircleAvatar(
      radius: 28,
      backgroundColor: Colors.green[50],
      child: Text(
        _getInitials(chat.saddler.name),
        style: TextStyle(
          color: Colors.green[700],
          fontWeight: FontWeight.bold,
          fontSize: 16,
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