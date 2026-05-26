import 'dart:async';
import 'dart:typed_data';
import 'dart:ui' as ui;
import 'dart:math';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:path_provider/path_provider.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:reiterappfrontend/screens/horses/horse_history.dart';
import 'package:share_plus/share_plus.dart';

import 'package:flutter_svg/flutter_svg.dart';
import 'package:reiterappfrontend/models/measurement.dart';
import 'package:reiterappfrontend/models/horse.dart';

class MeasurementDetailScreen extends StatefulWidget {
  final Measurement measurement;
  final Horse horse;

  const MeasurementDetailScreen({
    super.key,
    required this.measurement,
    required this.horse,
  });

  @override
  State<MeasurementDetailScreen> createState() =>
      _MeasurementDetailScreenState();
}

class _MeasurementDetailScreenState extends State<MeasurementDetailScreen> {
  static const int gridSize = 20;
  static const int outputSize = 300;

  // Light theme colors
  static const Color _accent = Color(0xFF6B4C9A);
  static const Color _accentLight = Color(0xFFD4B5F5);
  static const Color _chipActive = Color(0xFF6B4C9A);
  static const Color _textSecondary = Color(0xFF888888);

  bool showFilters = true;

  // Filter state
  String? _selectedGait;
  String? _selectedHand;
  bool _isMaxMode = false;
  bool _isProfiMode = false;

  // Profi playbar
  bool _isPlaying = false;
  double _playbackPosition = 0.0;
  Timer? _playTimer;

  // Pre-generated profi frames (no flicker)
  static const int _profiFrameCount = 60;
  List<ui.Image>? _profiFrames;
  bool _profiFramesLoading = false;

  // Cached heatmap for non-profi mode
  ui.Image? _cachedHeatmapImage;
  List<double>? _cachedData;

  // GlobalKey for capturing heatmap
  final GlobalKey _heatmapKey = GlobalKey();

  late final List<String> _availableGaits;
  late final List<String> _availableHands;

  @override
  void initState() {
    super.initState();
    final sections = widget.measurement.sections;
    _availableGaits = sections.map((s) => s.gait).toSet().toList();
    _availableHands = sections.map((s) => s.hand).toSet().toList();
  }

  @override
  void dispose() {
    _playTimer?.cancel();
    _cachedHeatmapImage?.dispose();
    _disposeProfiFrames();
    super.dispose();
  }

  void _disposeProfiFrames() {
    if (_profiFrames != null) {
      for (final img in _profiFrames!) {
        img.dispose();
      }
      _profiFrames = null;
    }
  }

  String _formatDate(DateTime date) {
    return '${date.day.toString().padLeft(2, '0')}.${date.month.toString().padLeft(2, '0')}.${date.year}';
  }

  // ── Pixel generation (sync, returns raw pixel bytes) ──

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
        pixels[pixelIndex] = (color.r * 255.0).round().clamp(0, 255);
        pixels[pixelIndex + 1] = (color.g * 255.0).round().clamp(0, 255);
        pixels[pixelIndex + 2] = (color.b * 255.0).round().clamp(0, 255);
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

  Future<void> _generateHeatmapImage(List<double> data) async {
    if (_cachedData == data && _cachedHeatmapImage != null) return;

    final pixels = _renderPixels(data);
    _cachedHeatmapImage?.dispose();
    _cachedHeatmapImage = await _pixelsToImage(pixels);
    _cachedData = data;
    if (mounted) setState(() {});
  }

