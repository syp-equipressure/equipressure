import 'package:flutter/material.dart';

class SelectionDropdown<T> extends StatelessWidget {
  final String label;
  final T? value;
  final List<T> items;
  final String Function(T) getItemText;
  final String Function(T) getItemId; // Neue Funktion für eindeutige ID
  final ValueChanged<T?> onChanged;
  final bool enabled;
  final VoidCallback? onAddNew;
  final String? addNewText;

  const SelectionDropdown({
    super.key,
    required this.label,
    required this.value,
    required this.items,
    required this.getItemText,
    required this.getItemId,
    required this.onChanged,
    this.enabled = true,
    this.onAddNew,
    this.addNewText,
  });

  @override
  Widget build(BuildContext context) {
    // Verwende einen speziellen Marker-Wert für "Hinzufügen"
    const addNewMarker = '__ADD_NEW__';
    
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          decoration: BoxDecoration(
            border: Border.all(
              color: enabled ? Colors.grey[300]! : Colors.grey[200]!,
            ),
            borderRadius: BorderRadius.circular(12),
            color: enabled ? Colors.white : Colors.grey[50],
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: value != null ? getItemId(value as T) : null,
              isExpanded: true,
              icon: Icon(
                Icons.expand_more,
                color: enabled ? Colors.grey[600] : Colors.grey[400],
              ),
              hint: Text(
                'Bitte wählen...',
                style: TextStyle(color: Colors.grey[400]),
              ),
              items: [
                ...items.map((T item) {
                  return DropdownMenuItem<String>(
                    value: getItemId(item),
                    child: Text(getItemText(item)),
                  );
                }),
                if (onAddNew != null)
                  DropdownMenuItem<String>(
                    value: addNewMarker,
                    child: Row(
                      children: [
                        const Icon(
                          Icons.add_circle_outline,
                          size: 20,
                          color: Color(0xFF6B4C9A),
                        ),
                        const SizedBox(width: 8),
                        Text(
                          addNewText ?? '$label hinzufügen',
                          style: const TextStyle(
                            color: Color(0xFF6B4C9A),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
              onChanged: enabled
                  ? (String? newValue) {
                      if (newValue == addNewMarker && onAddNew != null) {
                        onAddNew!();
                      } else if (newValue != null) {
                        // Finde das entsprechende Item anhand der ID
                        final selectedItem = items.firstWhere(
                          (item) => getItemId(item) == newValue,
                        );
                        onChanged(selectedItem);
                      }
                    }
                  : null,
            ),
          ),
        ),
      ],
    );
  }
}