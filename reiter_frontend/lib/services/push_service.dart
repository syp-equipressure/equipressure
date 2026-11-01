import 'dart:async';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/saddler_contact/chat_screen.dart';
import 'package:reiterappfrontend/services/app_navigation.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';
import 'package:reiterappfrontend/services/chat_service.dart';
import 'package:reiterappfrontend/services/person_service.dart';

/// Benachrichtigungen am Handy über Firebase Cloud Messaging, auch bei geschlossener App.
/// Bei offener App zeigt der [ChatNotificationListener] neue Nachrichten an.
class PushService {
  static final PushService _instance = PushService._internal();
  factory PushService() => _instance;
  PushService._internal();

  StreamSubscription<String>? _tokenSubscription;
  StreamSubscription<RemoteMessage>? _openSubscription;
  String? _personId;

  static bool get _isSupported => !kIsWeb && defaultTargetPlatform == TargetPlatform.android;

  /// Fragt nach der Erlaubnis und meldet dieses Handy für die angemeldete Person an.
  /// Ein Fehler stört die App nicht, es gibt dann nur keine Benachrichtigungen.
  Future<void> start() async {
    if (!_isSupported) return;

    try {
      final personId = await BackendPersonContextService.loadPersonId();
      if (personId == null || personId == _personId) return;

      await Firebase.initializeApp();
      final messaging = FirebaseMessaging.instance;
      await messaging.requestPermission();

      final token = await messaging.getToken();
      if (token != null) await PersonService.registerPushToken(personId, token);
      _personId = personId;

      await _tokenSubscription?.cancel();
      _tokenSubscription = messaging.onTokenRefresh.listen(
        (token) => PersonService.registerPushToken(personId, token),
      );

      // Tippen auf die Benachrichtigung bei App im Hintergrund oder geschlossener App
      await _openSubscription?.cancel();
      _openSubscription = FirebaseMessaging.onMessageOpenedApp.listen(_openChat);
      final initialMessage = await messaging.getInitialMessage();
      if (initialMessage != null) _openChat(initialMessage);
    } catch (error) {
      debugPrint('Benachrichtigungen nicht verfügbar: $error');
    }
  }

  /// Beim Abmelden bekommt dieses Handy keine Nachrichten dieser Person mehr.
  Future<void> stop() async {
    final personId = _personId;
    if (personId == null) return;

    _personId = null;
    await _tokenSubscription?.cancel();
    await _openSubscription?.cancel();
    try {
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) await PersonService.removePushToken(personId, token);
    } catch (error) {
      debugPrint('Abmelden von Benachrichtigungen fehlgeschlagen: $error');
    }
  }

  void _openChat(RemoteMessage message) {
    final contactId = message.data['contactId'] as String?;
    if (contactId == null) return;

    final contactName = message.data['contactName'] as String? ?? '';
    AppNavigation.navigatorKey.currentState?.push(
      MaterialPageRoute(
        builder: (_) => ChatScreen(chat: ChatService().chatWith(contactId, contactName)),
      ),
    );
  }
}
