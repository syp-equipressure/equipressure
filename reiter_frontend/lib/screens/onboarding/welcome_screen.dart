import 'package:flutter/material.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import 'widgets/page_indicator.dart';
import 'widgets/horse_logo.dart';

class WelcomeScreen extends StatelessWidget {
  final VoidCallback onNext;

  const WelcomeScreen({super.key, required this.onNext});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      // nach links wischen führt zur Anmeldung
      onHorizontalDragEnd: (details) {
        if ((details.primaryVelocity ?? 0) < 0) onNext();
      },
      child: Container(
        color: AppColors.background,
        child: SafeArea(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Spacer(),
              const Text('Willkommen bei', style: AppTextStyles.h1),
              const SizedBox(height: 24),
              const HorseLogo(size: 120),
              const SizedBox(height: 16),
              const Text('EquiPressure', style: AppTextStyles.h2),
              const Spacer(),
              const Text('Wische nach links, um zu starten', style: AppTextStyles.labelSmall),
              const SizedBox(height: 16),
              const PageIndicator(currentPage: 0, totalPages: 5),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ),
    );
  }
}
