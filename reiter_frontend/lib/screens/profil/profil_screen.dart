import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/user_data.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/screens/onboarding/auth_gate.dart';
import 'package:reiterappfrontend/services/auth_service.dart';
import 'package:reiterappfrontend/widgets/sidenav.dart';
import 'package:reiterappfrontend/screens/profil/personal_data_screen.dart';
import 'package:reiterappfrontend/screens/profil/devices_screen.dart';
import 'package:reiterappfrontend/services/person_service.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';

class ProfilScreen extends StatefulWidget {
  const ProfilScreen({super.key});

  @override
  State<ProfilScreen> createState() => _ProfilScreenState();
}

class _ProfilScreenState extends State<ProfilScreen> {
  String? _personId;
  UserData? _userData;
  List<Device>? _devices;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    try {
      final personId = await BackendPersonContextService.loadPersonId();
      if (!mounted) return;
      setState(() {
        _personId = personId;
        _loading = personId != null;
      });
      if (personId != null) await _loadPerson(personId);
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = 'Personen-ID konnte nicht geladen werden: $error';
        _loading = false;
      });
    }
  }

  Future<void> _loadPerson(String personId) async {
    setState(() {
      _personId = personId;
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait<Object>([
        PersonService.loadUserData(personId),
        PersonService.loadDevices(personId),
      ]);
      if (!mounted) return;
      setState(() {
        _personId = personId;
        _userData = results[0] as UserData;
        _devices = results[1] as List<Device>;
        _loading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = 'Daten konnten nicht geladen werden: $error';
        _loading = false;
      });
    }
  }

  /// Meldet ab und startet wieder beim Login
  Future<void> _logout() async {
    await AuthService().logout();
    await BackendPersonContextService.clearPersonId();
    if (!mounted) return;
    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => const AuthGate()),
      (_) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(
        backgroundColor: Color(0xFFB8A5C8),
        body: Center(child: CircularProgressIndicator()),
      );
    }
    if (_error != null ||
        _personId == null ||
        _userData == null ||
        _devices == null) {
      return Scaffold(
        backgroundColor: const Color(0xFFB8A5C8),
        appBar: AppBar(
          backgroundColor: const Color(0xFFB8A5C8),
          actions: [
            if (_personId != null)
              IconButton(
                tooltip: 'Erneut laden',
                onPressed: () => _loadPerson(_personId!),
                icon: const Icon(Icons.refresh),
              ),
            IconButton(
              tooltip: 'Logout',
              onPressed: _logout,
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(_error ?? 'Die Personendaten sind unvollständig.'),
          ),
        ),
      );
    }

    final userData = _userData!;
    final devices = _devices!;
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
                _buildAvatar(userData.imageUrl),
                const SizedBox(height: 16),
                Text(
                  userData.name,
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
                          builder: (context) =>
                              PersonalDataScreen(userData: userData),
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
                          builder: (context) => DevicesScreen(
                            devices: devices,
                            ownerId: _personId!,
                          ),
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
                  InkWell(
                    onTap: _logout,
                    child: const Padding(
                      padding: EdgeInsets.fromLTRB(24, 16, 24, 32),
                      child: Row(
                        children: [
                          Icon(Icons.logout, size: 20, color: Colors.red),
                          SizedBox(width: 8),
                          Text(
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
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
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
        child: imageUrl != null && imageUrl.isNotEmpty
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
                child: const Icon(Icons.person, size: 60, color: Colors.grey),
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
