import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';

class ProfilScreen extends StatelessWidget {
  const ProfilScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Color(0xFFB8A5C8),
      appBar: AppBar(
        backgroundColor: Color(0xFFB8A5C8),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.menu, color: Colors.black),
          onPressed: () {},
        ),
      ),
      body: Column(
        children: [
          // Header mit Avatar und Name
          Container(
            width: double.infinity,
            color: Color(0xFFB8A5C8),
            padding: const EdgeInsets.only(bottom: 24),
            child: Column(
              children: [
                // Avatar - zeigt Icon wenn kein Bild vorhanden
                _buildAvatar(
                  'https://www.pferd-aktuell.de/ausbildung/ausbildung-des-reiters/ausbildung-des-reiters',
                ), // null = kein Bild, String URL = mit Bild
                const SizedBox(height: 16),
                // Name
                const Text(
                  'Max Mustermann',
                  style: TextStyle(
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                    color: Colors.black,
                  ),
                ),
              ],
            ),
          ),

          // Weißer Container mit Menü
          Expanded(
            child: Container(
              margin: const EdgeInsets.symmetric(horizontal: 16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.only(
                  topLeft: Radius.circular(30),
                  topRight: Radius.circular(30),
                ),
                border: Border.all(
                  color: Color(0xFF4A90E2),
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
                          builder: (context) => const PersonalDataScreen(),
                        ),
                      );
                    },
                  ),
                  Divider(height: 1, indent: 50, endIndent: 50, color: Colors.grey[300]),
                  _buildMenuTile(
                    context,
                    title: 'Meine Satteldruckmessgeräte',
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => const DevicesScreen(),
                        ),
                      );
                    },
                  ),
                  Divider(height: 1, indent: 50, endIndent: 50, color: Colors.grey[300]),
                  _buildMenuTile(
                    context,
                    title: 'Meine Pferde',
                    onTap: () {
                      // Navigation zum Ho
                      //
                      //rse Screen
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => const HorsesScreen(),
                        ),
                      );
                    },
                  ),
                  const Spacer(),
                  // Logout Button
                  Padding(
                    padding: const EdgeInsets.fromLTRB(24, 16, 24, 32),
                    child: Row(
                      children: [
                        Icon(Icons.logout, size: 20, color: Colors.red),
                        const SizedBox(width: 8),
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

  Widget _buildMenuTile(BuildContext context,
      {required String title, required VoidCallback onTap}) {
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

// Persönliche Daten Screen
class PersonalDataScreen extends StatelessWidget {
  const PersonalDataScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Color(0xFFB8A5C8),
      appBar: AppBar(
        backgroundColor: Color(0xFFB8A5C8),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit_outlined, color: Colors.black),
            onPressed: () {},
          ),
        ],
      ),
      body: Column(
        children: [
          // Header mit Avatar und Name
          Container(
            width: double.infinity,
            color: Color(0xFFB8A5C8),
            padding: const EdgeInsets.only(bottom: 24),
            child: Column(
              children: [
                Container(
                  width: 120,
                  height: 120,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.white, width: 5),
                  ),
                  child: ClipOval(
                    child: Image.network(
                      'https://www.pferd-aktuell.de/ausbildung/ausbildung-des-reiters/ausbildung-des-reiters',
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
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                const Text(
                  'Max Mustermann',
                  style: TextStyle(
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                    color: Colors.black,
                  ),
                ),
              ],
            ),
          ),

          // Weißer Container mit Daten
          Expanded(
            child: Container(
              margin: const EdgeInsets.symmetric(horizontal: 16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.only(
                  topLeft: Radius.circular(30),
                  topRight: Radius.circular(30),
                ),
                border: Border.all(
                  color: Color(0xFFE0E0E0),
                  width: 2,
                ),
              ),
              padding: const EdgeInsets.all(32),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildDataRow('Geburtsdatum:', '01.01.2000'),
                  Divider(height: 32, color: Colors.grey[300]),
                  _buildDataRow('Adresse:', 'Musterstraße 1\n                         1111 Musterdorf'),
                  Divider(height: 32, color: Colors.grey[300]),
                  _buildDataRow('Email:', 'maxmuster@muster.com'),
                  Divider(height: 32, color: Colors.grey[300]),
                  _buildDataRow('Größe:', '11cm'),
                  Divider(height: 32, color: Colors.grey[300]),
                  _buildDataRow('Gewicht:', '11kg'),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDataRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w700,
            fontSize: 15,
            color: Colors.black,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 15,
              color: Colors.black,
              fontWeight: FontWeight.w400,
            ),
          ),
        ),
      ],
    );
  }
}

// Geräte Screen mit Expand/Collapse
class DevicesScreen extends StatefulWidget {
  const DevicesScreen({super.key});

