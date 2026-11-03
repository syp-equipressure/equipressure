import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';

/// Auswahl, was im Chat geteilt wird: Bilder, Dateien, ganze Pferde mit allen
/// Messungen oder eine einzelne Messung.
class ChatAttachmentMenu extends StatelessWidget {
  final VoidCallback onImagesSelected;
  final VoidCallback onFilesSelected;
  final VoidCallback onHorsesSelected;
  final VoidCallback onMeasurementSelected;

  const ChatAttachmentMenu({
    super.key,
    required this.onImagesSelected,
    required this.onFilesSelected,
    required this.onHorsesSelected,
    required this.onMeasurementSelected,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: AppColors.shadow(0.1),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          _AttachmentMenuItem(
            icon: Icons.image_outlined,
            label: 'Bilder',
            onTap: onImagesSelected,
          ),
          Divider(height: 1, color: AppColors.borderSoft),
          _AttachmentMenuItem(
            icon: Icons.insert_drive_file_outlined,
            label: 'Dateien',
            onTap: onFilesSelected,
          ),
          Divider(height: 1, color: AppColors.borderSoft),
          _AttachmentMenuItem(
            svgPath: 'assets/icon/horseIcon.svg',
            label: 'Pferde mit allen Messungen',
            onTap: onHorsesSelected,
          ),
          Divider(height: 1, color: AppColors.borderSoft),
          _AttachmentMenuItem(
            icon: Icons.analytics_outlined,
            label: 'Einzelne Messung',
            onTap: onMeasurementSelected,
          ),
        ],
      ),
    );
  }
}

class _AttachmentMenuItem extends StatelessWidget {
  final IconData? icon;
  final String? svgPath;
  final String label;
  final VoidCallback onTap;

  const _AttachmentMenuItem({
    this.icon,
    this.svgPath,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Row(
          children: [
            if (svgPath != null)
              SvgPicture.asset(
                svgPath!,
                width: 22,
                height: 22,
                colorFilter: ColorFilter.mode(AppColors.text, BlendMode.srcIn),
              )
            else
              Icon(
                icon,
                size: 22,
                color: AppColors.text,
              ),
            const SizedBox(width: 16),
            Text(
              label,
              style: TextStyle(
                fontSize: 15,
                color: AppColors.textStrong,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
