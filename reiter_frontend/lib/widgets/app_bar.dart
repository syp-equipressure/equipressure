import 'package:reiterappfrontend/theme/app_colors.dart';
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
      backgroundColor: AppColors.surface,

      // 👇 DAS MACHT DEN SCHATTEN WIE IM BILD
      elevation: 3,
      shadowColor: AppColors.shadow(1.5),

      centerTitle: true,

      // ☰ MENU
      leading: IconButton(
        icon: const Icon(Icons.menu, color: AppColors.textStrong),
        onPressed: () => Scaffold.of(context).openDrawer(),
      ),

      // TITLE
      title: Text(
        title,
        style: const TextStyle(
          fontWeight: FontWeight.w600,
          color: AppColors.textStrong,
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
              backgroundColor: AppColors.purple100,
              child: Icon(
                Icons.person_outline,
                color: AppColors.purple400,
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
