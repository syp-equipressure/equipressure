import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/services/data_service.dart';

class DeviceGroupScreen extends StatefulWidget {
  final String deviceId;
  final String deviceName;

  const DeviceGroupScreen({
    super.key,
    required this.deviceId,
    required this.deviceName,
  });

  @override
  State<DeviceGroupScreen> createState() => _DeviceGroupScreenState();
}

class _DeviceGroupScreenState extends State<DeviceGroupScreen> {
  late Future<List<GroupMember>> _membersFuture;

  @override
  void initState() {
    super.initState();
    _membersFuture = DataService.loadGroupMembers(widget.deviceId);
  }

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
        title: Column(
          children: [
            const Text(
              'Meine Gruppe',
              style: TextStyle(
                color: Colors.black,
                fontWeight: FontWeight.w500,
                fontSize: 18,
              ),
            ),
            FutureBuilder<List<GroupMember>>(
              future: _membersFuture,
              builder: (context, snapshot) {
                if (snapshot.hasData) {
                  return Text(
                    '${snapshot.data!.length} Mitglieder*innen',
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.grey[500],
                      fontWeight: FontWeight.normal,
                    ),
                  );
                }
                return Text(
                  'Lädt...',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[500],
                  ),
                );
              },
            ),
          ],
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.info_outline, color: Colors.black),
            onPressed: () {},
          ),
        ],
      ),
      body: FutureBuilder<List<GroupMember>>(
        future: _membersFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Text('Fehler beim Laden der Daten: ${snapshot.error}'),
            );
          }

          final members = snapshot.data ?? [];

          return Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SizedBox(height: 20),
                Expanded(
                  child: ListView.builder(
                    itemCount: members.length,
                    itemBuilder: (context, index) {
                      final member = members[index];
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: _buildMemberTile(
                          member,
                          canRemove: !member.isCurrentUser,
                          onRemove: () {
                            _showDeleteMemberDialog(context, member, members, index);
                          },
                        ),
                      );
                    },
                  ),
                ),
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
          );
        },
      ),
    );
  }

  Widget _buildMemberTile(
    GroupMember member, {
    bool canRemove = false,
    VoidCallback? onRemove,
  }) {
    // Generate initials from member name
    final initials = member.name
        .split(' ')
        .map((word) => word.isNotEmpty ? word[0].toUpperCase() : '')
        .take(2)
        .join();

    return Row(
      children: [
        CircleAvatar(
          radius: 28,
          backgroundColor: _getColorForMember(member.id),
          child: Text(
            initials,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Text(
            member.name,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w500,
              color: Colors.black,
            ),
          ),
        ),
        if (member.isOwner)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              border: Border.all(color: Colors.grey[300]!, width: 1),
              borderRadius: BorderRadius.circular(16),
            ),
            child: Text(
              'Owner',
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey[600],
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        if (canRemove)
          IconButton(
            icon: Icon(Icons.delete_outline, color: Colors.grey[600]),
            onPressed: onRemove,
          ),
      ],
    );
  }

  Color _getColorForMember(String memberId) {
    final colors = [
      Colors.blue,
      Colors.green,
      Colors.orange,
      Colors.red,
      Colors.purple,
      Colors.teal,
      Colors.amber,
      Colors.pink,
    ];
    
    // Use hash of memberId to consistently pick a color
    final hash = memberId.hashCode.abs();
    return colors[hash % colors.length];
  }

  void _showDeleteMemberDialog(BuildContext context, GroupMember member, List<GroupMember> members, int index) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: const Text('Mitglied entfernen?'),
          content: Text(
            'Sind Sie sicher, dass Sie "${member.name}" aus der Gruppe entfernen möchten?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Abbrechen'),
            ),
            TextButton(
              onPressed: () {
                Navigator.pop(context);
                setState(() {
                  members.removeAt(index);
                  _membersFuture = Future.value(members);
                });
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text('${member.name} wurde aus der Gruppe entfernt'),
                    duration: const Duration(seconds: 2),
                  ),
                );
              },
              child: const Text(
                'Entfernen',
                style: TextStyle(color: Colors.red),
              ),
            ),
          ],
        );
      },
    );
  }
}
