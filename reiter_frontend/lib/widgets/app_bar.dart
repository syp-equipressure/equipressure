import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import '../screens/profil/profil_screen.dart';
import '../services/person_service.dart';

class CustomAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;

  /// Ein Zurück-Pfeil, sobald es eine vorherige Seite gibt. Seiten mit eigenem Pfeil schalten ihn aus.
  final bool showBack;

  const CustomAppBar({
    super.key,
    required this.title,
    this.showBack = true,
  });

  @override
  Widget build(BuildContext context) {
    return AppBar(
      backgroundColor: AppColors.surface,

      centerTitle: true,
      toolbarHeight: 64,
      automaticallyImplyLeading: showBack,

      // TITLE
      // das ganze Logo mit Schriftzug, der Titel dient nur noch der Barrierefreiheit
      title: Semantics(
        label: title,
        child: Image.asset('assets/images/equipressure-logo.png', height: 48),
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
  Size get preferredSize => const Size.fromHeight(64);
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
