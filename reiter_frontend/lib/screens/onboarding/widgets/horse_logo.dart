import 'package:flutter/material.dart';

class HorseLogo extends StatelessWidget {
  final double size;

  const HorseLogo({super.key, this.size = 120});

  @override
  Widget build(BuildContext context) {
    return Image.asset(
      'assets/images/logo.png',
      height: size,
      width: size,
    );
  }
}
