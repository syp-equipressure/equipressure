import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import '../../widgets/common/app_button.dart';
import 'widgets/page_indicator.dart';

class LoginScreen extends StatelessWidget {
  final VoidCallback onNext;

  const LoginScreen({super.key, required this.onNext});

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
              const SizedBox(height: 48),
              AppSocialButton(
                icon: FontAwesomeIcons.google,
                label: 'Mit Google anmelden',
                onPressed: onNext,
              ),
              const SizedBox(height: 16),
              AppSocialButton(
                icon: FontAwesomeIcons.apple,
                label: 'Mit Apple anmelden',
                onPressed: onNext,
              ),
              const SizedBox(height: 16),
              AppSocialButton(
                icon: FontAwesomeIcons.facebook,
                label: 'Mit Facebook anmelden',
                onPressed: onNext,
              ),
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
