import 'package:flutter/material.dart';
import '../../services/account_service.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import '../../widgets/common/app_button.dart';
import '../horses/horses_screen.dart';
import 'onboarding_flow.dart';

/// Startpunkt der App: eingeloggt mit fertigem Onboarding -> Pferde, sonst Onboarding an der richtigen Stelle
class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  late Future<AccountState?> _state = _load();

  /// null = nicht eingeloggt
  Future<AccountState?> _load() async {
    await AuthService().init();
    if (!AuthService().isLoggedIn) return null;
    return AccountService.loadState();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<AccountState?>(
      future: _state,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Scaffold(
            backgroundColor: AppColors.background,
            body: Center(child: CircularProgressIndicator()),
          );
        }

        if (snapshot.hasError) {
          return Scaffold(
            backgroundColor: AppColors.background,
            body: Padding(
              padding: const EdgeInsets.all(32),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Text(
                    'Der Server ist gerade nicht erreichbar.',
                    textAlign: TextAlign.center,
                    style: AppTextStyles.h3,
                  ),
                  const SizedBox(height: 24),
                  AppButton(
                    label: 'Erneut versuchen',
                    icon: Icons.refresh,
                    onPressed: () => setState(() => _state = _load()),
                  ),
                ],
              ),
            ),
          );
        }

        return switch (snapshot.data) {
          AccountState.ready => const HorsesScreen(),
          AccountState.noProfile =>
            const OnboardingFlow(initialPage: OnboardingFlow.profilePage),
          AccountState.noDevice =>
            const OnboardingFlow(initialPage: OnboardingFlow.devicePage),
          null => const OnboardingFlow(),
        };
      },
    );
  }
}
