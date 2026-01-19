import 'package:flutter/material.dart';
import '../../models/chat.dart';
import '../../models/saddler.dart';
import '../../services/saddler_service.dart';

class AddSaddlerDialog extends StatefulWidget {
  final Function(Chat) onSaddlerAdded;

  const AddSaddlerDialog({
    super.key,
    required this.onSaddlerAdded,
  });

  @override
  State<AddSaddlerDialog> createState() => _AddSaddlerDialogState();
}

class _AddSaddlerDialogState extends State<AddSaddlerDialog> {
  final SaddlerService _saddlerService = SaddlerService();
  List<Saddler> _availableSaddlers = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadSaddlers();
  }

  Future<void> _loadSaddlers() async {
    final allSaddlers = await _saddlerService.getSaddlers();
    setState(() {
      _availableSaddlers = allSaddlers;
      _isLoading = false;
    });
  }

  void _selectSaddler(Saddler saddler) {
    final chat = Chat(
      id: 'chat_${DateTime.now().millisecondsSinceEpoch}',
      saddler: saddler,
      messages: [],
      lastMessageTime: DateTime.now(),
      lastMessagePreview: 'Neuer Chat',
    );

    widget.onSaddlerAdded(chat);
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      child: Container(
        constraints: const BoxConstraints(maxWidth: 400, maxHeight: 500),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Sattler*in auswählen',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            _isLoading
                ? const Padding(
                    padding: EdgeInsets.all(32),
                    child: CircularProgressIndicator(),
                  )
                : _availableSaddlers.isEmpty
                    ? const Padding(
                        padding: EdgeInsets.all(32),
                        child: Text(
                          'Keine Sattler*innen verfügbar',
                          style: TextStyle(color: Colors.grey),
                        ),
                      )
                    : Flexible(
                        child: ListView.builder(
                          shrinkWrap: true,
                          itemCount: _availableSaddlers.length,
                          itemBuilder: (context, index) {
                            final saddler = _availableSaddlers[index];
                            return _SaddlerListItem(
                              saddler: saddler,
                              onTap: () => _selectSaddler(saddler),
                            );
                          },
                        ),
                      ),
          ],
        ),
      ),
    );
  }
}

class _SaddlerListItem extends StatelessWidget {
  final Saddler saddler;
  final VoidCallback onTap;

  const _SaddlerListItem({
    required this.saddler,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            CircleAvatar(
              radius: 24,
              backgroundColor: Colors.green[50],
              child: Text(
                _getInitials(saddler.name),
                style: TextStyle(
                  color: Colors.green[700],
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Text(
              saddler.name,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _getInitials(String name) {
    final parts = name.split(' ');
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }
}
