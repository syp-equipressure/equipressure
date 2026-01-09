import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/models/measurement.dart';
import 'package:reiterappfrontend/services/measurement_service.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import 'package:intl/intl.dart';

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
          .map((m) => 'Prestige Dressursattel ${m.pressureLevel}')
          .toSet()
          .toList();

      setState(() {
        allMeasurements = horseMeasurements;
        filteredMeasurements = horseMeasurements;
        availableRiders = riders;
        availableSaddles = saddles;
        isLoading = false;
      });

      print('Geladene Messungen für ${widget.horse.name}: ${allMeasurements.length}');
    } catch (e) {
      print('Fehler beim Laden: $e');
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
            'Prestige Dressursattel ${m.pressureLevel}' == selectedSaddle;
        return matchesRider && matchesSaddle;
      }).toList();
    });
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
                _buildFilterChip('Zeitraum'),
                const SizedBox(width: 8),
                _buildRiderFilterChip(),
                const SizedBox(width: 8),
                _buildSaddleFilterChip(),
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
                          return _buildMeasurementCard(filteredMeasurements[index]);
                        },
                      ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(String label) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: Colors.grey[300]!),
      ),
      child: Text(
        label,
        style: const TextStyle(
          fontSize: 14,
          color: Colors.black87,
        ),
      ),
    );
  }

  Widget _buildRiderFilterChip() {
    return PopupMenuButton<String>(
      onSelected: (value) {
        setState(() {
          selectedRider = value == 'Alle' ? null : value;
        });
        _applyFilters();
      },
      itemBuilder: (context) {
        return [
          const PopupMenuItem(
            value: 'Alle',
            child: Row(
              children: [
                Icon(Icons.check_box_outline_blank, size: 20),
                SizedBox(width: 8),
                Text('Alle'),
              ],
            ),
          ),
          ...availableRiders.map((rider) {
            final isSelected = selectedRider == rider;
            return PopupMenuItem(
              value: rider,
              child: Row(
                children: [
                  Icon(
                    isSelected ? Icons.check_box : Icons.check_box_outline_blank,
                    size: 20,
                  ),
                  const SizedBox(width: 8),
                  Text(rider),
                ],
              ),
            );
          }).toList(),
        ];
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: selectedRider != null ? Colors.grey[200] : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: selectedRider != null ? Colors.grey[400]! : Colors.grey[300]!,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'Reiter:in',
              style: TextStyle(
                fontSize: 14,
                color: Colors.black87,
                fontWeight: selectedRider != null ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            const SizedBox(width: 4),
            Icon(
              Icons.arrow_drop_down,
              size: 20,
              color: Colors.grey[700],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSaddleFilterChip() {
    return PopupMenuButton<String>(
      onSelected: (value) {
        setState(() {
          selectedSaddle = value == 'Alle' ? null : value;
        });
        _applyFilters();
      },
      itemBuilder: (context) {
        return [
          const PopupMenuItem(
            value: 'Alle',
            child: Row(
              children: [
                Icon(Icons.check_box_outline_blank, size: 20),
                SizedBox(width: 8),
                Text('Alle'),
              ],
            ),
          ),
          ...availableSaddles.map((saddle) {
            final isSelected = selectedSaddle == saddle;
            return PopupMenuItem(
              value: saddle,
              child: Row(
                children: [
                  Icon(
                    isSelected ? Icons.check_box : Icons.check_box_outline_blank,
                    size: 20,
                  ),
                  const SizedBox(width: 8),
                  Text(saddle),
                ],
              ),
            );
          }).toList(),
        ];
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: selectedSaddle != null ? Colors.grey[200] : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: selectedSaddle != null ? Colors.grey[400]! : Colors.grey[300]!,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'Sattel',
              style: TextStyle(
                fontSize: 14,
                color: Colors.black87,
                fontWeight: selectedSaddle != null ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            const SizedBox(width: 4),
            Icon(
              Icons.arrow_drop_down,
              size: 20,
              color: Colors.grey[700],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMeasurementCard(Measurement measurement) {
    final dateFormat = DateFormat('dd.MM.yyyy');

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () {
            // TODO: Navigation zur Messungsdetail-Seite
            print('Messung angeklickt: ${measurement.id}');
          },
          borderRadius: BorderRadius.circular(12),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header mit Titel und Delete Button
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        measurement.notes,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    IconButton(
                      icon: Icon(Icons.delete_outline, color: Colors.grey[600], size: 20),
                      onPressed: () {
                        // TODO: Messung löschen
                        print('Löschen: ${measurement.id}');
                      },
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(),
                    ),
                  ],
                ),

                const SizedBox(height: 12),

                // Details
                _buildDetailRow(Icons.access_time, dateFormat.format(measurement.date)),
                const SizedBox(height: 8),
                _buildDetailRow(Icons.person_outline, measurement.rider),
                const SizedBox(height: 8),
                _buildDetailRow(
                  Icons.analytics_outlined,
                  'Prestige Dressursattel ${measurement.pressureLevel}',
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildDetailRow(IconData icon, String text) {
    return Row(
      children: [
        Icon(icon, size: 16, color: Colors.grey[600]),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            text,
            style: TextStyle(
              fontSize: 14,
              color: Colors.grey[700],
            ),
          ),
        ),
      ],
    );
  }
}