  Color _getHeatmapColor(double value, double minVal, double maxVal) {
    final double normalized =
        (maxVal == minVal) ? (value > 0 ? 0.5 : 0.0) : (value - minVal) / (maxVal - minVal);
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

  // ── Data retrieval ──

  List<double> _getFilteredPressureData() {
    final sections = widget.measurement.sections;
    if (sections.isEmpty) return List.filled(gridSize * gridSize, 0);

    var matched = sections
        .where((s) => s.pressureData.length >= gridSize * gridSize)
        .toList();

    if (_selectedGait != null) {
      matched = matched.where((s) => s.gait == _selectedGait).toList();
    }
    if (_selectedHand != null) {
      matched = matched.where((s) => s.hand == _selectedHand).toList();
    }

    if (matched.isEmpty) return List.filled(gridSize * gridSize, 0);

    if (_isMaxMode) {
      final result = List<double>.filled(gridSize * gridSize, 0);
      for (final section in matched) {
        for (int i = 0; i < gridSize * gridSize; i++) {
          result[i] = max(result[i], section.pressureData[i]);
        }
      }
      return result;
    }

    final count = matched.length;
    final averaged = List<double>.filled(gridSize * gridSize, 0);
    for (final section in matched) {
      for (int i = 0; i < gridSize * gridSize; i++) {
        averaged[i] += section.pressureData[i];
      }
    }
    for (int i = 0; i < averaged.length; i++) {
      averaged[i] /= count;
    }
    return averaged;
  }

  /// Interpolate pressure data at a given position (0.0–1.0) across sections.
  List<double> _interpolateProfiData(double position) {
    final sections = widget.measurement.sections
        .where((s) => s.pressureData.length >= gridSize * gridSize)
        .toList();
    if (sections.isEmpty) return List.filled(gridSize * gridSize, 0);

    if (sections.length == 1) {
      // Single section: simulate subtle variation over time
      final base = sections.first.pressureData;
      final wave = sin(position * 2 * pi);
      return List.generate(gridSize * gridSize, (i) {
        final variation = base[i] * 0.08 * wave *
            sin((i % gridSize) * 0.5 + position * pi);
        return (base[i] + variation).clamp(0.0, double.infinity);
      });
    }

    final pos = position.clamp(0.0, 1.0) * (sections.length - 1);
    final idx = pos.floor().clamp(0, sections.length - 2);
    final t = pos - idx;

    final a = sections[idx].pressureData;
    final b = sections[idx + 1].pressureData;
    return List.generate(
        gridSize * gridSize, (i) => a[i] * (1 - t) + b[i] * t);
  }

  // ── Pre-generate all profi frames ──

  Future<void> _generateProfiFrames() async {
    if (_profiFramesLoading) return;
    _profiFramesLoading = true;

    _disposeProfiFrames();
    final frames = <ui.Image>[];

    for (int f = 0; f < _profiFrameCount; f++) {
      final position = f / (_profiFrameCount - 1);
      final data = _interpolateProfiData(position);
      final pixels = _renderPixels(data);
      final image = await _pixelsToImage(pixels);
      frames.add(image);
    }

    if (mounted) {
      setState(() {
        _profiFrames = frames;
        _profiFramesLoading = false;
      });
    } else {
      for (final img in frames) {
        img.dispose();
      }
      _profiFramesLoading = false;
    }
  }

  void _invalidateCache() {
    _cachedHeatmapImage?.dispose();
    _cachedHeatmapImage = null;
    _cachedData = null;
  }

  // ── Filter actions ──

  void _onClearFilters() {
    _playTimer?.cancel();
    setState(() {
      _selectedGait = null;
      _selectedHand = null;
      _isMaxMode = false;
      _isProfiMode = false;
      _isPlaying = false;
      _playbackPosition = 0.0;
      _invalidateCache();
      _disposeProfiFrames();
    });
  }

  void _onToggleMax() {
    _playTimer?.cancel();
    setState(() {
      _isMaxMode = !_isMaxMode;
      if (_isMaxMode) {
        _isProfiMode = false;
        _disposeProfiFrames();
      }
      _isPlaying = false;
      _invalidateCache();
    });
  }

  void _onToggleProfi() {
    _playTimer?.cancel();
    setState(() {
      _isProfiMode = !_isProfiMode;
      if (_isProfiMode) {
        _isMaxMode = false;
        _invalidateCache();
        _generateProfiFrames();
      } else {
        _isPlaying = false;
        _playbackPosition = 0.0;
        _disposeProfiFrames();
        _invalidateCache();
      }
    });
  }

  void _onSelectGait(String gait) {
    setState(() {
      _selectedGait = _selectedGait == gait ? null : gait;
      _invalidateCache();
    });
  }

  void _onSelectHand(String hand) {
    setState(() {
      _selectedHand = _selectedHand == hand ? null : hand;
      _invalidateCache();
    });
  }

  void _togglePlayback() {
    if (_profiFrames == null) return;
    if (_isPlaying) {
      _playTimer?.cancel();
      setState(() => _isPlaying = false);
    } else {
      setState(() => _isPlaying = true);
      _playTimer = Timer.periodic(const Duration(milliseconds: 80), (_) {
        setState(() {
          _playbackPosition += 1.0 / _profiFrameCount;
          if (_playbackPosition >= 1.0) _playbackPosition = 0.0;
        });
      });
    }
  }

  String _formatPlaybackTime(double position) {
    const totalSeconds = 60;
    final current = (position * totalSeconds).round();
    final m = current ~/ 60;
    final s = current % 60;
    return '$m:${s.toString().padLeft(2, '0')}';
  }

  /// Get the current profi frame image (no async, instant).
  ui.Image? _getCurrentProfiFrame() {
    if (_profiFrames == null || _profiFrames!.isEmpty) return null;
    final idx =
        (_playbackPosition * (_profiFrames!.length - 1)).round().clamp(0, _profiFrames!.length - 1);
    return _profiFrames![idx];
  }

  /// Get current section label for profi playbar.
  String _getCurrentProfiLabel() {
    final sections = widget.measurement.sections
        .where((s) => s.pressureData.length >= gridSize * gridSize)
        .toList();
    if (sections.isEmpty) return '';
    final idx = (_playbackPosition * (sections.length - 1))
        .round()
        .clamp(0, sections.length - 1);
    return '${sections[idx].gait} ${sections[idx].hand}';
  }

  // ── PDF Generation ──

  Future<void> _generateAndSharePdf() async {
    try {
      // Show loading indicator
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Row(
            children: [
              SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                ),
              ),
              SizedBox(width: 16),
              Text('PDF wird erstellt...'),
            ],
          ),
          duration: Duration(seconds: 2),
        ),
      );

      // Capture heatmap as image
      final RenderRepaintBoundary boundary = _heatmapKey.currentContext!
          .findRenderObject() as RenderRepaintBoundary;
      final ui.Image image = await boundary.toImage(pixelRatio: 2.0);
      final ByteData? byteData =
          await image.toByteData(format: ui.ImageByteFormat.png);
      final Uint8List pngBytes = byteData!.buffer.asUint8List();

      // Create PDF
      final pdf = pw.Document();
      final m = widget.measurement;

      // Add page
      pdf.addPage(
        pw.Page(
          pageFormat: PdfPageFormat.a4,
          build: (pw.Context context) {
            return pw.Column(
              crossAxisAlignment: pw.CrossAxisAlignment.start,
              children: [
                // Title
                pw.Text(
                  'Satteldruckmessung',
                  style: pw.TextStyle(
                    fontSize: 24,
                    fontWeight: pw.FontWeight.bold,
                  ),
                ),
                pw.SizedBox(height: 8),
                pw.Text(
                  'Messung vom ${_formatDate(m.date)}',
                  style: const pw.TextStyle(fontSize: 16),
                ),
                pw.Divider(thickness: 2),
                pw.SizedBox(height: 20),

                // Horse Information
                pw.Text(
                  'Pferdeinformationen',
                  style: pw.TextStyle(
                    fontSize: 16,
                    fontWeight: pw.FontWeight.bold,
                  ),
                ),
                pw.SizedBox(height: 8),
                _pdfInfoRow('Pferd:', '${m.horseName} | ${m.weight} | ${m.height}'),
                _pdfInfoRow('Reiter:', m.rider),
                _pdfInfoRow('Sattel:', m.saddleName),
                if (m.notes.isNotEmpty) _pdfInfoRow('Notizen:', m.notes),
                pw.SizedBox(height: 20),

                // Filter Information
                if (_selectedGait != null || _selectedHand != null || _isMaxMode)
                  pw.Column(
                    crossAxisAlignment: pw.CrossAxisAlignment.start,
                    children: [
                      pw.Text(
                        'Angewandte Filter',
                        style: pw.TextStyle(
                          fontSize: 16,
                          fontWeight: pw.FontWeight.bold,
                        ),
                      ),
                      pw.SizedBox(height: 8),
                      if (_selectedGait != null)
                        _pdfInfoRow('Gangart:', _selectedGait!),
                      if (_selectedHand != null)
                        _pdfInfoRow('Hand:', _selectedHand!),
                      if (_isMaxMode)
                        _pdfInfoRow('Modus:', 'Maximum'),
                      pw.SizedBox(height: 20),
                    ],
                  ),

                // Heatmap
                pw.Text(
                  'Druckverteilung',
                  style: pw.TextStyle(
                    fontSize: 16,
                    fontWeight: pw.FontWeight.bold,
                  ),
                ),
                pw.SizedBox(height: 12),
                pw.Center(
                  child: pw.Container(
                    width: 400,
                    height: 400,
                    decoration: pw.BoxDecoration(
                      border: pw.Border.all(
                        color: PdfColor.fromHex('#6B4C9A'),
                        width: 2,
                      ),
                    ),
                    child: pw.Image(
                      pw.MemoryImage(pngBytes),
                      fit: pw.BoxFit.contain,
                    ),
                  ),
                ),

                pw.Spacer(),

                // Footer
                pw.Text(
                  'Erstellt am ${_formatDate(DateTime.now())}',
                  style: pw.TextStyle(
                    fontSize: 10,
                    color: PdfColors.grey,
                  ),
                ),
              ],
            );
          },
        ),
      );

      // Save PDF to temporary directory
      final output = await getTemporaryDirectory();
      final file = File(
          '${output.path}/messung_${m.horseName}_${_formatDate(m.date)}.pdf');
      await file.writeAsBytes(await pdf.save());

      // Share the PDF
      if (mounted) {
        await Share.shareXFiles(
          [XFile(file.path)],
          subject: 'Satteldruckmessung ${m.horseName}',
        );

        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('PDF erfolgreich erstellt'),
            backgroundColor: Colors.green,
            duration: Duration(seconds: 2),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Fehler beim Erstellen des PDFs: $e'),
            backgroundColor: Colors.red,
            duration: const Duration(seconds: 3),
          ),
        );
      }
    }
  }

  pw.Widget _pdfInfoRow(String label, String value) {
    return pw.Padding(
      padding: const pw.EdgeInsets.only(bottom: 4),
      child: pw.Row(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.SizedBox(
            width: 120,
            child: pw.Text(
              label,
              style: pw.TextStyle(fontWeight: pw.FontWeight.bold),
            ),
          ),
          pw.Expanded(
            child: pw.Text(value),
          ),
        ],
      ),
    );
  }

  // ── Build ──

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
              if (_isProfiMode) _buildPlaybar(),
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
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 12),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.arrow_back, color: Colors.black),
            onPressed: () {
              // Navigate back to Horse History
              Navigator.of(context).pushAndRemoveUntil(
                MaterialPageRoute(
                  builder: (context) => HorseHistoryScreen(horse: widget.horse),
                ),
                (route) => false,
              );
            },
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
            onPressed: _generateAndSharePdf,
          ),
        ],
      ),
    );
  }

  Widget _buildFilterSection() {
    final sections = widget.measurement.sections;
    if (sections.isEmpty) return const SizedBox.shrink();

    final bool noFilterActive =
        _selectedGait == null &&
        _selectedHand == null &&
        !_isMaxMode &&
        !_isProfiMode;

    return Container(
      color: Colors.white,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
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
                const SizedBox(width: 4),
                Icon(
                  showFilters ? Icons.arrow_drop_down : Icons.arrow_right,
                  size: 20,
                  color: Colors.grey,
                ),
              ],
            ),
          ),
          if (showFilters) ...[
            const SizedBox(height: 10),
            // Row 1: ⊘ Durchschnitt, Max, Profi
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildChip(
                  '⊘  Durchschnitt',
                  noFilterActive,
                  onTap: _onClearFilters,
                ),
                _buildChip('Max', _isMaxMode, onTap: _onToggleMax),
                _buildChip('Profi', _isProfiMode, onTap: _onToggleProfi),
              ],
            ),
            const SizedBox(height: 8),
            // Row 2: Gangarten
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _availableGaits
                  .map((g) => _buildChip(
                        g,
                        _selectedGait == g,
                        onTap: () => _onSelectGait(g),
                      ))
                  .toList(),
            ),
            const SizedBox(height: 8),
            // Row 3: Hände
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _availableHands
                  .map((h) => _buildChip(
                        h,
                        _selectedHand == h,
                        onTap: () => _onSelectHand(h),
                      ))
                  .toList(),
            ),
            const SizedBox(height: 8),
          ],
        ],
      ),
    );
  }

  Widget _buildChip(String label, bool isActive, {VoidCallback? onTap}) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
        decoration: BoxDecoration(
          color: isActive ? _chipActive : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isActive ? _chipActive : Colors.grey[300]!,
            width: 1.5,
          ),
        ),
        child: Text(
          isActive ? '✓ $label' : label,
          style: TextStyle(
            fontSize: 13,
            color: isActive ? Colors.white : Colors.grey[700],
            fontWeight: isActive ? FontWeight.w600 : FontWeight.normal,
          ),
        ),
      ),
    );
  }

  Widget _buildHeatmap() {
    // In Profi mode use pre-generated frames, otherwise generate on demand
    ui.Image? displayImage;

    if (_isProfiMode) {
      displayImage = _getCurrentProfiFrame();
    } else {
      final data = _getFilteredPressureData();
      if (_cachedHeatmapImage == null) {
        _generateHeatmapImage(data);
      }
      displayImage = _cachedHeatmapImage;
    }

    return Padding(
      padding: const EdgeInsets.all(16),
      child: RepaintBoundary(
        key: _heatmapKey,
        child: Container(
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
              child: displayImage != null
                  ? CustomPaint(painter: CachedHeatmapPainter(displayImage))
                  : Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          CircularProgressIndicator(color: _accent),
                          if (_isProfiMode) ...[
                            const SizedBox(height: 12),
                            Text(
                              'Frames werden generiert...',
                              style: TextStyle(
                                fontSize: 13,
                                color: Colors.grey[600],
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildPlaybar() {
    final sections = widget.measurement.sections
        .where((s) => s.pressureData.length >= gridSize * gridSize)
        .toList();
    if (sections.isEmpty) return const SizedBox.shrink();

    final framesReady = _profiFrames != null;

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
      ),
      child: Column(
        children: [
          Text(
            _getCurrentProfiLabel(),
            style: const TextStyle(
              fontSize: 13,
              color: _accent,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              GestureDetector(
                onTap: framesReady ? _togglePlayback : null,
                child: Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    color: framesReady ? _accent : Colors.grey[300],
                    borderRadius: BorderRadius.circular(18),
                  ),
                  child: Icon(
                    _isPlaying ? Icons.pause : Icons.play_arrow,
                    color: Colors.white,
                    size: 20,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: SliderTheme(
                  data: SliderThemeData(
                    activeTrackColor: _accent,
                    inactiveTrackColor: _accentLight.withValues(alpha: 0.5),
                    thumbColor: _accent,
                    thumbShape:
                        const RoundSliderThumbShape(enabledThumbRadius: 6),
                    trackHeight: 4,
                    overlayShape:
                        const RoundSliderOverlayShape(overlayRadius: 14),
                  ),
                  child: Slider(
                    value: _playbackPosition,
                    onChanged: framesReady
                        ? (v) => setState(() => _playbackPosition = v)
                        : null,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Text(
                '${_formatPlaybackTime(_playbackPosition)} | 1:00',
                style: const TextStyle(
                  fontSize: 12,
                  color: _textSecondary,
                  fontFamily: 'monospace',
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInfoSection() {
    final m = widget.measurement;
    final infoItems = [
      _InfoItem(text: '${m.horseName} | ${m.weight} | ${m.height}', svgPath: 'assets/icon/horseIcon.svg'),
      _InfoItem(text: _formatDate(m.date), icon: Icons.calendar_today),
      _InfoItem(text: m.rider, icon: Icons.person),
      _InfoItem(text: m.saddleName, svgPath: 'assets/icon/saddleIcon.svg'),
      if (m.notes.isNotEmpty) _InfoItem(text: m.notes, icon: Icons.info_outline),
    ];

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
      ),
      child: Column(
        children: infoItems
            .map((item) => Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: Row(
                    children: [
                      item.svgPath != null
                          ? SvgPicture.asset(
                              item.svgPath!,
                              width: 20,
                              height: 20,
                              colorFilter: ColorFilter.mode(Colors.grey[600]!, BlendMode.srcIn),
                            )
                          : Icon(item.icon, size: 20, color: Colors.grey[600]),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          item.text,
                          style: TextStyle(
                            fontSize: 13,
                            color: Colors.grey[800],
                          ),
                        ),
                      ),
                    ],
                  ),
                ))
            .toList(),
      ),
    );
  }
}

class _InfoItem {
  final IconData? icon;
  final String? svgPath;
  final String text;
  _InfoItem({this.icon, this.svgPath, required this.text});
}

class CachedHeatmapPainter extends CustomPainter {
  final ui.Image image;
  CachedHeatmapPainter(this.image);

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
  bool shouldRepaint(covariant CachedHeatmapPainter oldDelegate) {
    return oldDelegate.image != image;
  }
}