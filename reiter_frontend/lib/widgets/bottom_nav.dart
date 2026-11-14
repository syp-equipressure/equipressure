import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:reiterappfrontend/screens/find_saddler/find_saddler.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'package:reiterappfrontend/screens/saddler_contact/saddler_contact.dart';
import 'package:reiterappfrontend/services/chat_service.dart';

/// Die Hauptseiten der App, zwischen denen die Leiste unten wechselt
enum AppTab { measurement, horses, chat, map }

/// Die Leiste unten auf den vier Hauptseiten, sie ist die einzige Navigation zwischen ihnen
class AppBottomNav extends StatelessWidget {
  final AppTab current;

  const AppBottomNav({super.key, required this.current});

  void _open(BuildContext context, AppTab item) {
    if (item == current) return;
    final Widget screen = switch (item) {
      AppTab.horses => HorsesScreen(),
      AppTab.measurement => NewMeasurementScreen(),
      AppTab.chat => SaddlerContact(),
      AppTab.map => FindSaddler(),
    };
    // die Seite wird getauscht statt gestapelt, ohne Animation wie bei Tabs
    Navigator.of(context).pushReplacement(
      PageRouteBuilder(
        pageBuilder: (_, _, _) => screen,
        transitionDuration: Duration.zero,
        reverseTransitionDuration: Duration.zero,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(top: BorderSide(color: AppColors.borderSoft)),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      child: Row(
        children: [
          _item(context, AppTab.horses, 'Pferde', svgPath: 'assets/icon/horseIcon.svg'),
          _item(context, AppTab.measurement, 'Messung', icon: Icons.add_circle_outline),
          // zeigt die Anzahl der ungelesenen Chat-Nachrichten
          Expanded(
            child: ListenableBuilder(
              listenable: ChatService(),
              builder: (context, _) => _BottomNavItem(
                icon: Icons.chat_bubble_outline,
                label: 'Chat',
                selected: current == AppTab.chat,
                badge: ChatService().unreadTotal,
                onTap: () => _open(context, AppTab.chat),
              ),
            ),
          ),
          _item(context, AppTab.map, 'Map', icon: Icons.location_on_outlined),
        ],
      ),
    );
  }

  Widget _item(
    BuildContext context,
    AppTab item,
    String label, {
    IconData? icon,
    String? svgPath,
  }) {
    return Expanded(
      child: _BottomNavItem(
        icon: icon,
        svgPath: svgPath,
        label: label,
        selected: current == item,
        onTap: () => _open(context, item),
      ),
    );
  }
}

class _BottomNavItem extends StatelessWidget {
  final IconData? icon;
  final String? svgPath;
  final String label;
  final bool selected;
  final int badge;
  final VoidCallback onTap;

  const _BottomNavItem({
    this.icon,
    this.svgPath,
    required this.label,
    required this.selected,
    required this.onTap,
    this.badge = 0,
  }) : assert(icon != null || svgPath != null);

  @override
  Widget build(BuildContext context) {
    final color = selected ? AppColors.primary : AppColors.textMuted;

    return InkWell(
      borderRadius: BorderRadius.circular(12),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Badge(
              isLabelVisible: badge > 0,
              label: Text('$badge'),
              backgroundColor: AppColors.primary,
              child: svgPath != null
                  ? SvgPicture.asset(
                      svgPath!,
                      width: 24,
                      height: 24,
                      colorFilter: ColorFilter.mode(color, BlendMode.srcIn),
                    )
                  : Icon(icon, size: 24, color: color),
            ),
            const SizedBox(height: 3),
            Text(
              label,
              style: TextStyle(
                fontSize: 11,
                color: color,
                fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
