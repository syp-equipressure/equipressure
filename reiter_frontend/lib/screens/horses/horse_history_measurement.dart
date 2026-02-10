import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/measurement.dart';
import 'dart:ui' as ui;
import 'dart:math';

class MeasurementDetailScreen extends StatefulWidget {
  final Measurement measurement;

  const MeasurementDetailScreen({
    super.key,
    required this.measurement,
  });

  @override
  State<MeasurementDetailScreen> createState() => _MeasurementDetailScreenState();
}

class _MeasurementDetailScreenState extends State<MeasurementDetailScreen> {
  static const int gridSize = 20;
  
  // Filter states
  final Map<String, bool> filters = {
    'profi': true,
    'galopp': true,
    'rechts': true,
  };
  
  bool showFilters = true;
  bool isPlaying = false;
  double currentTime = 30.0;

  String _formatDate(DateTime date) {
    return '${date.day.toString().padLeft(2, '0')}.${date.month.toString().padLeft(2, '0')}.${date.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[50],
      body: SafeArea(
        child: SingleChildScrollView(
          child: Column(
            children: [
              _buildHeader(),
              _buildFilterSection(),
              _buildHeatmap(),
              _buildInfoSection(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.all(16),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.arrow_back, color: Colors.black),
            onPressed: () => Navigator.pop(context),
          ),
          Expanded(
            child: Text(
              'Messung vom ${_formatDate(widget.measurement.date)}',
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w600,
                color: Colors.black,
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.download, color: Colors.black),
            onPressed: () {
              // TODO: Implement download functionality
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Download-Funktion kommt bald')),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildFilterSection() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: () => setState(() => showFilters = !showFilters),
            child: Row(
              children: [
                const Text(
                  'Filtern',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                    color: Colors.grey,
                  ),
                ),
                const SizedBox(width: 8),
                Icon(
                  showFilters ? Icons.arrow_drop_down : Icons.arrow_right,
                  size: 20,
                  color: Colors.grey,
                ),
              ],
            ),
          ),
          if (showFilters) ...[
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildFilterChip('⊘', false),
                _buildFilterChip('Max', false),
                _buildFilterChip('Profi', filters['profi']!, onTap: () {
                  setState(() => filters['profi'] = !filters['profi']!);
                }),
                _buildFilterChip('Schritt', false),
                _buildFilterChip('Trab', false),
                _buildFilterChip('Galopp', filters['galopp']!, onTap: () {
                  setState(() => filters['galopp'] = !filters['galopp']!);
                }),
                _buildFilterChip('Links', false),
                _buildFilterChip('Rechts', filters['rechts']!, onTap: () {
                  setState(() => filters['rechts'] = !filters['rechts']!);
                }),
              ],
            ),
            const SizedBox(height: 8),
          ],
        ],
      ),
    );
  }

  Widget _buildFilterChip(String label, bool isActive, {VoidCallback? onTap}) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: isActive ? Colors.blue[600] : Colors.white,
          border: Border.all(
            color: isActive ? Colors.blue[600]! : Colors.grey[300]!,
            width: 2,
          ),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(
          isActive && !label.contains('⊘') && label != 'Max' ? '✓ $label' : label,
          style: TextStyle(
            fontSize: 13,
            color: isActive ? Colors.white : Colors.grey[700],
            fontWeight: isActive ? FontWeight.w500 : FontWeight.normal,
          ),
        ),
      ),
    );
  }

  Widget _buildHeatmap() {
  // Lokale Testdaten (String → List<double>)
  const rawData = '''
234,328,401,488,586,599,518,421,274,160,123,106,112,172,252,255,186,141,144,135,
442,654,964,1674,2571,2592,2227,2236,1461,665,412,373,562,1182,1721,1538,940,501,382,333,
600,925,1634,3628,5978,5523,4635,5404,3457,1239,500,521,1438,4112,5824,4360,2373,1088,672,557,
586,882,1524,3342,5730,6331,6111,5980,3812,1452,655,726,2047,5068,7120,5540,2871,1236,753,628,
418,586,849,1768,3721,5185,5241,4434,2747,1208,573,688,1792,3794,5106,3978,1938,819,564,486,
222,296,376,728,1840,3437,4270,3656,2147,853,349,526,1527,3017,3683,2628,1166,468,332,277,
93,124,156,374,1277,2681,3906,3624,2093,703,270,617,1912,3259,3387,2156,832,301,214,150,
43,67,77,197,810,2136,3615,3676,2202,787,325,835,2291,3504,3127,1664,557,240,216,121,
43,88,82,113,470,1569,3169,3743,2542,1014,482,1148,2660,3562,2703,1180,360,259,278,134,
64,149,145,130,307,1167,2792,3810,2933,1338,734,1551,3095,3605,2374,891,282,316,334,142,
84,208,206,135,243,1002,2631,3921,3303,1754,1059,1951,3481,3679,2222,780,277,358,343,129,
85,228,232,127,275,1099,2767,4097,3534,1888,1206,2200,3744,3848,2317,853,346,355,296,98,
67,194,223,192,442,1465,3217,4338,3545,1859,1202,2221,3880,4206,2770,1165,471,319,217,63,
41,129,211,432,785,2092,3960,4656,3376,1647,1054,2098,4019,4923,3787,1870,687,343,182,39,
20,71,146,413,1196,2872,4609,4700,3025,1334,836,1882,4129,5941,5394,3120,1228,556,280,37,
8,37,127,512,1673,3583,4913,4322,2453,950,559,1458,3778,6409,6821,4485,1844,585,182,22,
4,24,135,637,2005,3915,4731,3600,1740,561,283,868,2772,5684,7019,5052,2113,537,91,10,
4,112,171,629,1954,3588,3905,2696,1127,271,101,365,1498,3779,5373,4132,1713,397,56,6,
3,101,137,517,1654,2919,2843,1616,542,103,26,102,550,1689,2707,2187,890,189,23,2,
1,6,47,288,956,1662,1508,726,192,30,5,17,112,393,682,573,234,48,6,1
''';

  final List<double> testData = rawData
      .replaceAll('\n', '')
      .split(',')
      .map((e) => double.tryParse(e.trim()) ?? 0)
      .take(gridSize * gridSize)
      .toList();

  return Container(
    color: Colors.white,
    padding: const EdgeInsets.all(16),
    child: Container(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.blue[700]!, width: 4),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.2),
            blurRadius: 10,
            spreadRadius: 2,
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: AspectRatio(
          aspectRatio: 1,
          child: CustomPaint(
            painter: HeatmapPainter(testData, gridSize),
          ),
        ),
      ),
    ),
  );
}

  Widget _buildInfoSection() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey[50],
      ),
      child: Column(
        children: [
          _buildInfoRow(
            Icons.pets,
            '${widget.measurement.horseName}',
          ),
          const SizedBox(height: 10),
          _buildInfoRow(
            Icons.calendar_today,
            _formatDate(widget.measurement.date),
          ),
          const SizedBox(height: 10),
          _buildInfoRow(
            Icons.person,
            widget.measurement.rider,
          ),
          const SizedBox(height: 10),
          _buildInfoRow(
            Icons.menu,
            widget.measurement.saddleName,
          ),
          const SizedBox(height: 10),
          _buildInfoRow(
            Icons.info_outline,
            widget.measurement.notes,
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String text) {
    return Row(
      children: [
        Container(
          width: 32,
          height: 32,
          alignment: Alignment.center,
          child: Icon(icon, size: 22, color: Colors.grey[600]),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              text,
              style: TextStyle(
                fontSize: 13,
                color: Colors.grey[800],
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class HeatmapPainter extends CustomPainter {
  final List<double> sensorData;
  final int gridSize;

  HeatmapPainter(this.sensorData, this.gridSize);

  @override
  void paint(Canvas canvas, Size size) {
    if (sensorData.isEmpty || sensorData.length < gridSize * gridSize) return;

    final double minValue = sensorData.reduce(min);
    final double maxValue = sensorData.reduce(max);

    // Create image data
    final int width = size.width.toInt();
    final int height = size.height.toInt();

    final ui.PictureRecorder recorder = ui.PictureRecorder();
    final Canvas imageCanvas = Canvas(recorder);

    for (int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        // Map pixel to grid coordinates
        final double gridX = (x / width) * gridSize;
        final double gridY = (y / height) * gridSize;

        // Bilinear interpolation
        final int x0 = gridX.floor();
        final int y0 = gridY.floor();
        final int x1 = min(x0 + 1, gridSize - 1);
        final int y1 = min(y0 + 1, gridSize - 1);

        final double fx = gridX - x0;
        final double fy = gridY - y0;

        final double v00 = _getValue(x0, y0);
        final double v10 = _getValue(x1, y0);
        final double v01 = _getValue(x0, y1);
        final double v11 = _getValue(x1, y1);

        final double v0 = v00 * (1 - fx) + v10 * fx;
        final double v1 = v01 * (1 - fx) + v11 * fx;
        final double value = v0 * (1 - fy) + v1 * fy;

        // Get color
        final color = _getColor(value, minValue, maxValue);

        final paint = Paint()..color = color;
        imageCanvas.drawRect(
          Rect.fromLTWH(x.toDouble(), y.toDouble(), 1, 1),
          paint,
        );
      }
    }

    final ui.Picture picture = recorder.endRecording();
    canvas.drawPicture(picture);
  }

  double _getValue(int x, int y) {
    final index = y * gridSize + x;
    if (index >= 0 && index < sensorData.length) {
      return sensorData[index];
    }
    return 0;
  }

  Color _getColor(double value, double min, double max) {
    final double normalized = (value - min) / (max - min);

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

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}