import 'package:flutter/material.dart';

class StyledDropdown<T> extends StatelessWidget {
  final T? value;
  final String placeholder;
  final List<PopupMenuEntry<T?>> items;
  final Widget Function(T value) labelBuilder;
  final void Function(T? value) onSelected;

  const StyledDropdown({
    super.key,
    required this.value,
    required this.placeholder,
    required this.items,
    required this.labelBuilder,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<T?>(
      offset: const Offset(0, 58),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      onSelected: onSelected,
      itemBuilder: (_) => items,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          border: Border.all(color: Colors.grey[300]!),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          children: [
            Expanded(
              child: value == null
                  ? Text(
                      placeholder,
                      style: TextStyle(
                        fontSize: 16,
                        color: Colors.grey[600],
                      ),
                    )
                  : labelBuilder(value as T),
            ),
            Icon(
              Icons.keyboard_arrow_down,
              color: Colors.grey[600],
            ),
          ],
        ),
      ),
    );
  }
}
