import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';

/// Auswahl, was im Chat geteilt wird: ganze Pferde mit allen Messungen oder
/// eine einzelne Messung.
class ChatAttachmentMenu extends StatelessWidget {
  final VoidCallback onHorsesSelected;
  final VoidCallback onMeasurementSelected;

  const ChatAttachmentMenu({
    super.key,
    required this.onHorsesSelected,
    required this.onMeasurementSelected,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.1),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          _AttachmentMenuItem(
            svgPath: 'assets/icon/horseIcon.svg',
            label: 'Pferde mit allen Messungen',
            onTap: onHorsesSelected,
          ),
          Divider(height: 1, color: Colors.grey[200]),
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
                colorFilter: ColorFilter.mode(Colors.grey[700]!, BlendMode.srcIn),
              )
            else
              Icon(
                icon,
                size: 22,
                color: Colors.grey[700],
              ),
            const SizedBox(width: 16),
            Text(
              label,
              style: TextStyle(
                fontSize: 15,
                color: Colors.grey[800],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
