import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/models/measurement.dart';
import 'package:reiterappfrontend/screens/horses/horse_history_measurement.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/services/measurement_service.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import 'package:reiterappfrontend/widgets/date_range_filter_chip.dart';
import 'package:reiterappfrontend/widgets/dropdown_filter.dart';
import 'package:reiterappfrontend/widgets/measurement_card.dart';
import 'package:reiterappfrontend/widgets/sidenav.dart';

class HorseHistoryScreen extends StatefulWidget {
  final Horse horse;

  const HorseHistoryScreen({super.key, required this.horse});

  @override
  State<HorseHistoryScreen> createState() => _HorseHistoryScreenState();
}

class _HorseHistoryScreenState extends State<HorseHistoryScreen> {
  final MeasurementService _measurementService = MeasurementService();
  List<Measurement> allMeasurements = [];
  List<Measurement> filteredMeasurements = [];
  bool isLoading = true;

  String? selectedRider;
  String? selectedSaddle;
  DateTimeRange? selectedPeriod;
  List<String> availableRiders = [];
  List<String> availableSaddles = [];

  @override
  void initState() {
    super.initState();
    _loadMeasurements();
  }

  Future<void> _loadMeasurements() async {
    setState(() => isLoading = true);

    try {
      final horseMeasurements = await _measurementService
          .getMeasurementsForHorse(widget.horse.id);

      horseMeasurements.sort((a, b) => b.date.compareTo(a.date));

      // Extrahiere unique Reiter und Sättel
      final riders = horseMeasurements.map((m) => m.rider).toSet().toList();
      final saddles = horseMeasurements
          .map((m) => m.saddleName)
          .toSet()
          .toList();

      // der Screen kann während des Ladens schon verlassen worden sein
      if (!mounted) return;
      setState(() {
        allMeasurements = horseMeasurements;
        filteredMeasurements = horseMeasurements;
        availableRiders = riders;
        availableSaddles = saddles;
        isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => isLoading = false);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Fehler beim Laden: $e')));
    }
  }

  void _applyFilters() {
    setState(() {
      filteredMeasurements = allMeasurements.where((m) {
        bool matchesRider = selectedRider == null || m.rider == selectedRider;
        bool matchesSaddle =
            selectedSaddle == null || m.saddleName == selectedSaddle;
        return matchesRider && matchesSaddle && _isInPeriod(m.date);
      }).toList();
    });
  }

  /// Von und Bis zählen jeweils mit, verglichen wird nur der Tag.
  bool _isInPeriod(DateTime date) {
    final period = selectedPeriod;
    if (period == null) return true;
    final day = DateTime(date.year, date.month, date.day);
    final start = DateTime(
      period.start.year,
      period.start.month,
      period.start.day,
    );
    final end = DateTime(period.end.year, period.end.month, period.end.day);
    return !day.isBefore(start) && !day.isAfter(end);
  }

  /// Frühester auswählbarer Tag: die älteste Messung, mindestens aber ein Jahr zurück.
  DateTime get _firstSelectableDate {
    final oneYearAgo = DateTime.now().subtract(const Duration(days: 365));
    if (allMeasurements.isEmpty) return oneYearAgo;
    final oldest = allMeasurements
        .map((m) => m.date)
        .reduce((a, b) => a.isBefore(b) ? a : b);
    return oldest.isBefore(oneYearAgo) ? oldest : oneYearAgo;
  }

  void _showDeleteConfirmation(Measurement measurement) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Messung löschen'),
        content: Text(
          'Möchtest du die Messung "${measurement.notes}" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () async {
              Navigator.pop(context); // Dialog schließen
              await _deleteMeasurement(measurement);
            },
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            child: const Text('Löschen'),
          ),
        ],
      ),
    );
  }

  Future<void> _deleteMeasurement(Measurement measurement) async {
    try {
      await _measurementService.deleteMeasurement(measurement.id);
      if (!mounted) return;

      // Entferne aus der lokalen Liste
      setState(() {
        allMeasurements.removeWhere((m) => m.id == measurement.id);
      });

      // Filter neu anwenden
      _applyFilters();

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Messung wurde gelöscht'),
            duration: Duration(seconds: 2),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Fehler beim Löschen: $e'),
            duration: Duration(seconds: 2),
          ),
        );
      }
    }
  }

  void _navigateToMeasurementDetail(Measurement measurement) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => MeasurementDetailScreen(
          measurement: measurement,
          horse: widget.horse,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.bgPanel,
      appBar: const CustomAppBar(title: 'EquiPressure'),
      drawer: const SideNav(),
      body: Column(
        children: [
          // Header mit Zurück-Button und Titel
          Container(
            color: AppColors.surface,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                IconButton(
                  icon: const Icon(Icons.arrow_back, color: AppColors.textStrong),
                  onPressed: () {
                    Navigator.of(context).pushAndRemoveUntil(
                      MaterialPageRoute(
                        builder: (context) =>
                            const HorsesScreen(), // Ihr Screen Name
                      ),
                      (route) => false,
                    );
                  },
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    '${widget.horse.name}\'s Historie',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  '${filteredMeasurements.length} Messungen',
                  style: TextStyle(fontSize: 14, color: AppColors.textSoft),
                ),
              ],
            ),
          ),

          // Filter Chips
          Container(
            color: AppColors.surface,
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            // scrollbar, weil ein gewählter Zeitraum auf schmalen Geräten sonst überläuft
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  DateRangeFilterChip(
                    label: 'Zeitraum',
                    selectedRange: selectedPeriod,
                    firstDate: _firstSelectableDate,
                    lastDate: DateTime.now(),
                    onSelected: (range) {
                      setState(() {
                        selectedPeriod = range;
                      });
                      _applyFilters();
                    },
                  ),
                  const SizedBox(width: 8),
                  DropdownFilterChip(
                    label: 'Reiter:in',
                    items: availableRiders,
                    selectedItem: selectedRider,
                    onSelected: (value) {
                      setState(() {
                        selectedRider = value;
                      });
                      _applyFilters();
                    },
                  ),
                  const SizedBox(width: 8),
                  DropdownFilterChip(
                    label: 'Sattel',
                    items: availableSaddles,
                    selectedItem: selectedSaddle,
                    onSelected: (value) {
                      setState(() {
                        selectedSaddle = value;
                      });
                      _applyFilters();
                    },
                  ),
                ],
              ),
            ),
          ),

          const SizedBox(height: 8),

          // Messungen Liste
          Expanded(
            child: isLoading
                ? const Center(child: CircularProgressIndicator())
                : filteredMeasurements.isEmpty
                ? Center(
                    child: Text(
                      'Keine Messungen vorhanden',
                      style: TextStyle(fontSize: 16, color: AppColors.textSoft),
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: filteredMeasurements.length,
                    itemBuilder: (context, index) {
                      final measurement = filteredMeasurements[index];
                      return MeasurementCard(
                        measurement: measurement,
                        onTap: () => _navigateToMeasurementDetail(measurement),
                        onDelete: () => _showDeleteConfirmation(measurement),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
