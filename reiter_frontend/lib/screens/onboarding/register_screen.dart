import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
import 'widgets/auth_form.dart';

/// Neues Konto erstellen, entweder über einen Anbieter (Keycloak im Browser der App) oder mit E-Mail
/// und Passwort (das Backend legt den Benutzer in Keycloak an). Danach ist man angemeldet und der
/// Screen gibt true zurück.
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  /// Gleich wie die Passwort-Richtlinie in backend/keycloak/realm-equipressure.json
  static const int _minPasswordLength = 8;

  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _passwordRepeatController = TextEditingController();

  bool _loading = false;
  bool _passwordVisible = false;
  String? _error;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _passwordRepeatController.dispose();
    super.dispose();
  }

  String? _validate() {
    final email = _emailController.text.trim();
    if (_firstNameController.text.trim().isEmpty || _lastNameController.text.trim().isEmpty) {
      return 'Bitte gib deinen Vor- und Nachnamen ein.';
    }
    if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
      return 'Bitte gib eine gültige E-Mail-Adresse ein.';
    }
    if (_passwordController.text.length < _minPasswordLength) {
      return 'Das Passwort braucht mindestens $_minPasswordLength Zeichen.';
    }
    if (_passwordController.text != _passwordRepeatController.text) {
      return 'Die Passwörter stimmen nicht überein.';
    }
    return null;
  }

  Future<void> _register() async {
    final problem = _validate();
    if (problem != null) {
      setState(() => _error = problem);
      return;
    }

    await _run(() async {
      await AuthService().register(
        email: _emailController.text.trim(),
        password: _passwordController.text,
        firstName: _firstNameController.text.trim(),
        lastName: _lastNameController.text.trim(),
      );
      TextInput.finishAutofillContext();
    });
  }

  /// Führt die Registrierung aus und schließt danach den Screen
  Future<void> _run(Future<void> Function() register) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await register();
      if (mounted) Navigator.pop(context, true);
    } on EmailAlreadyUsedException {
      _showError('Zu dieser E-Mail gibt es schon ein Konto.');
    } on TooManyAttemptsException {
      _showError('Zu viele Versuche. Bitte warte kurz und probiere es noch einmal.');
    } on FlutterAppAuthUserCancelledException {
      // Seite im Browser geschlossen
    } on PlatformException {
      _showError('Die Anmeldeseite ist gerade nicht erreichbar.');
    } on TimeoutException {
      _showError('Der Server ist gerade nicht erreichbar.');
    } catch (_) {
      _showError('Das Konto konnte nicht erstellt werden. Bitte versuche es noch einmal.');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _showError(String message) {
    if (mounted) setState(() => _error = message);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            child: AutofillGroup(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const AuthHeading(
                    title: 'Konto erstellen',
                    subtitle: 'In einer Minute bist du startklar.',
                  ),
                  const SizedBox(height: 24),
                  AuthProviderButtons(
                    action: 'registrieren',
                    enabled: !_loading,
                    onSelected: (provider) => _run(() => AuthService().loginWithProvider(provider)),
                  ),
                  const SizedBox(height: 18),
                  const AuthDivider(label: 'Oder mit E-Mail'),
                  const SizedBox(height: 18),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _firstNameController,
                          textInputAction: TextInputAction.next,
                          textCapitalization: TextCapitalization.words,
                          autofillHints: const [AutofillHints.givenName],
                          decoration: authInputDecoration('Vorname'),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: TextField(
                          controller: _lastNameController,
                          textInputAction: TextInputAction.next,
                          textCapitalization: TextCapitalization.words,
                          autofillHints: const [AutofillHints.familyName],
                          decoration: authInputDecoration('Nachname'),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _emailController,
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.next,
                    autocorrect: false,
                    autofillHints: const [AutofillHints.email],
                    decoration: authInputDecoration('E-Mail'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _passwordController,
                    obscureText: !_passwordVisible,
                    textInputAction: TextInputAction.next,
                    autofillHints: const [AutofillHints.newPassword],
                    decoration: authInputDecoration('Passwort').copyWith(
                      suffixIcon: PasswordVisibilityButton(
                        visible: _passwordVisible,
                        onPressed: () => setState(() => _passwordVisible = !_passwordVisible),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _passwordRepeatController,
                    obscureText: !_passwordVisible,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.newPassword],
                    onSubmitted: (_) => _register(),
                    decoration: authInputDecoration('Passwort wiederholen'),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Text(
                      _error!,
                      textAlign: TextAlign.center,
                      style: const TextStyle(fontSize: 13, color: AppColors.error),
                    ),
                  ],
                  const SizedBox(height: 18),
                  AuthGradientButton(
                    label: 'Konto erstellen',
                    loading: _loading,
                    onPressed: _register,
                  ),
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text(
                        'Schon ein Konto?',
                        style: TextStyle(fontSize: 14, color: AppColors.textSoft),
                      ),
                      TextButton(
                        onPressed: _loading ? null : () => Navigator.pop(context),
                        style: TextButton.styleFrom(
                          padding: const EdgeInsets.symmetric(horizontal: 6),
                          textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                        ),
                        child: const Text('Anmelden'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
