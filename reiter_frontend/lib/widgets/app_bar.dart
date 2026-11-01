import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import '../screens/profil/profil_screen.dart';
import '../services/person_service.dart';

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
            child: const _OwnAvatar(),
          ),
        ),
      ],
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);
}

/// Das eigene Profilbild, ohne Bild oder bei einem Fehler das Personen-Symbol.
class _OwnAvatar extends StatefulWidget {
  const _OwnAvatar();

  @override
  State<_OwnAvatar> createState() => _OwnAvatarState();
}

class _OwnAvatarState extends State<_OwnAvatar> {
  @override
  void initState() {
    super.initState();
    PersonService.loadOwnImage();
  }

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<String?>(
      valueListenable: PersonService.ownImageUpdatedAt,
      builder: (context, imageUpdatedAt, _) {
        final personId = PersonService.ownPersonId;
        final url = personId == null ? null : PersonService.imageUrl(personId, imageUpdatedAt);
        return CircleAvatar(
          radius: 18,
          backgroundColor: AppColors.purple100,
          foregroundImage: url == null ? null : NetworkImage(url),
          onForegroundImageError: url == null ? null : (_, _) {},
          child: Icon(
            Icons.person_outline,
            color: AppColors.purple400,
          ),
        );
      },
    );
  }
}
