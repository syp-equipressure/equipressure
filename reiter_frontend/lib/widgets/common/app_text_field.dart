import 'package:flutter/material.dart';
import '../../theme/app_text_styles.dart';

class AppTextField extends StatelessWidget {
  final String label;
  final String? hint;
  final TextEditingController? controller;
  final TextInputType? keyboardType;
  final bool centerText;

  const AppTextField({
    super.key,
    required this.label,
    this.hint,
    this.controller,
    this.keyboardType,
    this.centerText = false,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: AppTextStyles.label),
        const SizedBox(height: 8),
        TextField(
          controller: controller,
          keyboardType: keyboardType,
          textAlign: centerText ? TextAlign.center : TextAlign.start,
          decoration: InputDecoration(hintText: hint),
        ),
      ],
    );
  }
}

class AppTextFieldSmall extends StatelessWidget {
  final String hint;
  final TextEditingController? controller;
  final TextInputType? keyboardType;

  const AppTextFieldSmall({
    super.key,
    required this.hint,
    this.controller,
    this.keyboardType,
  });

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      textAlign: TextAlign.center,
      decoration: InputDecoration(hintText: hint),
    );
  }
}

class AppTextFieldWithSuffix extends StatelessWidget {
  final String label;
  final String suffix;
  final TextEditingController? controller;

  const AppTextFieldWithSuffix({
    super.key,
    required this.label,
    required this.suffix,
    this.controller,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: AppTextStyles.label),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: controller,
                keyboardType: TextInputType.number,
              ),
            ),
            const SizedBox(width: 8),
            Text(suffix, style: AppTextStyles.body),
          ],
        ),
      ],
    );
  }
}
