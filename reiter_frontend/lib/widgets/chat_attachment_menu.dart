import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

class ChatAttachmentMenu extends StatelessWidget {
  final VoidCallback onFilesSelected;
  final VoidCallback onImagesSelected;
  final VoidCallback onProfileSelected;
  final VoidCallback onHorsesSelected;

  const ChatAttachmentMenu({
    super.key,
    required this.onFilesSelected,
    required this.onImagesSelected,
    required this.onProfileSelected,
    required this.onHorsesSelected,
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
            icon: Icons.insert_drive_file_outlined,
            label: 'Dateien',
            onTap: onFilesSelected,
          ),
          _buildDivider(),
          _AttachmentMenuItem(
            icon: Icons.image_outlined,
            label: 'Bilder',
            onTap: onImagesSelected,
          ),
          _buildDivider(),
          _AttachmentMenuItem(
            icon: Icons.person_outline,
            label: 'Mein Profil',
            onTap: onProfileSelected,
          ),
          _buildDivider(),
          _AttachmentMenuItem(
            icon: FontAwesomeIcons.horseHead,
            label: 'Pferde',
            onTap: onHorsesSelected,
          ),
        ],
      ),
    );
  }

  Widget _buildDivider() {
    return Divider(
      height: 1,
      color: Colors.grey[200],
    );
  }
}

class _AttachmentMenuItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _AttachmentMenuItem({
    required this.icon,
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
