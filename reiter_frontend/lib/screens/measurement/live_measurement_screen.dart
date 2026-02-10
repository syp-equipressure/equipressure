import 'dart:async';
import 'dart:math';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';

/// Result returned when user stops the live measurement.
class LiveMeasurementResult {
  final Duration duration;
  final List<double> pressureData; // 400 values (20x20)

  LiveMeasurementResult({required this.duration, required this.pressureData});
}

class LiveMeasurementScreen extends StatefulWidget {
  final String gait;
  final String hand;

  const LiveMeasurementScreen({
    super.key,
    required this.gait,
    required this.hand,
  });

  @override
  State<LiveMeasurementScreen> createState() => _LiveMeasurementScreenState();
}

class _LiveMeasurementScreenState extends State<LiveMeasurementScreen> {
  static const int gridSize = 20;
  static const int outputSize = 300;
  static const Color _accent = Color(0xFF6B4C9A);
  static const Color _accentLight = Color(0xFFD4B5F5);

  late final Stopwatch _stopwatch;
  Timer? _uiTimer;
  Timer? _frameTimer;

  // Pre-generate base pattern once, then add noise each frame
  late final List<double> _basePattern;
  List<double> _currentData = List.filled(400, 0);
  ui.Image? _currentFrame;
  final Random _rng = Random();

  @override
  void initState() {
    super.initState();
    _basePattern = _generateBasePattern();
    _currentData = List.from(_basePattern);
    _stopwatch = Stopwatch()..start();

    // Update timer display every 100ms
    _uiTimer = Timer.periodic(const Duration(milliseconds: 100), (_) {
      if (mounted) setState(() {});
    });

    // Update heatmap frame every 300ms (slow enough to look like real data, not flickery)
    _frameTimer = Timer.periodic(const Duration(milliseconds: 300), (_) {
      _updateFrame();
    });

    // Generate first frame
    _updateFrame();
  }

  @override
  void dispose() {
    _stopwatch.stop();
    _uiTimer?.cancel();
    _frameTimer?.cancel();
    _currentFrame?.dispose();
    super.dispose();
  }

  /// Generate a base saddle pressure pattern based on gait.
  List<double> _generateBasePattern() {
    final data = List<double>.filled(gridSize * gridSize, 0);
    final gait = widget.gait.toLowerCase();

    double intensity;
    double centerY;
    double spreadX;
    double spreadY;

    if (gait == 'trab') {
      intensity = 6000;
      centerY = 10;
      spreadX = 5.0;
      spreadY = 6.0;
    } else if (gait == 'galopp') {
      intensity = 8000;
      centerY = 7;
      spreadX = 4.0;
      spreadY = 5.0;
    } else {
      // Schritt or other
      intensity = 4000;
      centerY = 10;
      spreadX = 6.0;
      spreadY = 7.0;
    }

    // Shift slightly based on hand
    double centerX = 10.0;
    if (widget.hand == 'Links') {
      centerX = 8.5;
    } else if (widget.hand == 'Rechts') {
      centerX = 11.5;
    }

    for (int row = 0; row < gridSize; row++) {
      for (int col = 0; col < gridSize; col++) {
        final dx = (col - centerX) / spreadX;
        final dy = (row - centerY) / spreadY;
        final distance = dx * dx + dy * dy;
        data[row * gridSize + col] = intensity * exp(-distance);
      }
    }
    return data;
  }

  void _updateFrame() async {
    // Add noise to base pattern to simulate live data
    final noisy = List<double>.generate(400, (i) {
      final base = _basePattern[i];
      final noise = (_rng.nextDouble() - 0.5) * base * 0.3;
      return max(0, base + noise);
    });

    _currentData = noisy;
    final pixels = _renderPixels(noisy);
    final image = await _pixelsToImage(pixels);

    if (mounted) {
      _currentFrame?.dispose();
      setState(() {
        _currentFrame = image;
      });
    } else {
      image.dispose();
    }
  }

  Uint8List _renderPixels(List<double> data) {
    final double minValue = data.reduce(min);
    final double maxValue = data.reduce(max);
    final pixels = Uint8List(outputSize * outputSize * 4);

    for (int y = 0; y < outputSize; y++) {
      for (int x = 0; x < outputSize; x++) {
        final double gridX = (x / outputSize) * (gridSize - 1);
        final double gridY = (y / outputSize) * (gridSize - 1);

        final int x0 = gridX.floor();
        final int y0 = gridY.floor();
        final int x1 = min(x0 + 1, gridSize - 1);
        final int y1 = min(y0 + 1, gridSize - 1);

        final double fx = gridX - x0;
        final double fy = gridY - y0;

        final double v00 = data[y0 * gridSize + x0];
        final double v10 = data[y0 * gridSize + x1];
        final double v01 = data[y1 * gridSize + x0];
        final double v11 = data[y1 * gridSize + x1];

        final double v0 = v00 * (1 - fx) + v10 * fx;
        final double v1 = v01 * (1 - fx) + v11 * fx;
        final double value = v0 * (1 - fy) + v1 * fy;

        final color = _getHeatmapColor(value, minValue, maxValue);

        final int pixelIndex = (y * outputSize + x) * 4;
        pixels[pixelIndex] = color.red;
        pixels[pixelIndex + 1] = color.green;
        pixels[pixelIndex + 2] = color.blue;
        pixels[pixelIndex + 3] = 255;
      }
    }
    return pixels;
  }

