import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/user_data.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
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

  Future<void> _configurePersonId() async {
    final controller = TextEditingController(text: _personId ?? '');
    final formKey = GlobalKey<FormState>();
    final personId = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Backend-Person auswählen'),
        content: Form(
          key: formKey,
          child: TextFormField(
            controller: controller,
            autofocus: true,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Personen-ID',
              helperText: 'Numerische ID aus dem Backend eingeben.',
            ),
            validator: (value) =>
                RegExp(r'^[1-9]\d*$').hasMatch(value?.trim() ?? '')
                ? null
                : 'Bitte eine positive numerische ID eingeben.',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Abbrechen'),
          ),
          FilledButton(
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(context, controller.text.trim());
              }
            },
            child: const Text('Verbinden'),
          ),
        ],
      ),
    );
    controller.dispose();
    if (personId == null || !mounted) return;

    try {
      final saved = await BackendPersonContextService.savePersonId(personId);
      if (!saved) throw const FormatException('Ungültige Personen-ID');
      await _loadPerson(personId);
    } catch (error) {
      if (!mounted) return;
      setState(
        () => _error = 'Personen-ID konnte nicht gespeichert werden: $error',
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(
        backgroundColor: Color(0xFFB8A5C8),
        body: Center(child: CircularProgressIndicator()),
      );
    }
    if (_personId == null) return _buildPersonIdPrompt();
    if (_error != null || _userData == null || _devices == null) {
      return Scaffold(
        backgroundColor: const Color(0xFFB8A5C8),
        appBar: AppBar(
          backgroundColor: const Color(0xFFB8A5C8),
          actions: [
            IconButton(
              tooltip: 'Personen-ID ändern',
              onPressed: _configurePersonId,
              icon: const Icon(Icons.edit_outlined),
            ),
            IconButton(
              tooltip: 'Erneut laden',
              onPressed: () => _loadPerson(_personId!),
              icon: const Icon(Icons.refresh),
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
        actions: [
          IconButton(
            tooltip: 'Personen-ID ändern',
            onPressed: _configurePersonId,
            icon: const Icon(Icons.edit_outlined, color: Colors.black),
          ),
        ],
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
  }

  Widget _buildPersonIdPrompt() {
    return Scaffold(
      backgroundColor: const Color(0xFFB8A5C8),
      appBar: AppBar(
        backgroundColor: const Color(0xFFB8A5C8),
        title: const Text('Profil mit Backend verbinden'),
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'Für Profildaten und Geräte fehlt noch die Anmeldung. '
                'Gib vorübergehend eine numerische Personen-ID aus dem Backend ein.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 16),
              if (_error != null) ...[
                Text(_error!, style: const TextStyle(color: Colors.red)),
                const SizedBox(height: 12),
              ],
              FilledButton(
                onPressed: _configurePersonId,
                child: const Text('Personen-ID eingeben'),
              ),
            ],
          ),
        ),
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
