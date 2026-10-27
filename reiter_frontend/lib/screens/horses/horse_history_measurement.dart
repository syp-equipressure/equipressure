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
import 'package:reiterappfrontend/services/measurement_service.dart';

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
  static const Color _textSecondary = Color(0xFF888888);

  bool showFilters = true;

  // Filter state (wie im Sattler-Frontend: Gangart, Hand und Ansicht)
  String? _selectedGait;
  String? _selectedHand;
  _PressureView _view = _PressureView.average;

  bool get _isMaxMode => _view == _PressureView.maximum;
  bool get _isProfiMode => _view == _PressureView.pro;

  // Profi playbar
  bool _isPlaying = false;
  bool _autoPlayWhenReady = false;
  double _playbackPosition = 0.0;
  Timer? _playTimer;

  // Pre-generated profi frames (no flicker)
  static const int _profiFrameCount = 90;
  List<ui.Image>? _profiFrames;
  int _profiGeneration = 0;

  // Cached heatmap for non-profi mode
  ui.Image? _cachedHeatmapImage;
  List<double>? _cachedData;

  // GlobalKey for capturing heatmap
  final GlobalKey _heatmapKey = GlobalKey();

  /// Die Liste liefert Messungen ohne Aufnahmen, die werden hier nachgeladen.
  late Measurement _measurement = widget.measurement;
  bool _loadingData = false;

  @override
  void initState() {
    super.initState();
    _loadFullMeasurement();
  }

  Future<void> _loadFullMeasurement() async {
    // eine gerade aufgenommene Messung hat ihre Daten schon
    if (_measurement.sections.any((s) => s.pressureData.isNotEmpty)) return;
    setState(() => _loadingData = true);
    try {
      final full = await MeasurementService().getMeasurementById(
        _measurement.id,
        horseId: _measurement.horseId,
      );
      if (!mounted) return;
      setState(() {
        if (full != null) _measurement = full;
        _loadingData = false;
        _invalidateCache();
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _loadingData = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Messdaten konnten nicht geladen werden: $e')),
      );
    }
  }

  /// Teilmessungen mit Heatmap-Daten, nur deren Gangarten und Hände stehen zur Auswahl.
  List<MeasurementSectionData> get _recordedSections => _measurement.sections
      .where((s) => s.pressureData.length >= gridSize * gridSize)
      .toList();

  /// Nur Gangarten, die gemessen wurden.
  List<String> get _gaitOptions =>
      _recordedSections.map((s) => s.gait).toSet().toList();

  /// Nur Hände, die (für die gewählte Gangart) gemessen wurden.
  List<String> get _handOptions => _recordedSections
      .where((s) => _selectedGait == null || s.gait == _selectedGait)
      .map((s) => s.hand)
      .toSet()
      .toList();

  /// Profi geht nur mit gewählter Gangart und Hand, für die es eine Teilmessung gibt.
  bool get _isProAvailable =>
      _selectedGait != null &&
      _selectedHand != null &&
      _matchedSections.isNotEmpty;

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
    final double normalized = (maxVal == minVal)
        ? (value > 0 ? 0.5 : 0.0)
        : (value - minVal) / (maxVal - minVal);
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

  /// Teilmessungen mit Heatmap-Daten, die zu den gewählten Filtern passen.
  List<MeasurementSectionData> get _matchedSections {
    return _measurement.sections
        .where((s) => s.pressureData.length >= gridSize * gridSize)
        .where((s) => _selectedGait == null || s.gait == _selectedGait)
        .where((s) => _selectedHand == null || s.hand == _selectedHand)
        .toList();
  }

  /// Die einzelnen Aufnahmen einer Teilmessung, ohne Aufnahmen nur der Durchschnitt.
  List<List<double>> _framesOf(MeasurementSectionData section) {
    final frames = section.frames
        .where((f) => f.length >= gridSize * gridSize)
        .toList();
    return frames.isNotEmpty ? frames : [section.pressureData];
  }

  /// Ohne bekannte Dauer zählt eine Teilmessung als 30 Sekunden.
  Duration _durationOf(MeasurementSectionData section) {
    final duration = section.duration;
    return duration == null || duration.inSeconds <= 0
        ? const Duration(seconds: 30)
        : duration;
  }

  Duration get _totalDuration => _matchedSections.fold(
    Duration.zero,
    (sum, section) => sum + _durationOf(section),
  );

  List<double> _getFilteredPressureData() {
    final frames = _matchedSections.expand(_framesOf).toList();
    if (frames.isEmpty) return List.filled(gridSize * gridSize, 0);

    // Max: höchster Wert jedes Sensors über alle Aufnahmen
    if (_isMaxMode) {
      final result = List<double>.filled(gridSize * gridSize, 0);
      for (final frame in frames) {
        for (int i = 0; i < gridSize * gridSize; i++) {
          result[i] = max(result[i], frame[i]);
        }
      }
      return result;
    }

    // Durchschnitt über alle Aufnahmen
    final averaged = List<double>.filled(gridSize * gridSize, 0);
    for (final frame in frames) {
      for (int i = 0; i < gridSize * gridSize; i++) {
        averaged[i] += frame[i];
      }
    }
    for (int i = 0; i < averaged.length; i++) {
      averaged[i] /= frames.length;
    }
    return averaged;
  }

  /// Welche Teilmessung an einer Stelle (0.0–1.0) der Zeitachse läuft und wie weit
  /// sie dort fortgeschritten ist (0.0–1.0). Die Teilmessungen laufen nacheinander ab.
  (MeasurementSectionData, double, Duration)? _sectionAt(double position) {
    final sections = _matchedSections;
    if (sections.isEmpty) return null;

    final totalSeconds = _totalDuration.inMilliseconds / 1000;
    var remaining = position.clamp(0.0, 1.0) * totalSeconds;
    for (final section in sections) {
      final seconds = _durationOf(section).inMilliseconds / 1000;
      if (remaining <= seconds || section == sections.last) {
        final progress = (remaining / seconds).clamp(0.0, 1.0);
        return (
          section,
          progress,
          Duration(milliseconds: (remaining * 1000).round()),
        );
      }
      remaining -= seconds;
    }
    return null;
  }

  /// Druckbild an einer Stelle (0.0–1.0) der Zeitachse, zwischen zwei Aufnahmen
  /// wird interpoliert.
  List<double> _interpolateProfiData(double position) {
    final current = _sectionAt(position);
    if (current == null) return List.filled(gridSize * gridSize, 0);
    final (section, progress, _) = current;
    final frames = _framesOf(section);

    if (frames.length == 1) {
      // Nur ein Druckbild vorhanden: leichte Bewegung über die Zeit andeuten
      final base = frames.first;
      final wave = sin(progress * 2 * pi);
      return List.generate(gridSize * gridSize, (i) {
        final variation =
            base[i] * 0.08 * wave * sin((i % gridSize) * 0.5 + progress * pi);
        return (base[i] + variation).clamp(0.0, double.infinity);
      });
    }

    final pos = progress * (frames.length - 1);
    final idx = pos.floor().clamp(0, frames.length - 2);
    final t = pos - idx;

    final a = frames[idx];
    final b = frames[idx + 1];
    return List.generate(gridSize * gridSize, (i) => a[i] * (1 - t) + b[i] * t);
  }

  // ── Pre-generate all profi frames ──

  /// So viele Bilder wie Aufnahmen vorhanden sind, mindestens 30 und höchstens
  /// [_profiFrameCount], damit die Wiedergabe flüssig bleibt.
  int get _profiImageCount {
    final recorded = _matchedSections.fold<int>(
      0,
      (sum, section) => sum + _framesOf(section).length,
    );
    return recorded.clamp(30, _profiFrameCount);
  }

  Future<void> _generateProfiFrames() async {
    // eine neuere Generierung (z.B. nach Filterwechsel) verwirft die laufende
    final generation = ++_profiGeneration;

    _disposeProfiFrames();
    final frames = <ui.Image>[];
    final imageCount = _profiImageCount;

    for (int f = 0; f < imageCount; f++) {
      final position = f / (imageCount - 1);
      final data = _interpolateProfiData(position);
      final pixels = _renderPixels(data);
      final image = await _pixelsToImage(pixels);
      frames.add(image);
      if (generation != _profiGeneration) {
        for (final img in frames) {
          img.dispose();
        }
        return;
      }
    }

    if (mounted && _isProfiMode) {
      setState(() {
        _profiFrames = frames;
      });
      if (_autoPlayWhenReady) {
        _autoPlayWhenReady = false;
        _startPlayback();
      }
    } else {
      for (final img in frames) {
        img.dispose();
      }
    }
  }

  void _invalidateCache() {
    _cachedHeatmapImage?.dispose();
    _cachedHeatmapImage = null;
    _cachedData = null;
  }

  // ── Filter actions ──

  void _onResetFilters() {
    _resetPlayback();
    setState(() {
      _selectedGait = null;
      _selectedHand = null;
      _view = _PressureView.average;
      _invalidateCache();
      _disposeProfiFrames();
    });
  }

  void _onSelectGait(String? gait) {
    setState(() {
      _selectedGait = gait;
      // die Hand bleibt nur, wenn sie für die neue Gangart gemessen wurde
      if (_selectedHand != null && !_handOptions.contains(_selectedHand)) {
        _selectedHand = null;
      }
      _invalidateCache();
    });
    _onFilterChanged();
  }

  void _onSelectHand(String? hand) {
    setState(() {
      _selectedHand = hand;
      _invalidateCache();
    });
    _onFilterChanged();
  }

  /// Nach einem Filterwechsel: Profi ohne vollständige Auswahl fällt auf Durchschnitt
  /// zurück, sonst wird die Wiedergabe mit der neuen Teilmessung neu aufgebaut.
  void _onFilterChanged() {
    _resetPlayback();
    if (!_isProfiMode) return;
    if (!_isProAvailable) {
      setState(() {
        _view = _PressureView.average;
        _disposeProfiFrames();
        _invalidateCache();
      });
      return;
    }
    _autoPlayWhenReady = true;
    _generateProfiFrames();
  }

  void _onSelectView(_PressureView view) {
    if (view == _PressureView.pro && !_isProAvailable) return;
    _resetPlayback();
    setState(() {
      _view = view;
      _invalidateCache();
      _disposeProfiFrames();
    });
    if (view == _PressureView.pro) {
      // wie im Sattler-Frontend startet der Profimodus direkt mit der Wiedergabe
      _autoPlayWhenReady = true;
      _generateProfiFrames();
    }
  }

  void _resetPlayback() {
    _playTimer?.cancel();
    _autoPlayWhenReady = false;
    if (!mounted) return;
    setState(() {
      _isPlaying = false;
      _playbackPosition = 0.0;
    });
  }

  void _startPlayback() {
    if (_profiFrames == null) return;
    _playTimer?.cancel();
    if (_playbackPosition >= 1.0) _playbackPosition = 0.0;
    setState(() => _isPlaying = true);

    // in Echtzeit abspielen: die Position ergibt sich aus der verstrichenen Zeit
    final totalMs = _totalDuration.inMilliseconds;
    if (totalMs <= 0) return;
    final startPosition = _playbackPosition;
    final stopwatch = Stopwatch()..start();
    _playTimer = Timer.periodic(const Duration(milliseconds: 80), (_) {
      if (!mounted) return;
      setState(() {
        _playbackPosition =
            startPosition + stopwatch.elapsedMilliseconds / totalMs;
        // am Ende anhalten statt von vorne zu beginnen
        if (_playbackPosition >= 1.0) {
          _playbackPosition = 1.0;
          _isPlaying = false;
          _playTimer?.cancel();
        }
      });
    });
  }

  void _togglePlayback() {
    if (_profiFrames == null) return;
    if (_isPlaying) {
      _playTimer?.cancel();
      setState(() => _isPlaying = false);
    } else {
      _startPlayback();
    }
  }

  void _stopPlayback() {
    _playTimer?.cancel();
    setState(() {
      _isPlaying = false;
      _playbackPosition = 0.0;
    });
  }

  /// Spult um [seconds] Sekunden vor oder zurück.
  void _skipPlayback(int seconds) {
    final totalSeconds = _totalDuration.inMilliseconds / 1000;
    if (totalSeconds <= 0) return;
    _seekPlayback((_playbackPosition + seconds / totalSeconds).clamp(0.0, 1.0));
  }

  /// Springt an eine Stelle, eine laufende Wiedergabe läuft von dort weiter.
  void _seekPlayback(double position) {
    setState(() => _playbackPosition = position);
    if (_isPlaying) {
      if (position >= 1.0) {
        _playTimer?.cancel();
        setState(() => _isPlaying = false);
      } else {
        _startPlayback();
      }
    }
  }

  String _formatDuration(Duration duration) {
    final totalSeconds = duration.inSeconds;
    final m = totalSeconds ~/ 60;
    final s = totalSeconds % 60;
    return '$m:${s.toString().padLeft(2, '0')}';
  }

  String _formatPlaybackTime(double position) {
    final totalMs = _totalDuration.inMilliseconds;
    return _formatDuration(
      Duration(milliseconds: (position * totalMs).round()),
    );
  }

  /// Get the current profi frame image (no async, instant).
  ui.Image? _getCurrentProfiFrame() {
    if (_profiFrames == null || _profiFrames!.isEmpty) return null;
    final idx = (_playbackPosition * (_profiFrames!.length - 1)).round().clamp(
      0,
      _profiFrames!.length - 1,
    );
    return _profiFrames![idx];
  }

  /// Get current section label for profi playbar.
  String _getCurrentProfiLabel() {
    final current = _sectionAt(_playbackPosition);
    if (current == null) return '';
    final (section, progress, _) = current;
    final elapsed = Duration(
      milliseconds: (_durationOf(section).inMilliseconds * progress).round(),
    );
    return '${section.gait} ${section.hand} · '
        '${_formatDuration(elapsed)} / ${_formatDuration(_durationOf(section))}';
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
      final RenderRepaintBoundary boundary =
          _heatmapKey.currentContext!.findRenderObject()
              as RenderRepaintBoundary;
      final ui.Image image = await boundary.toImage(pixelRatio: 2.0);
      final ByteData? byteData = await image.toByteData(
        format: ui.ImageByteFormat.png,
      );
      final Uint8List pngBytes = byteData!.buffer.asUint8List();

      // Create PDF
      final pdf = pw.Document();
      final m = _measurement;

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
                _pdfInfoRow(
                  'Pferd:',
                  '${m.horseName} | ${m.weight} | ${m.height}',
                ),
                _pdfInfoRow('Reiter:', m.rider),
                _pdfInfoRow('Sattel:', m.saddleName),
                if (m.notes.isNotEmpty) _pdfInfoRow('Notizen:', m.notes),
                pw.SizedBox(height: 20),

                // Filter Information
                if (_selectedGait != null ||
                    _selectedHand != null ||
                    _isMaxMode)
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
                      if (_isMaxMode) _pdfInfoRow('Modus:', 'Maximum'),
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
                  style: pw.TextStyle(fontSize: 10, color: PdfColors.grey),
                ),
              ],
            );
          },
        ),
      );

      // Save PDF to temporary directory
      final output = await getTemporaryDirectory();
      final file = File(
        '${output.path}/messung_${m.horseName}_${_formatDate(m.date)}.pdf',
      );
      await file.writeAsBytes(await pdf.save());

      // Share the PDF
      if (mounted) {
        final messenger = ScaffoldMessenger.of(context);
        await SharePlus.instance.share(
          ShareParams(
            files: [XFile(file.path)],
            subject: 'Satteldruckmessung ${m.horseName}',
          ),
        );

        messenger.showSnackBar(
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
          pw.Expanded(child: pw.Text(value)),
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
              'Messung vom ${_formatDate(_measurement.date)}',
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
    if (_recordedSections.isEmpty) return const SizedBox.shrink();

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
            Row(
              children: [
                Expanded(
                  child: _buildSelect<String?>(
                    label: 'Gangart',
                    value: _selectedGait,
                    items: [
                      const DropdownMenuItem(
                        value: null,
                        child: Text('Alle Gangarten'),
                      ),
                      ..._gaitOptions.map(
                        (gait) =>
                            DropdownMenuItem(value: gait, child: Text(gait)),
                      ),
                    ],
                    onChanged: _onSelectGait,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _buildSelect<String?>(
                    label: 'Hand',
                    value: _selectedHand,
                    items: [
                      const DropdownMenuItem(
                        value: null,
                        child: Text('Alle Hände'),
                      ),
                      ..._handOptions.map(
                        (hand) =>
                            DropdownMenuItem(value: hand, child: Text(hand)),
                      ),
                    ],
                    onChanged: _onSelectHand,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _buildSelect<_PressureView>(
                    label: 'Ansicht',
                    value: _view,
                    items: [
                      const DropdownMenuItem(
                        value: _PressureView.average,
                        child: Text('Durchschnitt'),
                      ),
                      const DropdownMenuItem(
                        value: _PressureView.maximum,
                        child: Text('Maximum'),
                      ),
                      DropdownMenuItem(
                        value: _PressureView.pro,
                        enabled: _isProAvailable,
                        child: Text(
                          'Profi',
                          style: TextStyle(
                            color: _isProAvailable ? null : Colors.grey[400],
                          ),
                        ),
                      ),
                    ],
                    onChanged: (view) {
                      if (view != null) _onSelectView(view);
                    },
                  ),
                ),
              ],
            ),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: _onResetFilters,
                style: TextButton.styleFrom(foregroundColor: _accent),
                child: const Text('Filter zurücksetzen'),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildSelect<T>({
    required String label,
    required T value,
    required List<DropdownMenuItem<T>> items,
    required ValueChanged<T?> onChanged,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w600,
            color: _textSecondary,
          ),
        ),
        const SizedBox(height: 4),
        Container(
          padding: const EdgeInsets.only(left: 10, right: 4),
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey[300]!),
            borderRadius: BorderRadius.circular(8),
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<T>(
              value: value,
              isExpanded: true,
              icon: Icon(Icons.expand_more, color: Colors.grey[600], size: 20),
              style: const TextStyle(fontSize: 13, color: Colors.black),
              items: items,
              // nebeneinander ist wenig Platz: die Auswahl bei Bedarf mit … abkürzen
              selectedItemBuilder: (_) => items
                  .map(
                    (item) => Align(
                      alignment: Alignment.centerLeft,
                      child: DefaultTextStyle.merge(
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        child: item.child,
                      ),
                    ),
                  )
                  .toList(),
              onChanged: onChanged,
            ),
          ),
        ),
      ],
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
              child: _loadingData
                  ? Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          CircularProgressIndicator(color: _accent),
                          const SizedBox(height: 12),
                          Text(
                            'Messdaten werden geladen …',
                            style: TextStyle(
                              fontSize: 13,
                              color: Colors.grey[600],
                            ),
                          ),
                        ],
                      ),
                    )
                  : _matchedSections.isEmpty
                  ? Center(
                      child: Padding(
                        padding: const EdgeInsets.all(24),
                        child: Text(
                          'Für diese Filter gibt es keine Teilmessungen.',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.grey[600],
                          ),
                        ),
                      ),
                    )
                  : displayImage != null
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
    if (_matchedSections.isEmpty) return const SizedBox.shrink();

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
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              _buildPlaybackButton(
                icon: Icons.replay_5,
                tooltip: '5 Sekunden zurück',
                onTap: framesReady ? () => _skipPlayback(-5) : null,
              ),
              const SizedBox(width: 8),
              _buildPlaybackButton(
                icon: _isPlaying ? Icons.pause : Icons.play_arrow,
                tooltip: _isPlaying ? 'Pause' : 'Wiedergabe',
                onTap: framesReady ? _togglePlayback : null,
                primary: true,
              ),
              const SizedBox(width: 8),
              _buildPlaybackButton(
                icon: Icons.stop,
                tooltip: 'Stop',
                onTap: framesReady ? _stopPlayback : null,
              ),
              const SizedBox(width: 8),
              _buildPlaybackButton(
                icon: Icons.forward_5,
                tooltip: '5 Sekunden vor',
                onTap: framesReady ? () => _skipPlayback(5) : null,
              ),
            ],
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              Expanded(
                child: SliderTheme(
                  data: SliderThemeData(
                    activeTrackColor: _accent,
                    inactiveTrackColor: _accentLight.withValues(alpha: 0.5),
                    thumbColor: _accent,
                    thumbShape: const RoundSliderThumbShape(
                      enabledThumbRadius: 6,
                    ),
                    trackHeight: 4,
                    overlayShape: const RoundSliderOverlayShape(
                      overlayRadius: 14,
                    ),
                  ),
                  child: Slider(
                    value: _playbackPosition,
                    onChanged: framesReady ? _seekPlayback : null,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Text(
                '${_formatPlaybackTime(_playbackPosition)} | ${_formatDuration(_totalDuration)}',
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

  Widget _buildPlaybackButton({
    required IconData icon,
    required String tooltip,
    required VoidCallback? onTap,
    bool primary = false,
  }) {
    final size = primary ? 40.0 : 34.0;
    return Tooltip(
      message: tooltip,
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          width: size,
          height: size,
          decoration: BoxDecoration(
            color: onTap == null
                ? Colors.grey[300]
                : primary
                ? _accent
                : _accentLight.withValues(alpha: 0.5),
            borderRadius: BorderRadius.circular(size / 2),
          ),
          child: Icon(
            icon,
            color: primary || onTap == null ? Colors.white : _accent,
            size: primary ? 22 : 18,
          ),
        ),
      ),
    );
  }

  Widget _buildInfoSection() {
    final m = _measurement;
    final infoItems = [
      _InfoItem(
        text: '${m.horseName} | ${m.weight} | ${m.height}',
        svgPath: 'assets/icon/horseIcon.svg',
      ),
      _InfoItem(text: _formatDate(m.date), icon: Icons.calendar_today),
      if (m.sections.any((s) => s.duration != null))
        _InfoItem(
          text:
              'Dauer ${_formatDuration(m.sections.fold(Duration.zero, (sum, s) => sum + (s.duration ?? Duration.zero)))}'
              ' · ${m.sections.length} Teilmessung${m.sections.length == 1 ? '' : 'en'}',
          icon: Icons.timer_outlined,
        ),
      _InfoItem(text: m.rider, icon: Icons.person),
      _InfoItem(text: m.saddleName, svgPath: 'assets/icon/saddleIcon.svg'),
      if (m.notes.isNotEmpty)
        _InfoItem(text: m.notes, icon: Icons.info_outline),
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
            .map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Row(
                  children: [
                    item.svgPath != null
                        ? SvgPicture.asset(
                            item.svgPath!,
                            width: 20,
                            height: 20,
                            colorFilter: ColorFilter.mode(
                              Colors.grey[600]!,
                              BlendMode.srcIn,
                            ),
                          )
                        : Icon(item.icon, size: 20, color: Colors.grey[600]),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        item.text,
                        style: TextStyle(fontSize: 13, color: Colors.grey[800]),
                      ),
                    ),
                  ],
                ),
              ),
            )
            .toList(),
      ),
    );
  }
}

/// Welche Druckstatistik die Heatmap zeigt.
enum _PressureView { average, maximum, pro }

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
