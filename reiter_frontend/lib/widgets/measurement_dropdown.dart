import 'package:flutter/material.dart';

class SelectionDropdown<T> extends StatelessWidget {
  final String label;
  final T? value;
  final List<T> items;
  final String Function(T) getItemText;
  final void Function(T?) onChanged;
  final bool enabled;

  const SelectionDropdown({
    super.key,
    required this.label,
    required this.value,
    required this.items,
    required this.getItemText,
    required this.onChanged,
    this.enabled = true,
  });

  @override
  Widget build(BuildContext context) {
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
            color: Colors.white,
            border: Border.all(color: Colors.grey[300]!),
            borderRadius: BorderRadius.circular(12),
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<T>(
              value: value,
              isExpanded: true,
              hint: Text(
                'Auswählen',
                style: TextStyle(color: Colors.grey[600]),
              ),
              icon: Icon(
                Icons.arrow_forward_ios,
                size: 16,
                color: enabled ? Colors.grey[600] : Colors.grey[400],
              ),
              items: items.map((item) {
                return DropdownMenuItem<T>(
                  value: item,
                  child: Text(
                    getItemText(item),
                    style: const TextStyle(fontSize: 16),
                  ),
                );
              }).toList(),
              onChanged: enabled ? onChanged : null,
            ),
          ),
        ),
      ],
    );
  }
}