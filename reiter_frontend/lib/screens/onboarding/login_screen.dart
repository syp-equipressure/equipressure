import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '../../services/account_service.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
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
    // gleicher Aufbau wie die Keycloak-Anmeldeseite (backend/keycloak/themes/equipressure):
    // Logo auf grauem Grund, darunter eine weiße Karte mit den Anbieter-Buttons
    return Container(
      color: AppColors.background,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            children: [
              const Spacer(),
              // das Logo hat einen weißen Hintergrund, multipliziert verschwindet er auf dem Grau
              Image.asset(
                'assets/images/equipressure-logo.png',
                height: 64,
                color: AppColors.background,
                colorBlendMode: BlendMode.multiply,
                semanticLabel: 'EquiPressure',
              ),
              const SizedBox(height: 28),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.borderSoft),
                  boxShadow: [
                    BoxShadow(color: AppColors.shadow(0.05), blurRadius: 2, offset: const Offset(0, 1)),
                    BoxShadow(color: AppColors.shadow(0.07), blurRadius: 14, offset: const Offset(0, 4)),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Anmelden',
                      style: TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textStrong,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Melde dich mit einem bestehenden Konto an, eine Registrierung ist nicht nötig.',
                      style: TextStyle(fontSize: 14, height: 1.4, color: AppColors.textSoft),
                    ),
                    const SizedBox(height: 24),
                    if (_loading)
                      const Center(
                        child: Padding(
                          padding: EdgeInsets.all(24),
                          child: CircularProgressIndicator(),
                        ),
                      )
                    else ...[
                      AppSocialButton(
                        icon: FontAwesomeIcons.google,
                        label: 'Mit Google anmelden',
                        onPressed: () => _login('google'),
                      ),
                      const SizedBox(height: 12),
                      AppSocialButton(
                        icon: FontAwesomeIcons.microsoft,
                        label: 'Mit Microsoft anmelden',
                        onPressed: () => _login('microsoft'),
                      ),
                      const SizedBox(height: 12),
                      AppSocialButton(
                        icon: FontAwesomeIcons.github,
                        label: 'Mit GitHub anmelden',
                        onPressed: () => _login('github'),
                      ),
                      // nur beim Entwickeln: Anmelden mit den Testkonten aus backend/keycloak
                      // (z.B. lena.grassauer / lena), dafür KC_LOGIN_THEME=equipressure-dev in der .env
                      if (kDebugMode) ...[
                        const SizedBox(height: 20),
                        const Row(
                          children: [
                            Expanded(child: Divider()),
                            Padding(
                              padding: EdgeInsets.symmetric(horizontal: 12),
                              child: Text(
                                'Entwicklung',
                                style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                              ),
                            ),
                            Expanded(child: Divider()),
                          ],
                        ),
                        const SizedBox(height: 8),
                        SizedBox(
                          width: double.infinity,
                          child: TextButton.icon(
                            onPressed: () => _login(null),
                            icon: const Icon(Icons.person_outline, size: 20),
                            label: const Text('Mit Testkonto anmelden'),
                          ),
                        ),
                      ],
                    ],
                  ],
                ),
              ),
              const Spacer(),
              const PageIndicator(currentPage: 1, totalPages: 5),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }
}
