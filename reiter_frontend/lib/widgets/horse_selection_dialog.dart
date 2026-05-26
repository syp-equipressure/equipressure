import 'package:flutter/material.dart';
import '../models/horse.dart';

class HorseSelectionDialog extends StatefulWidget {
  final List<Horse> horses;
  final Function(List<Horse>) onHorsesSelected;

  const HorseSelectionDialog({
    super.key,
    required this.horses,
    required this.onHorsesSelected,
  });

  @override
  State<HorseSelectionDialog> createState() => _HorseSelectionDialogState();
}

class _HorseSelectionDialogState extends State<HorseSelectionDialog> {
  final Set<String> _selectedHorseIds = {};

  void _toggleHorse(String horseId) {
    setState(() {
      if (_selectedHorseIds.contains(horseId)) {
        _selectedHorseIds.remove(horseId);
      } else {
        _selectedHorseIds.add(horseId);
      }
    });
  }

  void _onSend() {
    final selectedHorses = widget.horses
        .where((h) => _selectedHorseIds.contains(h.id))
        .toList();
    widget.onHorsesSelected(selectedHorses);
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
            // Header
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Pferde auswählen',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            // Horse list
            Flexible(
              child: ListView.builder(
                shrinkWrap: true,
                itemCount: widget.horses.length,
                itemBuilder: (context, index) {
                  final horse = widget.horses[index];
                  final isSelected = _selectedHorseIds.contains(horse.id);

                  return _HorseSelectionItem(
                    horse: horse,
                    isSelected: isSelected,
                    onTap: () => _toggleHorse(horse.id),
                  );
                },
              ),
            ),
            // Send button
            if (_selectedHorseIds.isNotEmpty) ...[
              const Divider(height: 1),
              Padding(
                padding: const EdgeInsets.all(16),
                child: SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: _onSend,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF7C4DFF),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                    child: Text(
                      'Senden (${_selectedHorseIds.length})',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _HorseSelectionItem extends StatelessWidget {
  final Horse horse;
  final bool isSelected;
  final VoidCallback onTap;

  const _HorseSelectionItem({
    required this.horse,
    required this.isSelected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: isSelected ? Colors.blue[50] : Colors.transparent,
        ),
        child: Row(
          children: [
            // Checkbox
            Container(
              width: 24,
              height: 24,
              decoration: BoxDecoration(
                color: isSelected ? Colors.blue : Colors.transparent,
                border: Border.all(
                  color: isSelected ? Colors.blue : Colors.grey[400]!,
                  width: 2,
                ),
                borderRadius: BorderRadius.circular(4),
              ),
              child: isSelected
                  ? const Icon(
                      Icons.check,
                      size: 18,
                      color: Colors.white,
                    )
                  : null,
            ),
            const SizedBox(width: 16),
            // Horse info
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    horse.name,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${horse.age} | ${horse.breed}',
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
