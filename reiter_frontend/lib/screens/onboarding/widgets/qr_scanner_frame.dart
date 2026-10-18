import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';

class QRScannerFrame extends StatelessWidget {
  const QRScannerFrame({super.key});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 200,
      height: 200,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          // White background
          Container(
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(16),
            ),
            child: Center(
              child: CustomPaint(
                size: const Size(140, 140),
                painter: _QRCodePlaceholderPainter(),
              ),
            ),
          ),
          // Corner brackets
          const Positioned(top: -8, left: -8, child: _Corner(isTop: true, isLeft: true)),
          const Positioned(top: -8, right: -8, child: _Corner(isTop: true, isLeft: false)),
          const Positioned(bottom: -8, left: -8, child: _Corner(isTop: false, isLeft: true)),
          const Positioned(bottom: -8, right: -8, child: _Corner(isTop: false, isLeft: false)),
        ],
      ),
    );
  }
}

class _Corner extends StatelessWidget {
  final bool isTop;
  final bool isLeft;

  const _Corner({required this.isTop, required this.isLeft});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 40,
      height: 40,
      child: CustomPaint(painter: _CornerPainter(isTop: isTop, isLeft: isLeft)),
    );
  }
}

class _CornerPainter extends CustomPainter {
  final bool isTop;
  final bool isLeft;

  _CornerPainter({required this.isTop, required this.isLeft});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = AppColors.primary
      ..strokeWidth = 4
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final path = Path();

    if (isTop && isLeft) {
      path.moveTo(size.width * 0.7, 8);
      path.lineTo(8, 8);
      path.lineTo(8, size.height * 0.7);
    } else if (isTop && !isLeft) {
      path.moveTo(size.width * 0.3, 8);
      path.lineTo(size.width - 8, 8);
      path.lineTo(size.width - 8, size.height * 0.7);
    } else if (!isTop && isLeft) {
      path.moveTo(8, size.height * 0.3);
      path.lineTo(8, size.height - 8);
      path.lineTo(size.width * 0.7, size.height - 8);
    } else {
      path.moveTo(size.width - 8, size.height * 0.3);
      path.lineTo(size.width - 8, size.height - 8);
      path.lineTo(size.width * 0.3, size.height - 8);
    }

    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _QRCodePlaceholderPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = AppColors.textPrimary
      ..style = PaintingStyle.fill;

    final cellSize = size.width / 21;

    final pattern = [
      [1,1,1,1,1,1,1,0,1,0,1,0,1,0,1,1,1,1,1,1,1],
      [1,0,0,0,0,0,1,0,0,1,0,1,0,0,1,0,0,0,0,0,1],
      [1,0,1,1,1,0,1,0,1,0,1,0,1,0,1,0,1,1,1,0,1],
      [1,0,1,1,1,0,1,0,0,1,1,1,0,0,1,0,1,1,1,0,1],
      [1,0,1,1,1,0,1,0,1,0,0,0,1,0,1,0,1,1,1,0,1],
      [1,0,0,0,0,0,1,0,0,1,0,1,0,0,1,0,0,0,0,0,1],
      [1,1,1,1,1,1,1,0,1,0,1,0,1,0,1,1,1,1,1,1,1],
      [0,0,0,0,0,0,0,0,0,1,1,1,0,0,0,0,0,0,0,0,0],
      [1,0,1,0,1,0,1,0,1,0,0,0,1,0,1,0,1,0,1,0,1],
      [0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0],
      [1,0,1,1,0,0,1,1,0,0,1,0,0,1,1,0,0,1,1,0,1],
      [0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0],
      [1,0,1,0,1,0,1,0,1,0,0,0,1,0,1,0,1,0,1,0,1],
      [0,0,0,0,0,0,0,0,1,1,0,1,1,0,0,0,0,0,0,0,0],
      [1,1,1,1,1,1,1,0,0,0,1,0,0,0,1,0,1,0,1,0,1],
      [1,0,0,0,0,0,1,0,1,1,0,1,1,0,0,1,0,1,0,1,0],
      [1,0,1,1,1,0,1,0,0,0,1,0,0,0,1,1,0,0,1,1,0],
      [1,0,1,1,1,0,1,0,1,1,0,1,1,0,0,1,0,1,0,1,0],
      [1,0,1,1,1,0,1,0,0,0,1,0,0,0,1,0,1,0,1,0,1],
      [1,0,0,0,0,0,1,0,1,1,0,1,1,0,0,0,0,0,0,0,0],
      [1,1,1,1,1,1,1,0,0,0,1,0,0,0,1,1,1,1,1,1,1],
    ];

    for (int y = 0; y < pattern.length; y++) {
      for (int x = 0; x < pattern[y].length; x++) {
        if (pattern[y][x] == 1) {
          canvas.drawRect(
            Rect.fromLTWH(x * cellSize, y * cellSize, cellSize, cellSize),
            paint,
          );
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
