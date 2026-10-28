import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart'; // <--- NEU
import 'package:reiterappfrontend/screens/find_saddler/find_saddler.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'package:reiterappfrontend/screens/saddler_contact/saddler_contact.dart';
import 'package:reiterappfrontend/services/chat_service.dart';

class SideNav extends StatelessWidget {
  const SideNav({super.key});

  @override
  Widget build(BuildContext context) {
    return Drawer(
      backgroundColor: Colors.white,
      child: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Align(
                alignment: Alignment.centerLeft,
                child: IconButton(
                  icon: const Icon(Icons.menu),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ),
            ),

            // Menu Items
            _DrawerItem(
                icon: Icons.add,
                text: 'Neue Messung',
                onTap: () {
                  Navigator.of(context).push(
                      MaterialPageRoute(builder: (context) => NewMeasurementScreen())
                  );
                }
            ),
            _DrawerItem(
              svgPath: 'assets/icon/horseIcon.svg',
              text: 'Meine Pferde',
              onTap: () {
                Navigator.of(context).push(
                    MaterialPageRoute(builder: (context) => HorsesScreen())
                );
              },
            ),
            // zeigt die Anzahl der ungelesenen Chat-Nachrichten
            ListenableBuilder(
              listenable: ChatService(),
              builder: (context, _) => _DrawerItem(
                icon: Icons.chat_bubble_outline,
                text: 'Sattler*innen',
                badge: ChatService().unreadTotal,
                onTap: () {
                  Navigator.of(context).push(
                      MaterialPageRoute(builder: (context) => SaddlerContact())
                  );
                },
              ),
            ),
            _DrawerItem(
              icon: Icons.location_on_outlined,
              text: 'Sattler*in finden',
              onTap: () {
                Navigator.of(context).push(
                    MaterialPageRoute(builder: (context) => FindSaddler())
                );
              },
            ),

            const Spacer(),

            _DrawerItem(
              icon: Icons.settings,
              text: 'Einstellungen',
              onTap: () {},
            ),
          ],
        ),
      ),
    );
  }
}

class _DrawerItem extends StatelessWidget {
  final IconData? icon;
  final String? svgPath;

  final String text;
  final VoidCallback onTap;
  final int badge;

  const _DrawerItem({
    this.icon,
    this.svgPath,
    required this.text,
    required this.onTap,
    this.badge = 0,
  }) : assert(icon != null || svgPath != null);

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: _buildIcon(),
      title: Text(
        text,
        style: const TextStyle(fontSize: 15),
      ),
      trailing: badge > 0
          ? Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: const Color(0xFF6955AE),
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                '$badge',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
            )
          : null,
      onTap: onTap,
    );
  }

  Widget _buildIcon() {
    if (svgPath != null) {
      return SvgPicture.asset(
        svgPath!,
        width: 22,
        height: 22,
        colorFilter: const ColorFilter.mode(Colors.black, BlendMode.srcIn),
      );
    }
    return Icon(icon, size: 22);
  }
}
