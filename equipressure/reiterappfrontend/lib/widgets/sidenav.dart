import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:reiterappfrontend/screens/find_saddler/find_saddler.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'package:reiterappfrontend/screens/saddler_contact/saddler_contact.dart';

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
                    MaterialPageRoute(builder: (context) => NewMeasurement())
                );
              }
            ),
            _DrawerItem(
              icon: FontAwesomeIcons.horseHead,
              text: 'Meine Pferde',
              onTap: () {
                Navigator.of(context).push(
                    MaterialPageRoute(builder: (context) => HorsesScreen())
                );
              },
            ),
            _DrawerItem(
              icon: Icons.chat_bubble_outline,
              text: 'Sattler*innen',
              onTap: () {
                Navigator.of(context).push(
                    MaterialPageRoute(builder: (context) => SaddlerContact())
                );
              },
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
  final IconData icon;
  final String text;
  final VoidCallback onTap;

  const _DrawerItem({
    required this.icon,
    required this.text,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon, size: 22),
      title: Text(
        text,
        style: const TextStyle(fontSize: 15),
      ),
      onTap: onTap,
    );
  }
}