  Future<ui.Image> _pixelsToImage(Uint8List pixels) {
    final completer = Completer<ui.Image>();
    ui.decodeImageFromPixels(
      pixels,
      outputSize,
      outputSize,
      ui.PixelFormat.rgba8888,
      (image) => completer.complete(image),
    );
    return completer.future;
  }

  Color _getHeatmapColor(double value, double minVal, double maxVal) {
    final double normalized =
        maxVal == minVal ? 0 : (value - minVal) / (maxVal - minVal);
    int r, g, b;

    if (normalized < 0.2) {
      final double t = normalized / 0.2;
      r = 0;
      g = (t * 100).toInt();
      b = 255;
    } else if (normalized < 0.4) {
      final double t = (normalized - 0.2) / 0.2;
      r = 0;
      g = (100 + t * 155).toInt();
      b = (255 * (1 - t)).toInt();
    } else if (normalized < 0.6) {
      final double t = (normalized - 0.4) / 0.2;
      r = (t * 255).toInt();
      g = 255;
      b = 0;
    } else if (normalized < 0.8) {
      final double t = (normalized - 0.6) / 0.2;
      r = 255;
      g = (255 * (1 - t * 0.5)).toInt();
      b = 0;
    } else {
      final double t = (normalized - 0.8) / 0.2;
      r = 255;
      g = (127 * (1 - t)).toInt();
      b = 0;
    }

    return Color.fromARGB(255, r, g, b);
  }

  String _formatDuration(Duration d) {
    final minutes = d.inMinutes.toString().padLeft(2, '0');
    final seconds = (d.inSeconds % 60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }

  void _stopMeasurement() {
    _stopwatch.stop();
    final result = LiveMeasurementResult(
      duration: _stopwatch.elapsed,
      pressureData: List.from(_currentData),
    );
    Navigator.pop(context, result);
  }

  @override
  Widget build(BuildContext context) {
    final elapsed = _stopwatch.elapsed;

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Column(
          children: [
            // Scrollable content
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 32),
                child: Column(
                  children: [
                    const SizedBox(height: 24),
                    const Text(
                      'Live-Messung',
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                        color: Colors.black,
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Heatmap
                    Container(
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: _accent, width: 3),
                        boxShadow: [
                          BoxShadow(
                            color: _accent.withValues(alpha: 0.15),
                            blurRadius: 12,
                            spreadRadius: 2,
                          ),
                        ],
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(13),
                        child: AspectRatio(
                          aspectRatio: 1,
                          child: _currentFrame != null
                              ? CustomPaint(
                                  painter: _HeatmapPainter(_currentFrame!))
                              : Center(
                                  child: CircularProgressIndicator(
                                      color: _accent)),
                        ),
                      ),
                    ),

                    const SizedBox(height: 24),

                    // Timer bar
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: null,
                        backgroundColor: Colors.grey[200],
                        color: _accentLight,
                        minHeight: 6,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '${_formatDuration(elapsed)} min',
                      style: TextStyle(
                        fontSize: 16,
                        color: Colors.grey[600],
                        fontFamily: 'monospace',
                      ),
                    ),

                    const SizedBox(height: 32),

                    // Active gait / hand info
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _buildInfoRow('Aktiver Gang', widget.gait),
                          const SizedBox(height: 12),
                          _buildInfoRow('Aktiver Hand', widget.hand),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),

            // Stop button - always visible at bottom
            Padding(
              padding: const EdgeInsets.fromLTRB(32, 8, 32, 32),
              child: SizedBox(
                width: double.infinity,
                height: 56,
                child: ElevatedButton(
                  onPressed: _stopMeasurement,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: _accentLight,
                    foregroundColor: _accent,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                    ),
                    elevation: 0,
                  ),
                  child: const Text(
                    'Messung stoppen',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Row(
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: 14,
            color: Colors.grey[600],
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(width: 12),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: Colors.grey[300]!),
          ),
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 14,
              color: Colors.black87,
            ),
          ),
        ),
      ],
    );
  }
}

class _HeatmapPainter extends CustomPainter {
  final ui.Image image;
  _HeatmapPainter(this.image);

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..filterQuality = FilterQuality.medium;
    canvas.drawImageRect(
      image,
      Rect.fromLTWH(0, 0, image.width.toDouble(), image.height.toDouble()),
      Rect.fromLTWH(0, 0, size.width, size.height),
      paint,
    );
  }

  @override
  bool shouldRepaint(covariant _HeatmapPainter oldDelegate) {
    return oldDelegate.image != image;
  }
}