  @override
  State<DevicesScreen> createState() => _DevicesScreenState();
}

class _DevicesScreenState extends State<DevicesScreen> {
  int? expandedIndex;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'Meine Satteldruckmessgeräte',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w500,
            fontSize: 16,
          ),
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          _buildDeviceItem(
            index: 0,
            deviceName: 'Lenas Gerät',
            deviceNumber: 'EP0706',
          ),
          Divider(height: 1, color: Colors.grey[300]),
          _buildDeviceItem(
            index: 1,
            deviceName: 'Floras Gerät',
            deviceNumber: 'EP0707',
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {},
        backgroundColor: Color(0xFFC8A2D0),
        elevation: 2,
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }

  Widget _buildDeviceItem({
    required int index,
    required String deviceName,
    required String deviceNumber,
  }) {
    final isExpanded = expandedIndex == index;

    return Column(
      children: [
        InkWell(
          onTap: () {
            setState(() {
              expandedIndex = isExpanded ? null : index;
            });
          },
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            child: Row(
              children: [
                Expanded(
                  child: RichText(
                    text: TextSpan(
                      style: const TextStyle(fontSize: 15, color: Colors.black),
                      children: [
                        TextSpan(
                          text: 'Gerät: ',
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                        TextSpan(
                          text: deviceName,
                          style: const TextStyle(fontWeight: FontWeight.w400),
                        ),
                      ],
                    ),
                  ),
                ),
                Icon(
                  isExpanded ? Icons.keyboard_arrow_up : Icons.chevron_right,
                  color: Colors.black54,
                  size: 24,
                ),
              ],
            ),
          ),
        ),
        if (isExpanded)
          Container(
            color: Colors.grey[50],
            padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Gerätenummer
                _buildInfoRow('Gerätenummer:', deviceNumber),
                const SizedBox(height: 16),

                // Gerätegruppe
                InkWell(
                  onTap: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const DeviceGroupScreen(),
                      ),
                    );
                  },
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        const Text(
                          'Gerätegruppe',
                          style: TextStyle(
                            fontWeight: FontWeight.w600,
                            fontSize: 15,
                          ),
                        ),
                        const Spacer(),
                        Icon(Icons.chevron_right, color: Colors.black54, size: 20),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 24),

                // Gerät löschen Button
                SizedBox(
                  width: double.infinity,
                  child: OutlinedButton.icon(
                    onPressed: () {},
                    icon: const Icon(Icons.delete_outline, size: 18, color: Colors.red),
                    label: const Text(
                      'Gerät löschen',
                      style: TextStyle(
                        color: Colors.red,
                        fontSize: 15,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    style: OutlinedButton.styleFrom(
                      side: const BorderSide(color: Colors.red, width: 1.5),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w600,
            fontSize: 15,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 15,
              color: Colors.black87,
            ),
          ),
        ),
      ],
    );
  }
}

// Gerätegruppe Screen
class DeviceGroupScreen extends StatelessWidget {
  const DeviceGroupScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'Meine Gruppe',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w500,
            fontSize: 18,
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.info_outline, color: Colors.black),
            onPressed: () {},
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Mitglieder (5)',
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey[600],
                fontWeight: FontWeight.w500,
                letterSpacing: 0.5,
              ),
            ),
            const SizedBox(height: 20),
            _buildMemberTile('Du', null, isCurrentUser: true),
            const SizedBox(height: 16),
            _buildMemberTile('Flora Dellinger', null, canRemove: true),
            const SizedBox(height: 16),
            _buildMemberTile('Katharina Einzl', null, canRemove: true),
            const SizedBox(height: 16),
            _buildMemberTile('Lejla Music', null, canRemove: true),
            const SizedBox(height: 16),
            _buildMemberTile('Christoph Pfeiffer', null, canRemove: true),
            const Spacer(),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton(
                onPressed: () {},
                style: OutlinedButton.styleFrom(
                  foregroundColor: Colors.red,
                  side: const BorderSide(color: Colors.red, width: 1.5),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                child: const Text(
                  'Gerät übergeben',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMemberTile(String name, String? imageUrl,
      {bool isCurrentUser = false, bool canRemove = false}) {
    return Row(
      children: [
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: Colors.grey[200],
          ),
          child: imageUrl != null
              ? ClipOval(
                  child: Image.network(
                    imageUrl,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) {
                      return Icon(Icons.person, color: Colors.grey[400], size: 24);
                    },
                  ),
                )
              : Icon(Icons.person, color: Colors.grey[400], size: 24),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Text(
            name,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w400,
              color: Colors.black87,
            ),
          ),
        ),
        if (canRemove)
          IconButton(
            icon: Icon(Icons.delete_outline, size: 22, color: Colors.grey[600]),
            onPressed: () {},
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(),
          ),
      ],
    );
  }
}

