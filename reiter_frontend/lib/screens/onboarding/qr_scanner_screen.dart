import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../services/account_service.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import '../../widgets/common/app_button.dart';
import 'widgets/page_indicator.dart';

/// QR-Code auf dem Satteldruckmessgerät scannen (oder Seriennummer eintippen) und das Gerät claimen
class QRScannerScreen extends StatefulWidget {
  final ValueChanged<String> onClaimed;

  const QRScannerScreen({super.key, required this.onClaimed});

  @override
  State<QRScannerScreen> createState() => _QRScannerScreenState();
}

class _QRScannerScreenState extends State<QRScannerScreen> {
  final MobileScannerController _scanner = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
  );
  bool _claiming = false;

  Future<void> _onDetect(BarcodeCapture capture) async {
    final raw = capture.barcodes.firstOrNull?.rawValue;
    if (raw == null || _claiming) return;
    await _claim(raw);
  }

  Future<void> _enterManually() async {
    final controller = TextEditingController();
    final serialNumber = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Seriennummer eingeben'),
        content: TextField(
          controller: controller,
          autofocus: true,
          decoration: const InputDecoration(hintText: 'z.B. EP0707'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(controller.text),
            child: const Text('Verbinden'),
          ),
        ],
      ),
    );
    controller.dispose();
    if (serialNumber != null) await _claim(serialNumber);
  }

  Future<void> _claim(String raw) async {
    final code = DeviceQrCode.tryParse(raw);
    if (code == null) {
      _showError('Das ist kein gültiger Code eines EquiPressure Satteldruckmessgeräts.');
      return;
    }

    setState(() => _claiming = true);
    try {
      await AccountService.claimDevice(code);
      await _scanner.stop();
      widget.onClaimed(code.serialNumber);
    } on AccountApiException catch (e) {
      _showError(e.isConflict
          ? 'Dieses Gerät ist bereits einem anderen Konto zugeordnet.'
          : 'Das Gerät konnte nicht verbunden werden.');
    } catch (_) {
      _showError('Das Gerät konnte nicht verbunden werden. Bitte versuche es noch einmal.');
    } finally {
      if (mounted) setState(() => _claiming = false);
    }
  }

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  void dispose() {
    _scanner.dispose();
    super.dispose();
  }

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
              Text(
                'Bitte scanne den QR-Code\nauf deinem EquiPressure\nSatteldruckmessgerät',
                textAlign: TextAlign.center,
                style: AppTextStyles.h3.copyWith(height: 1.4),
              ),
              const SizedBox(height: 32),
              ClipRRect(
                borderRadius: BorderRadius.circular(16),
                child: SizedBox(
                  width: 240,
                  height: 240,
                  child: _claiming
                      ? const ColoredBox(
                          color: AppColors.surface,
                          child: Center(child: CircularProgressIndicator()),
                        )
                      : MobileScanner(controller: _scanner, onDetect: _onDetect),
                ),
              ),
              const SizedBox(height: 24),
              AppTextButton(
                label: 'Seriennummer eingeben',
                onPressed: _claiming ? () {} : _enterManually,
              ),
              const Spacer(),
              const PageIndicator(currentPage: 3, totalPages: 5),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ),
    );
  }
}
