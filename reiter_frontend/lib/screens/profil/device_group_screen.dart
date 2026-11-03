import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/group_member.dart';
import 'package:reiterappfrontend/services/device_service.dart';

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
  bool _isUpdating = false;

  @override
  void initState() {
    super.initState();
    _membersFuture = DeviceService.getGroupMembers(widget.deviceId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      appBar: AppBar(
        backgroundColor: AppColors.surface,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textStrong),
          onPressed: () => Navigator.pop(context),
        ),
        title: Column(
          children: [
            const Text(
              'Meine Gruppe',
              style: TextStyle(
                color: AppColors.textStrong,
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
                      color: AppColors.textMuted,
                      fontWeight: FontWeight.normal,
                    ),
                  );
                }
                return Text(
                  'Lädt...',
                  style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                );
              },
            ),
          ],
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.info_outline, color: AppColors.textStrong),
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
                          canRemove: !member.isCurrentUser && !member.isOwner,
                          onRemove: () {
                            _showDeleteMemberDialog(context, member);
                          },
                        ),
                      );
                    },
                  ),
                ),
                SizedBox(
                  width: double.infinity,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      OutlinedButton(
                        onPressed: _isUpdating ? null : _showAddMemberDialog,
                        style: OutlinedButton.styleFrom(
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                        child: const Text('Mitglied hinzufügen'),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton(
                        onPressed: _isUpdating
                            ? null
                            : () => _showTransferOwnerDialog(members),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: AppColors.error,
                          side: const BorderSide(color: AppColors.error, width: 1.5),
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
                    ],
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
              color: AppColors.surface,
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
              color: AppColors.textStrong,
            ),
          ),
        ),
        if (member.isOwner)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              border: Border.all(color: AppColors.border, width: 1),
              borderRadius: BorderRadius.circular(16),
            ),
            child: Text(
              'Owner',
              style: TextStyle(
                fontSize: 12,
                color: AppColors.textSoft,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        if (canRemove)
          IconButton(
            icon: Icon(Icons.delete_outline, color: AppColors.textSoft),
            onPressed: onRemove,
          ),
      ],
    );
  }

  Color _getColorForMember(String memberId) {
    final colors = [
      AppColors.primary,
      AppColors.success,
      AppColors.warning,
      AppColors.error,
      AppColors.primary,
      AppColors.purple300,
      AppColors.warning,
      AppColors.purple400,
    ];

    // Use hash of memberId to consistently pick a color
    final hash = memberId.hashCode.abs();
    return colors[hash % colors.length];
  }

  void _showDeleteMemberDialog(BuildContext context, GroupMember member) {
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
              onPressed: _isUpdating
                  ? null
                  : () {
                      Navigator.pop(context);
                      _removeMember(member);
                    },
              child: const Text(
                'Entfernen',
                style: TextStyle(color: AppColors.error),
              ),
            ),
          ],
        );
      },
    );
  }

  Future<void> _showAddMemberDialog() async {
    final formKey = GlobalKey<FormState>();
    final userIdController = TextEditingController();
    final userId = await showDialog<int>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Mitglied hinzufügen'),
        content: Form(
          key: formKey,
          child: TextFormField(
            controller: userIdController,
            autofocus: true,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Personen-ID',
              hintText: 'Numerische ID eingeben',
              helperText:
                  'Das Backend stellt keine Personenliste zum Auswählen bereit.',
            ),
            validator: (value) {
              final parsed = int.tryParse(value?.trim() ?? '');
              return parsed == null || parsed < 0
                  ? 'Bitte eine gültige Personen-ID eingeben.'
                  : null;
            },
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
                Navigator.pop(context, int.parse(userIdController.text.trim()));
              }
            },
            child: const Text('Hinzufügen'),
          ),
        ],
      ),
    );
    userIdController.dispose();
    if (userId == null || !mounted) return;

    setState(() => _isUpdating = true);
    try {
      await DeviceService.addUserToDevice(widget.deviceId, userId);
      if (!mounted) return;
      setState(() {
        _membersFuture = DeviceService.getGroupMembers(widget.deviceId);
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mitglied wurde hinzugefügt.')),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Mitglied konnte nicht hinzugefügt werden: $error'),
        ),
      );
    } finally {
      if (mounted) setState(() => _isUpdating = false);
    }
  }

  Future<void> _removeMember(GroupMember member) async {
    setState(() => _isUpdating = true);
    try {
      await DeviceService.removeUserFromDevice(widget.deviceId, int.parse(member.id));
      if (!mounted) return;
      setState(() {
        _membersFuture = DeviceService.getGroupMembers(widget.deviceId);
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('${member.name} wurde aus der Gruppe entfernt'),
          duration: const Duration(seconds: 2),
        ),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Mitglied konnte nicht entfernt werden: $error'),
        ),
      );
    } finally {
      if (mounted) setState(() => _isUpdating = false);
    }
  }

  Future<void> _showTransferOwnerDialog(List<GroupMember> members) async {
    final eligibleMembers = members.where((member) => !member.isOwner).toList();
    if (eligibleMembers.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Es gibt keine Person, an die das Gerät übergeben werden kann.',
          ),
        ),
      );
      return;
    }

    final selectedMember = await showDialog<GroupMember>(
      context: context,
      builder: (context) => SimpleDialog(
        title: const Text('Neue Besitzerin oder neuen Besitzer auswählen'),
        children: eligibleMembers
            .map(
              (member) => SimpleDialogOption(
                onPressed: () => Navigator.pop(context, member),
                child: Text(member.name),
              ),
            )
            .toList(),
      ),
    );
    if (selectedMember == null || !mounted) return;

    setState(() => _isUpdating = true);
    try {
      await DeviceService.changeDeviceOwner(widget.deviceId, int.parse(selectedMember.id));
      if (!mounted) return;
      setState(() {
        _membersFuture = DeviceService.getGroupMembers(widget.deviceId);
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            '${selectedMember.name} ist jetzt Besitzer*in des Geräts.',
          ),
        ),
      );
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Besitz konnte nicht übertragen werden: $error'),
        ),
      );
    } finally {
      if (mounted) setState(() => _isUpdating = false);
    }
  }
}
