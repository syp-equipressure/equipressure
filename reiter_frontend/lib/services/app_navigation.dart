import 'package:flutter/material.dart';

/// Schlüssel für Navigation und Snackbars außerhalb eines BuildContext,
/// z.B. für Benachrichtigungen über neue Chat-Nachrichten.
class AppNavigation {
  static final navigatorKey = GlobalKey<NavigatorState>();
  static final scaffoldMessengerKey = GlobalKey<ScaffoldMessengerState>();
}
