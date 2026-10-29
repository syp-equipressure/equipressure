import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/services/device_connection_service.dart';

/// Zeigt den Verbindungsstatus eines Geräts und einen Knopf zum Verbinden/Trennen.
class DeviceConnectionButton extends StatelessWidget {
  final Device device;

  const DeviceConnectionButton({super.key, required this.device});

  static const Color _accent = AppColors.primary;
  static const Color _accentLight = AppColors.purple100;

  @override
  Widget build(BuildContext context) {
    final connection = DeviceConnectionService();

    return ListenableBuilder(
      listenable: connection,
      builder: (context, _) {
        final connected = connection.isConnectedTo(device);
        final connecting = connection.isConnectingTo(device);

        return Row(
          children: [
            Icon(
              Icons.circle,
              size: 10,
              color: connected
                  ? AppColors.success
                  : connecting
                      ? AppColors.warning
                      : AppColors.textMuted,
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                connected
                    ? 'Verbunden'
                    : connecting
                        ? 'Verbinde …'
                        : 'Nicht verbunden',
                style: TextStyle(fontSize: 14, color: AppColors.textStrong),
              ),
            ),
            if (connected)
              OutlinedButton(
                onPressed: connection.disconnect,
                style: OutlinedButton.styleFrom(
                  foregroundColor: _accent,
                  side: const BorderSide(color: _accent),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                child: const Text('Trennen'),
              )
            else
              ElevatedButton(
                onPressed: connecting ? null : () => connection.connect(device),
                style: ElevatedButton.styleFrom(
                  backgroundColor: _accentLight,
                  foregroundColor: _accent,
                  elevation: 0,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                child: connecting
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2, color: _accent),
                      )
                    : const Text('Verbinden'),
              ),
          ],
        );
      },
    );
  }
}
