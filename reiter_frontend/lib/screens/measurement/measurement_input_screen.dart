import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/models/person.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/models/measurement.dart';
import 'package:reiterappfrontend/screens/horses/horse_history_measurement.dart';
import 'package:reiterappfrontend/screens/horses/horses_screen.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'package:reiterappfrontend/services/measurement_service.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import 'package:intl/intl.dart';

class MeasurementInputScreen extends StatefulWidget {
  final Person user;
  final Horse horse;
  final Saddle saddle;

  const MeasurementInputScreen({
    super.key,
    required this.user,
    required this.horse,
    required this.saddle,
  });

  @override
  State<MeasurementInputScreen> createState() => _MeasurementInputScreenState();
}

class _MeasurementInputScreenState extends State<MeasurementInputScreen> {
  final TextEditingController _notesController = TextEditingController();
  final MeasurementService _measurementService = MeasurementService();
  
  List<MeasurementSection> sections = [];

  @override
  void initState() {
    super.initState();
    _initializeSections();
  }

  void _initializeSections() {
    sections = [
      MeasurementSection(
        gait: 'Schritt',
        hand: 'Links',
      ),
    ];
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  String _getSectionTitle(MeasurementSection section) {
    final gaitName = section.gait == 'Sonstiges' 
        ? (section.customGait?.isNotEmpty == true ? section.customGait! : 'Sonstiges')
        : section.gait;
    final handName = section.hand == 'Links' ? 'linke' : 'rechte';
    return '$gaitName $handName Hand';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: const CustomAppBar(title: 'EquiPressure'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                IconButton(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.arrow_back),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    'Messung von ${widget.horse.name}',
                    style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),

            // Measurement sections
            ...sections.asMap().entries.map((entry) {
              return Column(
                children: [
                  _buildMeasurementSection(entry.value, entry.key),
                  const SizedBox(height: 16),
                ],
              );
            }),

            // Add measurement button
            SizedBox(
              width: double.infinity,
              height: 48,
              child: OutlinedButton(
                onPressed: _addMeasurementSection,
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: Color(0xFFD4B5F5)),
                  foregroundColor: const Color(0xFF6B4C9A),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                child: const Text(
                  'Weitere Teilmessung hinzufügen',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),

            const SizedBox(height: 32),

            // Notes section
            const Text(
              'Notizen',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _notesController,
              maxLines: 5,
              decoration: InputDecoration(
                hintText: 'Notizen eingeben...',
                hintStyle: TextStyle(color: Colors.grey[400]),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide(color: Colors.grey[300]!),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide(color: Colors.grey[300]!),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: const BorderSide(color: Color(0xFF6B4C9A)),
                ),
                contentPadding: const EdgeInsets.all(16),
              ),
            ),

            const SizedBox(height: 32),

            // End measurement button
            SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton(
                onPressed: _endMeasurement,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFFD4B5F5),
                  foregroundColor: const Color(0xFF6B4C9A),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 0,
                ),
                child: const Text(
                  'Messung beenden',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMeasurementSection(MeasurementSection section, int index) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey[300]!),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          // Header
          InkWell(
            onTap: () {
              setState(() {
                section.isExpanded = !section.isExpanded;
              });
            },
            borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
            child: Container(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      _getSectionTitle(section),
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  if (sections.length > 1)
                    IconButton(
                      icon: const Icon(Icons.delete_outline, size: 20),
                      onPressed: () => _deleteMeasurementSection(index),
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(),
                    ),
                  const SizedBox(width: 8),
                  Icon(
                    section.isExpanded ? Icons.expand_less : Icons.expand_more,
                    color: Colors.grey[600],
                  ),
                ],
              ),
            ),
          ),

          // Expanded content
          if (section.isExpanded) ...[
            Divider(height: 1, color: Colors.grey[300]),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Gangart
                  const Text(
                    'Gangart',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 8),
                  _buildDropdown(
                    value: section.gait,
                    items: ['Schritt', 'Trab', 'Galopp', 'Sonstiges'],
                    onChanged: (value) {
                      setState(() {
                        section.gait = value!;
                        if (value == 'Sonstiges') {
                          section.showCustomGait = true;
                        } else {
                          section.showCustomGait = false;
                          section.customGait = null;
                        }
                      });
                    },
                  ),

                  // Custom gait input
                  if (section.showCustomGait) ...[
                    const SizedBox(height: 12),
                    TextField(
                      decoration: InputDecoration(
                        hintText: 'Name der Gangart',
                        hintStyle: TextStyle(color: Colors.grey[400]),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(12),
                          borderSide: BorderSide(color: Colors.grey[300]!),
                        ),
                        enabledBorder: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(12),
                          borderSide: BorderSide(color: Colors.grey[300]!),
                        ),
                        focusedBorder: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(12),
                          borderSide: const BorderSide(color: Color(0xFF6B4C9A)),
                        ),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      ),
                      onChanged: (value) {
                        setState(() {
                          section.customGait = value;
                        });
                      },
                    ),
                  ],

                  const SizedBox(height: 16),

                  // Hand
                  const Text(
                    'Hand',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 8),
                  _buildDropdown(
                    value: section.hand,
                    items: ['Links', 'Rechts'],
                    onChanged: (value) {
                      setState(() {
                        section.hand = value!;
                      });
                    },
                  ),

                  const SizedBox(height: 24),

                  // Action buttons
                  SizedBox(
                    width: double.infinity,
                    height: 48,
                    child: ElevatedButton(
                      onPressed: () => _startPartialMeasurement(section),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFD4B5F5),
                        foregroundColor: const Color(0xFF6B4C9A),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                        elevation: 0,
                      ),
                      child: const Text(
                        'Teilmessung starten',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildDropdown({
    required String value,
    required List<String> items,
    required ValueChanged<String?> onChanged,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey[300]!),
        borderRadius: BorderRadius.circular(12),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<String>(
          value: value,
          isExpanded: true,
          icon: Icon(Icons.expand_more, color: Colors.grey[600]),
          items: items.map((String item) {
            return DropdownMenuItem<String>(
              value: item,
              child: Text(item),
            );
          }).toList(),
          onChanged: onChanged,
        ),
      ),
    );
  }

  void _addMeasurementSection() {
    setState(() {
      sections.add(
        MeasurementSection(
          gait: 'Schritt',
          hand: 'Links',
        ),
      );
    });
  }

  void _deleteMeasurementSection(int index) {
    setState(() {
      sections.removeAt(index);
    });
  }

  void _startPartialMeasurement(MeasurementSection section) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Starte Teilmessung: ${_getSectionTitle(section)}'),
      ),
    );
  }

  Future<void> _endMeasurement() async {
  // Formatiere Datum für den Titel
  final dateFormat = DateFormat('dd.MM.yyyy');
  final measurementTitle = 'Messung vom ${dateFormat.format(DateTime.now())}';
  
  // Erstelle die Notizen aus den Teilmessungen
  final notes = _notesController.text.isNotEmpty 
      ? _notesController.text 
      : sections.map((s) => _getSectionTitle(s)).join(', ');

  // Erstelle neue Messung
  final newMeasurement = Measurement(
    id: DateTime.now().millisecondsSinceEpoch.toString(),
    horseId: widget.horse.id,
    horseName: widget.horse.name,
    date: DateTime.now(),
    owner: 'Lena Graßauer', 
    rider: widget.user.fullName,
    weight: '${widget.horse.weight}kg',
    height: '${widget.horse.height}m',
    saddleName: widget.saddle.name, 
    notes: notes.isNotEmpty ? notes : measurementTitle,
    images: MeasurementImages(
      normal: 'measurement_normal.png',
      filtered: 'measurement_filtered.png',
      profile: 'measurement_profile.png',
    ),
    sections: sections.map((s) => MeasurementSectionData(
      gait: s.gait == 'Sonstiges' && s.customGait?.isNotEmpty == true
          ? s.customGait!
          : s.gait,
      hand: s.hand,
    )).toList(),
  );

  try {
    // Speichere Messung
    await _measurementService.addMeasurement(newMeasurement);

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Messung erfolgreich gespeichert'),
          duration: Duration(seconds: 2),
        ),
      );

      // Gehe zurück zum NewMeasurementScreen (schließe beide Screens)
      // und öffne einen neuen NewMeasurementScreen ohne vorselektiertes Pferd
      Navigator.pushReplacement(
  context,
  MaterialPageRoute(
    builder: (_) => MeasurementDetailScreen(
      measurement: newMeasurement,
    ),
  ),
);
    }
  } catch (e) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Fehler beim Speichern: $e'),
          duration: const Duration(seconds: 2),
        ),
      );
    }
  }
  }
}

class MeasurementSection {
  String gait;
  String hand;
  bool isExpanded;
  bool showCustomGait;
  String? customGait;

  MeasurementSection({
    required this.gait,
    required this.hand,
    this.isExpanded = true,
    this.showCustomGait = false,
    this.customGait,
  });
}