import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/models/measurement.dart';
import 'package:reiterappfrontend/screens/horses/horse_history_measurement.dart';
import 'package:reiterappfrontend/services/measurement_service.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import 'package:reiterappfrontend/widgets/custom_filter_chip.dart';
import 'package:reiterappfrontend/widgets/dropdown_filter.dart';
import 'package:reiterappfrontend/widgets/measurement_card.dart';


class HorseHistoryScreen extends StatefulWidget {
  final Horse horse;

  const HorseHistoryScreen({
    super.key,
    required this.horse,
  });

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
      final loadedMeasurements = await _measurementService.getMeasurements();

      final horseMeasurements = loadedMeasurements
          .where((m) => m.horseName.toLowerCase() == widget.horse.name.toLowerCase())
          .toList();

      horseMeasurements.sort((a, b) => b.date.compareTo(a.date));

      // Extrahiere unique Reiter und Sättel
      final riders = horseMeasurements.map((m) => m.rider).toSet().toList();
      final saddles = horseMeasurements
          .map((m) => m.saddleName)
          .toSet()
          .toList();

      setState(() {
        allMeasurements = horseMeasurements;
        filteredMeasurements = horseMeasurements;
        availableRiders = riders;
        availableSaddles = saddles;
        isLoading = false;
      });
    } catch (e) {
      setState(() => isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler beim Laden: $e')),
        );
      }
    }
  }

  void _applyFilters() {
    setState(() {
      filteredMeasurements = allMeasurements.where((m) {
        bool matchesRider = selectedRider == null || m.rider == selectedRider;
        bool matchesSaddle = selectedSaddle == null ||
            m.saddleName == selectedSaddle;
        return matchesRider && matchesSaddle;
      }).toList();
    });
  }

  void _showDeleteConfirmation(Measurement measurement) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Messung löschen'),
        content: Text('Möchtest du die Messung "${measurement.notes}" wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.'),
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
            style: TextButton.styleFrom(
              foregroundColor: Colors.red,
            ),
            child: const Text('Löschen'),
          ),
        ],
      ),
    );
  }

  Future<void> _deleteMeasurement(Measurement measurement) async {
    try {
      await _measurementService.deleteMeasurement(measurement.id);
      
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
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[50],
      appBar: const CustomAppBar(title: 'EquiPressure'),
      body: Column(
        children: [
          // Header mit Zurück-Button und Titel
          Container(
            color: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                IconButton(
                  icon: const Icon(Icons.arrow_back, color: Colors.black),
                  onPressed: () => Navigator.pop(context),
                ),
                const SizedBox(width: 8),
                Text(
                  '${widget.horse.name}\'s Historie',
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const Spacer(),
                Text(
                  '${filteredMeasurements.length} Messungen',
                  style: TextStyle(
                    fontSize: 14,
                    color: Colors.grey[600],
                  ),
                ),
              ],
            ),
          ),

          // Filter Chips
          Container(
            color: Colors.white,
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Row(
              children: [
                const CustomFilterChip(label: 'Zeitraum'),
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

          const SizedBox(height: 8),

          // Messungen Liste
          Expanded(
            child: isLoading
                ? const Center(child: CircularProgressIndicator())
                : filteredMeasurements.isEmpty
                    ? Center(
                        child: Text(
                          'Keine Messungen vorhanden',
                          style: TextStyle(
                            fontSize: 16,
                            color: Colors.grey[600],
                          ),
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