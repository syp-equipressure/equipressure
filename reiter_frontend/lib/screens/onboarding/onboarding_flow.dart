import 'package:flutter/material.dart';
import '../../services/account_service.dart';
import '../horses/horses_screen.dart';
import 'welcome_screen.dart';
import 'login_screen.dart';
import 'profile_setup_screen.dart';
import 'qr_scanner_screen.dart';
import 'connection_success_screen.dart';

class OnboardingFlow extends StatefulWidget {
  static const int loginPage = 1;
  static const int profilePage = 2;
  static const int devicePage = 3;
  static const int successPage = 4;

  final int initialPage;

  const OnboardingFlow({super.key, this.initialPage = 0});

  @override
  State<OnboardingFlow> createState() => _OnboardingFlowState();
}

class _OnboardingFlowState extends State<OnboardingFlow> {
  late final PageController _pageController =
      PageController(initialPage: widget.initialPage);
  String? _claimedSerialNumber;

  void nextPage() {
    _pageController.nextPage(
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeInOut,
    );
  }

  void goToPage(int page) {
    _pageController.animateToPage(
      page,
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeInOut,
    );
  }

  void _onLoggedIn(AccountState state) {
    switch (state) {
      case AccountState.ready:
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(builder: (_) => const HorsesScreen()),
        );
      case AccountState.noProfile:
        goToPage(OnboardingFlow.profilePage);
      case AccountState.noDevice:
        goToPage(OnboardingFlow.devicePage);
    }
  }

  void _onDeviceClaimed(String serialNumber) {
    setState(() => _claimedSerialNumber = serialNumber);
    goToPage(OnboardingFlow.successPage);
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: PageView(
        controller: _pageController,
        // die Schritte dürfen nicht übersprungen werden
        physics: const NeverScrollableScrollPhysics(),
        children: [
          WelcomeScreen(onNext: nextPage),
          LoginScreen(onLoggedIn: _onLoggedIn),
          ProfileSetupScreen(onNext: nextPage),
          QRScannerScreen(onClaimed: _onDeviceClaimed),
          ConnectionSuccessScreen(serialNumber: _claimedSerialNumber ?? ''),
        ],
      ),
    );
  }
}
