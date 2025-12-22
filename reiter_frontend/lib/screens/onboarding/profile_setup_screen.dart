import 'package:flutter/material.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import '../../widgets/common/app_button.dart';
import '../../widgets/common/app_text_field.dart';
import 'widgets/page_indicator.dart';

class ProfileSetupScreen extends StatelessWidget {
  final VoidCallback onNext;

  const ProfileSetupScreen({super.key, required this.onNext});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.background,
      child: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 32),
              const Center(
                child: Text('Profil konfigurieren', style: AppTextStyles.h1),
              ),
              const SizedBox(height: 32),
              const AppTextField(label: 'Vorname'),
              const SizedBox(height: 16),
              const AppTextField(label: 'Nachname'),
              const SizedBox(height: 16),
              Text('Geburtsdatum', style: AppTextStyles.label),
              const SizedBox(height: 8),
              const Row(
                children: [
                  Expanded(child: AppTextFieldSmall(hint: 'DD')),
                  SizedBox(width: 12),
                  Expanded(child: AppTextFieldSmall(hint: 'MM')),
                  SizedBox(width: 12),
                  Expanded(child: AppTextFieldSmall(hint: 'YYYY')),
                ],
              ),
              const SizedBox(height: 16),
              const Row(
                children: [
                  Expanded(flex: 2, child: AppTextField(label: 'Ort')),
                  SizedBox(width: 16),
                  Expanded(flex: 1, child: AppTextField(label: 'PLZ')),
                ],
              ),
              const SizedBox(height: 16),
              const Row(
                children: [
                  Expanded(flex: 2, child: AppTextField(label: 'Straße')),
                  SizedBox(width: 16),
                  Expanded(flex: 1, child: AppTextField(label: 'Hausnr')),
                ],
              ),
              const SizedBox(height: 16),
              const Row(
                children: [
                  Expanded(
                    child: AppTextFieldWithSuffix(label: 'Größe', suffix: 'cm'),
                  ),
                  SizedBox(width: 16),
                  Expanded(
                    child: AppTextFieldWithSuffix(label: 'Gewicht', suffix: 'kg'),
                  ),
                ],
              ),
              const SizedBox(height: 32),
              AppButton(
                label: 'Speichern',
                icon: Icons.save_outlined,
                onPressed: onNext,
              ),
              const SizedBox(height: 32),
              const Center(
                child: PageIndicator(currentPage: 2, totalPages: 5),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}
