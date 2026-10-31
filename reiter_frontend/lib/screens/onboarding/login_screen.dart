import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../services/account_service.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
import 'register_screen.dart';
import 'widgets/auth_form.dart';
import 'widgets/page_indicator.dart';

/// Anmelden mit E-Mail und Passwort direkt in der App. "Passwort vergessen" und die Anbieter
/// laufen über Keycloak im Browser der App (backend/keycloak/themes/equipressure).
class LoginScreen extends StatefulWidget {
  final ValueChanged<AccountState> onLoggedIn;

  const LoginScreen({super.key, required this.onLoggedIn});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _loading = false;
  bool _remember = true;
  bool _passwordVisible = false;
  String? _error;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _loginWithPassword() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    if (email.isEmpty || password.isEmpty) {
      setState(() => _error = 'Bitte E-Mail und Passwort eingeben.');
      return;
    }

    await _run(() async {
      await AuthService().loginWithPassword(email, password, remember: _remember);
      TextInput.finishAutofillContext();
    });
  }

  /// Führt eine Anmeldung aus und geht danach im Onboarding weiter
  Future<void> _run(Future<void> Function() login) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await login();
      final state = await AccountService.loadState();
      widget.onLoggedIn(state);
    } on WrongCredentialsException {
      _showError('E-Mail oder Passwort ist falsch.');
    } on FlutterAppAuthUserCancelledException {
      // Seite im Browser geschlossen
    } on PlatformException {
      _showError('Die Anmeldeseite ist gerade nicht erreichbar.');
    } on TimeoutException {
      _showError('Der Server ist gerade nicht erreichbar.');
    } catch (_) {
      _showError('Anmelden hat nicht funktioniert. Bitte versuche es noch einmal.');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _showError(String message) {
    if (mounted) setState(() => _error = message);
  }

  Future<void> _openRegister() async {
    final registered = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => const RegisterScreen()),
    );
    // angemeldet ist man dann schon, es geht direkt im Onboarding weiter
    if (registered == true) await _run(() async {});
  }

  Future<void> _openResetPassword() async {
    final opened = await launchUrl(
      AuthService().resetPasswordUrl,
      mode: LaunchMode.inAppBrowserView,
    );
    if (!opened) _showError('Die Seite konnte nicht geöffnet werden.');
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.surface,
      child: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: Center(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                  child: AutofillGroup(child: _buildForm()),
                ),
              ),
            ),
            const PageIndicator(currentPage: 1, totalPages: 5),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  Widget _buildForm() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const AuthHeading(
          title: 'Willkommen zurück',
          subtitle: 'Schön, dass du wieder da bist.',
        ),
        const SizedBox(height: 24),
        TextField(
          controller: _emailController,
          keyboardType: TextInputType.emailAddress,
          textInputAction: TextInputAction.next,
          autocorrect: false,
          autofillHints: const [AutofillHints.email, AutofillHints.username],
          decoration: authInputDecoration('E-Mail'),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _passwordController,
          obscureText: !_passwordVisible,
          textInputAction: TextInputAction.done,
          autofillHints: const [AutofillHints.password],
          onSubmitted: (_) => _loginWithPassword(),
          decoration: authInputDecoration('Passwort').copyWith(
            suffixIcon: PasswordVisibilityButton(
              visible: _passwordVisible,
              onPressed: () => setState(() => _passwordVisible = !_passwordVisible),
            ),
          ),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            SizedBox(
              width: 24,
              height: 24,
              child: Checkbox(
                value: _remember,
                onChanged: (value) => setState(() => _remember = value ?? true),
              ),
            ),
            const SizedBox(width: 8),
            const Expanded(
              child: Text(
                'Angemeldet bleiben',
                style: TextStyle(fontSize: 13, color: AppColors.textSoft),
              ),
            ),
            TextButton(
              onPressed: _loading ? null : _openResetPassword,
              style: TextButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 4),
                textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w500),
              ),
              child: const Text('Passwort vergessen?'),
            ),
          ],
        ),
        if (_error != null) ...[
          const SizedBox(height: 4),
          Text(
            _error!,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 13, color: AppColors.error),
          ),
        ],
        const SizedBox(height: 12),
        AuthGradientButton(
          label: 'Anmelden',
          loading: _loading,
          onPressed: _loginWithPassword,
        ),
        const SizedBox(height: 22),
        const AuthDivider(label: 'Oder anmelden mit'),
        const SizedBox(height: 16),
        AuthProviderButtons(
          action: 'anmelden',
          enabled: !_loading,
          onSelected: (provider) => _run(() => AuthService().loginWithProvider(provider)),
        ),
        const SizedBox(height: 16),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Text(
              'Noch kein Konto?',
              style: TextStyle(fontSize: 14, color: AppColors.textSoft),
            ),
            TextButton(
              onPressed: _loading ? null : _openRegister,
              style: TextButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 6),
                textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
              ),
              child: const Text('Registrieren'),
            ),
          ],
        ),
      ],
    );
  }
}
