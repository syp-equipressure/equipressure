import 'package:reiterappfrontend/theme/app_colors.dart';
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
  /// Zeigt in der aufgeklappten Liste neben jedem Eintrag einen Stift zum Bearbeiten
  final ValueChanged<T>? onEditItem;

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
    this.onEditItem,
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
              color: enabled ? AppColors.border : AppColors.borderSoft,
            ),
            borderRadius: BorderRadius.circular(12),
            color: enabled ? AppColors.surface : AppColors.bgPanel,
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: value != null ? getItemId(value as T) : null,
              isExpanded: true,
              icon: Icon(
                Icons.expand_more,
                color: enabled ? AppColors.textSoft : AppColors.textMuted,
              ),
              hint: Text(
                'Bitte wählen...',
                style: TextStyle(color: AppColors.textMuted),
              ),
              // im zugeklappten Zustand nur der Name, ohne Stift
              selectedItemBuilder: (context) => [
                for (final item in items)
                  Align(
                    alignment: Alignment.centerLeft,
                    child: Text(getItemText(item), overflow: TextOverflow.ellipsis),
                  ),
                if (onAddNew != null) const SizedBox.shrink(),
              ],
              items: [
                ...items.map((T item) {
                  return DropdownMenuItem<String>(
                    value: getItemId(item),
                    child: onEditItem == null
                        ? Text(getItemText(item), overflow: TextOverflow.ellipsis)
                        : Row(
                            children: [
                              Expanded(
                                child: Text(getItemText(item), overflow: TextOverflow.ellipsis),
                              ),
                              IconButton(
                                tooltip: '$label bearbeiten',
                                icon: Icon(Icons.edit_outlined, size: 20, color: AppColors.text),
                                visualDensity: VisualDensity.compact,
                                onPressed: () {
                                  // zuerst die aufgeklappte Liste schließen
                                  Navigator.of(context).pop();
                                  onEditItem!(item);
                                },
                              ),
                            ],
                          ),
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
                          color: AppColors.primary,
                        ),
                        const SizedBox(width: 8),
                        Text(
                          addNewText ?? '$label hinzufügen',
                          style: const TextStyle(
                            color: AppColors.primary,
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