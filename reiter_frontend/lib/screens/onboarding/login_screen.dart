import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '../../services/account_service.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import '../../widgets/common/app_button.dart';
import 'widgets/page_indicator.dart';

/// Login über Keycloak, die Buttons gehen direkt zum jeweiligen Anbieter
class LoginScreen extends StatefulWidget {
  final ValueChanged<AccountState> onLoggedIn;

  const LoginScreen({super.key, required this.onLoggedIn});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  bool _loading = false;

  /// Ohne [identityProvider] öffnet sich die Keycloak-Seite selbst (für die Testkonten)
  Future<void> _login(String? identityProvider) async {
    setState(() => _loading = true);
    try {
      await AuthService().login(identityProvider: identityProvider);
      final state = await AccountService.loadState();
      widget.onLoggedIn(state);
    } on PlatformException {
      // Login im Browser abgebrochen
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Anmelden hat nicht funktioniert. Bitte versuche es noch einmal.'),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.background,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Spacer(),
              const Text('Login mit', style: AppTextStyles.h1),
              const SizedBox(height: 16),
              const Text(
                'Eine Registrierung ist nicht nötig, melde dich einfach mit einem bestehenden Konto an.',
                textAlign: TextAlign.center,
                style: AppTextStyles.body,
              ),
              const SizedBox(height: 32),
              if (_loading)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: CircularProgressIndicator(),
                )
              else ...[
                AppSocialButton(
                  icon: FontAwesomeIcons.google,
                  label: 'Mit Google anmelden',
                  onPressed: () => _login('google'),
                ),
                const SizedBox(height: 16),
                AppSocialButton(
                  icon: FontAwesomeIcons.microsoft,
                  label: 'Mit Microsoft anmelden',
                  onPressed: () => _login('microsoft'),
                ),
                const SizedBox(height: 16),
                AppSocialButton(
                  icon: FontAwesomeIcons.github,
                  label: 'Mit GitHub anmelden',
                  onPressed: () => _login('github'),
                ),
                // nur beim Entwickeln: Anmelden mit den Testkonten aus backend/keycloak
                // (z.B. reiter.dev / reiter), dafür KC_LOGIN_THEME=keycloak.v2 in der .env
                if (kDebugMode) ...[
                  const SizedBox(height: 16),
                  TextButton(
                    onPressed: () => _login(null),
                    child: const Text('Mit Testkonto anmelden (nur Entwicklung)'),
                  ),
                ],
              ],
              const Spacer(),
              const PageIndicator(currentPage: 1, totalPages: 5),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ),
    );
  }
}
