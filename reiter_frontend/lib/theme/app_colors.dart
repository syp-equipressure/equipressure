import 'package:flutter/material.dart';

/// Die Farben der App, dieselbe Palette wie im Sattler-Frontend
/// (sattler_frontend/src/styles.scss): violette Akzente auf neutralen Flächen.
/// In den Screens nur diese Farben verwenden, keine eigenen Farbwerte.
class AppColors {
  AppColors._();

  // Violett, von hell nach dunkel
  static const purple50 = Color(0xFFF4F2FB);
  static const purple100 = Color(0xFFEAE6F7);
  static const purple200 = Color(0xFFD5CDED);
  static const purple300 = Color(0xFFB9ACDE);
  static const purple400 = Color(0xFF8F7BC7);
  static const purple500 = Color(0xFF6955AE);
  static const purple600 = Color(0xFF55438F);
  static const purple700 = Color(0xFF40336B);

  // Flächen
  static const bgPage = Color(0xFFF5F6F8);
  static const bgCard = Color(0xFFFFFFFF);
  static const bgPanel = Color(0xFFFAFAFC);

  // Linien
  static const border = Color(0xFFDDE1E7);
  static const borderSoft = Color(0xFFE7E9EE);

  // Text
  static const textStrong = Color(0xFF18212F);
  static const text = Color(0xFF293342);
  static const textSoft = Color(0xFF5F6B7A);
  static const textMuted = Color(0xFF8993A0);

  // Akzente
  static const accentRed = Color(0xFFE53935);
  static const accentOrange = Color(0xFFF2A23A);
  static const accentGreen = Color(0xFF2E7D32);

  // Bedeutungen, für die Screens
  static const primary = purple500;
  static const primaryDark = purple600;
  static const primaryLight = purple100;
  static const onPrimary = bgCard;
  static const background = bgPage;
  static const surface = bgCard;
  static const surfaceVariant = bgPanel;
  static const textPrimary = textStrong;
  static const textSecondary = textSoft;
  static const textHint = textMuted;
  static const divider = borderSoft;
  static const inactive = textMuted;
  static const error = accentRed;
  static const errorLight = Color(0xFFFDECEA);
  static const warning = accentOrange;
  static const warningLight = Color(0xFFFEF4E6);
  static const success = accentGreen;
  static const successLight = Color(0xFFE8F5E9);

  /// Schatten wie im Sattler-Frontend (rgba(24, 33, 47, …))
  static Color shadow(double opacity) => textStrong.withValues(alpha: opacity);
}
