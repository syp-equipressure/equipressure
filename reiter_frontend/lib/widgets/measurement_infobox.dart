import 'package:flutter/material.dart';

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
    return Row(
      children: [
        Expanded(
          child: _buildInfoBox('$value1 $unit1'),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _buildInfoBox('$value2 $unit2'),
        ),
      ],
    );
  }

  Widget _buildInfoBox(String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.grey[100],
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 14,
          color: Colors.grey[700],
        ),
        textAlign: TextAlign.center,
      ),
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
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.grey[100],
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 14,
          color: Colors.grey[700],
        ),
      ),
    );
  }
}