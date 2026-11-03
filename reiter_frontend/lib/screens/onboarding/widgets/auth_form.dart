import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '../../../theme/app_colors.dart';

/// Eingabefeld von Login und Registrierung: groß und rund, der Name steht als Platzhalter im Feld
InputDecoration authInputDecoration(String hint) {
  OutlineInputBorder border(Color color, double width) => OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: color, width: width),
      );

  return InputDecoration(
    hintText: hint,
    contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 15),
    border: border(AppColors.border, 1),
    enabledBorder: border(AppColors.border, 1),
    focusedBorder: border(AppColors.primary, 2),
  );
}

/// Auge zum Ein- und Ausblenden des Passworts
class PasswordVisibilityButton extends StatelessWidget {
  final bool visible;
  final VoidCallback onPressed;

  const PasswordVisibilityButton({
    super.key,
    required this.visible,
    required this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: visible ? 'Passwort ausblenden' : 'Passwort einblenden',
      icon: Icon(
        visible ? Icons.visibility_off_outlined : Icons.visibility_outlined,
        color: AppColors.textMuted,
      ),
      onPressed: onPressed,
    );
  }
}

/// Überschrift mit Untertitel über dem Formular
class AuthHeading extends StatelessWidget {
  final String title;
  final String subtitle;

  const AuthHeading({super.key, required this.title, required this.subtitle});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Image.asset(
          'assets/images/equipressure-logo.png',
          height: 44,
          semanticLabel: 'EquiPressure',
        ),
        const SizedBox(height: 20),
        Text(
          title,
          textAlign: TextAlign.center,
          style: const TextStyle(
            fontSize: 28,
            fontWeight: FontWeight.w600,
            color: AppColors.textStrong,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          subtitle,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 14, color: AppColors.textSoft),
        ),
      ],
    );
  }
}

/// Trennlinie mit Text in der Mitte
class AuthDivider extends StatelessWidget {
  final String label;

  const AuthDivider({super.key, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        const Expanded(child: Divider()),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: AppColors.textSoft),
          ),
        ),
        const Expanded(child: Divider()),
      ],
    );
  }
}

/// Google, Microsoft und GitHub nebeneinander. Neue Benutzer legt Keycloak beim ersten Mal selbst an,
/// [action] ist nur der Text für die Beschreibung ("anmelden" oder "registrieren").
class AuthProviderButtons extends StatelessWidget {
  final String action;
  final bool enabled;
  final ValueChanged<String> onSelected;

  const AuthProviderButtons({
    super.key,
    required this.action,
    required this.enabled,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(child: _buildButton(FontAwesomeIcons.google, 'Google', 'google')),
        const SizedBox(width: 10),
        Expanded(child: _buildButton(FontAwesomeIcons.microsoft, 'Microsoft', 'microsoft')),
        const SizedBox(width: 10),
        Expanded(child: _buildButton(FontAwesomeIcons.github, 'GitHub', 'github')),
      ],
    );
  }

  Widget _buildButton(IconData icon, String name, String provider) {
    return Tooltip(
      message: 'Mit $name $action',
      child: OutlinedButton(
        onPressed: enabled ? () => onSelected(provider) : null,
        style: OutlinedButton.styleFrom(
          backgroundColor: AppColors.surfaceVariant,
          foregroundColor: AppColors.textStrong,
          side: const BorderSide(color: AppColors.border),
          padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 12),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 20, color: AppColors.purple600),
            const SizedBox(height: 6),
            FittedBox(
              fit: BoxFit.scaleDown,
              child: Text(
                name,
                maxLines: 1,
                style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w500),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Hauptbutton mit Verlauf im Violett der App, wie auf der Keycloak-Seite
class AuthGradientButton extends StatelessWidget {
  final String label;
  final bool loading;
  final VoidCallback onPressed;

  const AuthGradientButton({
    super.key,
    required this.label,
    required this.loading,
    required this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(999),
        gradient: const LinearGradient(colors: [AppColors.purple400, AppColors.purple600]),
        boxShadow: [
          BoxShadow(
            color: AppColors.purple600.withValues(alpha: 0.28),
            blurRadius: 18,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: ElevatedButton(
        onPressed: loading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.transparent,
          disabledBackgroundColor: Colors.transparent,
          shadowColor: Colors.transparent,
          foregroundColor: AppColors.onPrimary,
          minimumSize: const Size.fromHeight(50),
          shape: const StadiumBorder(),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
        child: loading
            ? const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.onPrimary),
              )
            : Text(label),
      ),
    );
  }
}
