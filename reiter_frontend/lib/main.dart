import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'screens/onboarding/auth_gate.dart';
import 'services/app_navigation.dart';
import 'theme/app_colors.dart';
import 'theme/app_theme.dart';
import 'widgets/chat_notification_listener.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      navigatorKey: AppNavigation.navigatorKey,
      scaffoldMessengerKey: AppNavigation.scaffoldMessengerKey,
      // deutsche Texte in Material-Widgets, z.B. Monatsnamen und Knöpfe im Datumswähler
      locale: const Locale('de', 'AT'),
      supportedLocales: const [Locale('de', 'AT'), Locale('de')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      // neue Chat-Nachrichten werden auf jedem Screen benachrichtigt
      // Android zeichnet die App bis unter die Navigationsleiste, Inhalte müssen darüber enden
      builder: (context, child) => ColoredBox(
        color: AppColors.surface,
        child: SafeArea(
          top: false,
          left: false,
          right: false,
          child: ChatNotificationListener(child: child ?? const SizedBox.shrink()),
        ),
      ),
      home: const AuthGate(),
    );
  }
}
