import 'dart:async';
import 'package:flutter/material.dart';
import '../screens/saddler_contact/chat_screen.dart';
import '../services/app_navigation.dart';
import '../services/chat_service.dart';

/// Startet den Chat beim App-Start und zeigt bei jeder neuen Nachricht eine
/// Benachrichtigung, über die der Chat direkt geöffnet werden kann.
class ChatNotificationListener extends StatefulWidget {
  final Widget child;

  const ChatNotificationListener({super.key, required this.child});

  @override
  State<ChatNotificationListener> createState() =>
      _ChatNotificationListenerState();
}

class _ChatNotificationListenerState extends State<ChatNotificationListener> {
  StreamSubscription<ChatNotification>? _subscription;

  @override
  void initState() {
    super.initState();
    _subscription = ChatService().notifications.listen(_show);
    ChatService().start();
  }

  void _show(ChatNotification notification) {
    final messenger = AppNavigation.scaffoldMessengerKey.currentState;
    if (messenger == null) return;

    messenger.hideCurrentSnackBar();
    messenger.showSnackBar(
      SnackBar(
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 5),
        backgroundColor: const Color(0xFF4A3B6B),
        content: Row(
          children: [
            const Icon(Icons.chat_bubble, color: Colors.white, size: 20),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    notification.chat.contactName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  Text(
                    notification.message.preview,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ],
        ),
        action: SnackBarAction(
          label: 'Öffnen',
          textColor: const Color(0xFFD1C4E9),
          onPressed: () => AppNavigation.navigatorKey.currentState?.push(
            MaterialPageRoute(
              builder: (_) => ChatScreen(chat: notification.chat),
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
