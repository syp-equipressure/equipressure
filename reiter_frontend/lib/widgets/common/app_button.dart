import 'package:flutter/material.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';

class AppButton extends StatelessWidget {
  final String label;
  final VoidCallback onPressed;
  final IconData? icon;
  final bool fullWidth;

  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.icon,
    this.fullWidth = true,
  });

  @override
  Widget build(BuildContext context) {
    final button = icon != null
        ? ElevatedButton.icon(
            onPressed: onPressed,
            icon: Icon(icon),
            label: Text(label, style: AppTextStyles.button),
          )
        : ElevatedButton(
            onPressed: onPressed,
            child: Text(label, style: AppTextStyles.button),
          );

    return fullWidth ? SizedBox(width: double.infinity, child: button) : button;
  }
}

class AppSocialButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final VoidCallback onPressed;

  const AppSocialButton({
    super.key,
    required this.label,
    required this.icon,
    required this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    // weiß mit Rand, wie die Anmelde-Buttons der Anbieter üblich aussehen
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton(
        onPressed: onPressed,
        style: OutlinedButton.styleFrom(
          backgroundColor: AppColors.surface,
          foregroundColor: AppColors.textStrong,
          padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 20),
        ),
        child: Row(
          children: [
            Icon(icon, size: 20),
            const SizedBox(width: 16),
            Flexible(
              child: Text(
                label,
                style: AppTextStyles.button,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class AppTextButton extends StatelessWidget {
  final String label;
  final VoidCallback onPressed;

  const AppTextButton({
    super.key,
    required this.label,
    required this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primaryLight,
          foregroundColor: AppColors.primary,
        ),
        child: Text(label, style: AppTextStyles.buttonLarge),
      ),
    );
  }
}
