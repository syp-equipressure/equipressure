import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';

/// Zwei kurze Angaben unter einer Auswahl, z.B. Größe und Gewicht
class InfoBoxes extends StatelessWidget {
  final String value1;
  final String unit1;
  final String value2;
  final String unit2;

  const InfoBoxes({
    super.key,
    required this.value1,
    required this.unit1,
    required this.value2,
    required this.unit2,
  });

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _InfoChip('$value1 $unit1'),
        _InfoChip('$value2 $unit2'),
      ],
    );
  }
}

class SingleInfoBox extends StatelessWidget {
  final String text;

  const SingleInfoBox({
    super.key,
    required this.text,
  });

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: _InfoChip(text),
    );
  }
}

class _InfoChip extends StatelessWidget {
  final String text;

  const _InfoChip(this.text);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.purple50,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        text,
        style: const TextStyle(fontSize: 13, color: AppColors.purple700),
      ),
    );
  }
}
