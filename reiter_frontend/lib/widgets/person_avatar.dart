import 'package:flutter/material.dart';
import 'package:reiterappfrontend/services/person_service.dart';
import 'package:reiterappfrontend/theme/app_colors.dart';

/// Profilbild einer Person, ohne Bild oder bei einem Fehler die Initialen.
class PersonAvatar extends StatelessWidget {
  final String personId;
  final String? imageUpdatedAt;
  final String name;
  final double radius;

  const PersonAvatar({
    super.key,
    required this.personId,
    required this.imageUpdatedAt,
    required this.name,
    this.radius = 28,
  });

  String get _initials {
    final parts = name.split(' ').where((part) => part.isNotEmpty).toList();
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return parts.isNotEmpty ? parts[0][0].toUpperCase() : '?';
  }

  @override
  Widget build(BuildContext context) {
    final url = PersonService.imageUrl(personId, imageUpdatedAt);
    return CircleAvatar(
      radius: radius,
      backgroundColor: AppColors.successLight,
      foregroundImage: url == null ? null : NetworkImage(url),
      onForegroundImageError: url == null ? null : (_, _) {},
      child: Text(
        _initials,
        style: TextStyle(
          color: AppColors.success,
          fontWeight: FontWeight.bold,
          fontSize: radius * 0.6,
        ),
      ),
    );
  }
}
