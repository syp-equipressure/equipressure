import 'package:flutter/material.dart';

class ProfilScreen extends StatelessWidget {
  const ProfilScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[50],
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 1,
        centerTitle: true,
        title: const Text(
          'Profil',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w600,
          ),
        ),
        iconTheme: const IconThemeData(color: Colors.black),
      ),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            const SizedBox(height: 20),

            // 👤 Avatar
            CircleAvatar(
              radius: 50,
              backgroundColor: Colors.deepPurple[100],
              child: Icon(
                Icons.person,
                size: 60,
                color: Colors.deepPurple[400],
              ),
            ),

            const SizedBox(height: 16),

            // Name
            const Text(
              'Max Mustermann',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),

            const SizedBox(height: 4),

            // Email
            Text(
              'max.mustermann@email.de',
              style: TextStyle(
                color: Colors.grey[600],
              ),
            ),

            const SizedBox(height: 40),

            // Optionen
            _profileTile(
              icon: Icons.edit,
              title: 'Profil bearbeiten',
              onTap: () {},
            ),
            _profileTile(
              icon: Icons.settings,
              title: 'Einstellungen',
              onTap: () {},
            ),
            _profileTile(
              icon: Icons.logout,
              title: 'Abmelden',
              onTap: () {},
              color: Colors.red,
            ),
          ],
        ),
      ),
    );
  }

  Widget _profileTile({
    required IconData icon,
    required String title,
    required VoidCallback onTap,
    Color? color,
  }) {
    return ListTile(
      leading: Icon(icon, color: color ?? Colors.black),
      title: Text(
        title,
        style: TextStyle(
          color: color ?? Colors.black,
          fontWeight: FontWeight.w500,
        ),
      ),
      trailing: const Icon(Icons.chevron_right),
      onTap: onTap,
    );
  }
}
