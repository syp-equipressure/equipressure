import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class DateRangeFilterChip extends StatelessWidget {
  final DateTime? startDate;
  final DateTime? endDate;
  final VoidCallback onTap;
  final VoidCallback onClear;

  const DateRangeFilterChip({
    super.key,
    this.startDate,
    this.endDate,
    required this.onTap,
    required this.onClear,
  });

  bool get hasDateFilter => startDate != null || endDate != null;

  String _getDateRangeLabel() {
    if (startDate == null && endDate == null) {
      return 'Zeitraum';
    }
    
    final formatter = DateFormat('dd.MM.yy');
    if (startDate != null && endDate != null) {
      return '${formatter.format(startDate!)} - ${formatter.format(endDate!)}';
    } else if (startDate != null) {
      return 'Ab ${formatter.format(startDate!)}';
    } else {
      return 'Bis ${formatter.format(endDate!)}';
    }
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: hasDateFilter ? Colors.grey[200] : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: hasDateFilter ? Colors.grey[400]! : Colors.grey[300]!,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              _getDateRangeLabel(),
              style: TextStyle(
                fontSize: 14,
                color: Colors.black87,
                fontWeight: hasDateFilter ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            if (hasDateFilter) ...[
              const SizedBox(width: 8),
              GestureDetector(
                onTap: onClear,
                behavior: HitTestBehavior.opaque,
                child: Padding(
                  padding: const EdgeInsets.only(left: 4),
                  child: Icon(
                    Icons.close,
                    size: 16,
                    color: Colors.black54,
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