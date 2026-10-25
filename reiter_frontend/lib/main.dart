import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'screens/horses/horses_screen.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      // deutsche Texte in Material-Widgets, z.B. Monatsnamen und Knöpfe im Datumswähler
      locale: const Locale('de', 'AT'),
      supportedLocales: const [Locale('de', 'AT'), Locale('de')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: const HorsesScreen(),
    );
  }
}
