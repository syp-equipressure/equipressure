import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';

class DropdownFilterChip extends StatelessWidget {
  final String label;
  final List<String> items;
  final String? selectedItem;
  final Function(String?) onSelected;

  const DropdownFilterChip({
    super.key,
    required this.label,
    required this.items,
    required this.selectedItem,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<String>(
      onSelected: (value) {
        onSelected(value == 'Alle' ? null : value);
      },
      itemBuilder: (context) {
        return [
          const PopupMenuItem(
            value: 'Alle',
            child: Row(
              children: [
                Icon(Icons.check_box_outline_blank, size: 20),
                SizedBox(width: 8),
                Text('Alle'),
              ],
            ),
          ),
          ...items.map((item) {
            final isSelected = selectedItem == item;
            return PopupMenuItem(
              value: item,
              child: Row(
                children: [
                  Icon(
                    isSelected ? Icons.check_box : Icons.check_box_outline_blank,
                    size: 20,
                  ),
                  const SizedBox(width: 8),
                  Text(item),
                ],
              ),
            );
          }),
        ];
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: selectedItem != null ? AppColors.borderSoft : AppColors.surface,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: selectedItem != null ? AppColors.textMuted : AppColors.border,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label,
              style: TextStyle(
                fontSize: 14,
                color: AppColors.text,
                fontWeight: selectedItem != null ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            const SizedBox(width: 4),
            Icon(
              Icons.arrow_drop_down,
              size: 20,
              color: AppColors.text,
            ),
          ],
        ),
      ),
    );
  }
}