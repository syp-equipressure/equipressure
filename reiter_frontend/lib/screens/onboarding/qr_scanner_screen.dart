import 'package:flutter/material.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import 'widgets/page_indicator.dart';
import 'widgets/qr_scanner_frame.dart';

class QRScannerScreen extends StatelessWidget {
  final VoidCallback onNext;

  const QRScannerScreen({super.key, required this.onNext});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onNext,
      child: Container(
        color: AppColors.background,
        child: SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Spacer(),
                Text(
                  'Bitte scannen sie den\nQR-Code auf ihrem\nEquiPressure -\nSatteldruckmessgerät',
                  textAlign: TextAlign.center,
                  style: AppTextStyles.h3.copyWith(height: 1.4),
                ),
                const SizedBox(height: 48),
                const QRScannerFrame(),
                const Spacer(),
                const PageIndicator(currentPage: 3, totalPages: 5),
                const SizedBox(height: 40),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
