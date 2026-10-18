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
      onTap: onNext,
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
              const PageIndicator(currentPage: 0, totalPages: 5),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ),
    );
  }
}
