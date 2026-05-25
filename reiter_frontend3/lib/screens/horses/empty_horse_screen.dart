import 'package:flutter/material.dart';

class HorseEmptyScreen extends StatelessWidget {
  final VoidCallback onAddPressed;

  const HorseEmptyScreen({super.key, required this.onAddPressed});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: GestureDetector(
        onTap: onAddPressed,
        child: Container(
          width: 260,
          height: 260,
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey),
            borderRadius: BorderRadius.circular(20),
          ),
          child: const Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.add, size: 40),
              SizedBox(height: 16),
              Text(
                'Füge ein Pferd hinzu\num mit den Messungen starten zu können',
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
