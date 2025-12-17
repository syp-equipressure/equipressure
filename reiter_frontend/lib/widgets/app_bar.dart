import 'package:flutter/material.dart';
import '../screens/profil/profil_screen.dart';

class CustomAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;

  const CustomAppBar({
    super.key,
    required this.title,
  });

  @override
  Widget build(BuildContext context) {
    return AppBar(
      backgroundColor: Colors.white,

      // 👇 DAS MACHT DEN SCHATTEN WIE IM BILD
      elevation: 3,
      shadowColor: Colors.black.withValues(alpha: 1.5),

      centerTitle: true,

      // ☰ MENU
      leading: IconButton(
        icon: const Icon(Icons.menu, color: Colors.black),
        onPressed: () => Scaffold.of(context).openDrawer(),
      ),

      // TITLE
      title: Text(
        title,
        style: const TextStyle(
          fontWeight: FontWeight.w600,
          color: Colors.black,
          fontSize: 18,
        ),
      ),

      // 👤 PROFILE
      actions: [
        Padding(
          padding: const EdgeInsets.only(right: 12),
          child: GestureDetector(
            onTap: () {
              Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (_) => const ProfilScreen(),
                ),
              );
            },
            child: CircleAvatar(
              radius: 18,
              backgroundColor: Colors.deepPurple[100],
              child: Icon(
                Icons.person_outline,
                color: Colors.deepPurple[400],
              ),
            ),
          ),
        ),
      ],
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);
}
