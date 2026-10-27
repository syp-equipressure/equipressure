import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

/// Filter-Chip für einen Zeitraum (von–bis). Antippen öffnet den Datumsbereich-Wähler,
/// das ✕ setzt den Filter wieder zurück.
class DateRangeFilterChip extends StatelessWidget {
  final String label;
  final DateTimeRange? selectedRange;
  final DateTime firstDate;
  final DateTime lastDate;
  final ValueChanged<DateTimeRange?> onSelected;

  const DateRangeFilterChip({
    super.key,
    required this.label,
    required this.selectedRange,
    required this.firstDate,
    required this.lastDate,
    required this.onSelected,
  });

  static final DateFormat _format = DateFormat('dd.MM.yyyy');

  Future<void> _pickRange(BuildContext context) async {
    final range = await showDateRangePicker(
      context: context,
      firstDate: firstDate,
      lastDate: lastDate,
      initialDateRange: selectedRange,
      helpText: 'Zeitraum der Messungen',
      saveText: 'Übernehmen',
    );
    if (range != null) onSelected(range);
  }

  @override
  Widget build(BuildContext context) {
    final range = selectedRange;
    final isSelected = range != null;
    final text = range == null
        ? label
        : '${_format.format(range.start)} – ${_format.format(range.end)}';

    return GestureDetector(
      onTap: () => _pickRange(context),
      child: Container(
        padding: EdgeInsets.only(
          left: 16,
          right: isSelected ? 8 : 12,
          top: 8,
          bottom: 8,
        ),
        decoration: BoxDecoration(
          color: isSelected ? Colors.grey[200] : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isSelected ? Colors.grey[400]! : Colors.grey[300]!,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              text,
              style: TextStyle(
                fontSize: 14,
                color: Colors.black87,
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            const SizedBox(width: 4),
            if (isSelected)
              GestureDetector(
                onTap: () => onSelected(null),
                child: Icon(Icons.close, size: 18, color: Colors.grey[700]),
              )
            else
              Icon(Icons.date_range, size: 18, color: Colors.grey[700]),
          ],
        ),
      ),
    );
  }
}
