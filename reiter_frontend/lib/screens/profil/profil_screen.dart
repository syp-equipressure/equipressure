import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/widgets/sidenav.dart';
import 'package:reiterappfrontend/screens/profil/personal_data_screen.dart';
import 'package:reiterappfrontend/screens/profil/devices_screen.dart';
import 'package:reiterappfrontend/services/person_service.dart';

class ProfilScreen extends StatelessWidget {
  const ProfilScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: Future.wait([
        PersonService.loadUserData(),
        PersonService.loadDevices(),
      ]),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(
            backgroundColor: const Color(0xFFB8A5C8),
            body: const Center(child: CircularProgressIndicator()),
          );
        }

        if (snapshot.hasError) {
          return Scaffold(
            backgroundColor: const Color(0xFFB8A5C8),
            body: Center(child: Text('Fehler: ${snapshot.error}')),
          );
        }

        final userData = snapshot.data![0] as dynamic;
        final devices = snapshot.data![1] as dynamic;

        return Scaffold(
          backgroundColor: const Color(0xFFB8A5C8),
          drawer: const SideNav(),
          appBar: AppBar(
            backgroundColor: const Color(0xFFB8A5C8),
            elevation: 0,
            leading: Builder(
              builder: (context) {
                return IconButton(
                  icon: const Icon(Icons.menu, color: Colors.black),
                  onPressed: () {
                    Scaffold.of(context).openDrawer();
                  },
                );
              },
            ),
          ),
          body: Column(
            children: [
              Container(
                width: double.infinity,
                color: const Color(0xFFB8A5C8),
                padding: const EdgeInsets.only(bottom: 24),
                child: Column(
                  children: [
                    _buildAvatar(userData.imageUrl as String?),
                    const SizedBox(height: 16),
                    Text(
                      userData.name as String,
                      style: const TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.bold,
                        color: Colors.black,
                      ),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: Container(
                  margin: const EdgeInsets.symmetric(horizontal: 16),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: const BorderRadius.only(
                      topLeft: Radius.circular(30),
                      topRight: Radius.circular(30),
                    ),
                    border: Border.all(
                      color: const Color.fromARGB(255, 250, 210, 255),
                      width: 3,
                    ),
                  ),
                  child: Column(
                    children: [
                      const SizedBox(height: 8),
                      _buildMenuTile(
                        context,
                        title: 'Persönliche Daten',
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => PersonalDataScreen(userData: userData),
                            ),
                          );
                        },
                      ),
                      Divider(
                        height: 1,
                        indent: 50,
                        endIndent: 50,
                        color: Colors.grey[300],
                      ),
                      _buildMenuTile(
                        context,
                        title: 'Meine Satteldruckmessgeräte',
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => DevicesScreen(devices: devices),
                            ),
                          );
                        },
                      ),
                      Divider(
                        height: 1,
                        indent: 50,
                        endIndent: 50,
                        color: Colors.grey[300],
                      ),
                      _buildMenuTile(
                        context,
                        title: 'Meine Pferde',
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => const HorsesScreen(),
                            ),
                          );
                        },
                      ),
                      const Spacer(),
                      Padding(
                        padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
                        child: Row(
                          children: [
                            const Icon(Icons.logout, size: 20, color: Colors.red),
                            const SizedBox(width: 8),
                            const Text(
                              'Logout',
                              style: TextStyle(
                                color: Colors.red,
                                fontSize: 16,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildAvatar(String? imageUrl) {
    return Container(
      width: 120,
      height: 120,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: Colors.white, width: 5),
      ),
      child: ClipOval(
        child: imageUrl != null
            ? Image.network(
                imageUrl,
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) {
                  return Container(
                    color: Colors.white,
                    child: const Icon(
                      Icons.person,
                      size: 60,
                      color: Colors.grey,
                    ),
                  );
                },
              )
            : Container(
                color: Colors.white,
                child: const Icon(
                  Icons.person,
                  size: 60,
                  color: Colors.grey,
                ),
              ),
      ),
    );
  }

  Widget _buildMenuTile(
    BuildContext context, {
    required String title,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 50, vertical: 18),
        child: Row(
          children: [
            Expanded(
              child: Text(
                title,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: Colors.black,
                ),
              ),
            ),
            const Icon(Icons.arrow_forward, color: Colors.black, size: 20),
          ],
        ),
      ),
    );
  }
}
