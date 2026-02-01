import 'package:flutter/material.dart';

/// Ein wiederverwendbarer Info-Button, der beim Klicken einen Dialog mit Text anzeigt
class InfoButton extends StatelessWidget {
  final String infoText;
  final double size;
  final Color? iconColor;

  const InfoButton({
    Key? key,
    required this.infoText,
    this.size = 20.0,
    this.iconColor,
  }) : super(key: key);

  void _showInfoDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          content: Text(infoText),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text('OK'),
            ),
          ],
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => _showInfoDialog(context),
      child: Container(
        padding: const EdgeInsets.all(4),
        child: Icon(
          Icons.info_outline,
          size: size,
          color: iconColor ?? Colors.grey[600],
        ),
      ),
    );
  }
}