import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/horses/add_horse_screen.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/horse_service.dart';
import 'dart:io';

class HorseProfileScreen extends StatefulWidget {
  final Horse horse;

  const HorseProfileScreen({
    super.key,
    required this.horse,
  });

  @override
  State<HorseProfileScreen> createState() => _HorseProfileScreenState();
}

class _HorseProfileScreenState extends State<HorseProfileScreen> {
  late Horse currentHorse;
  final HorseService _horseService = HorseService();

  @override
  void initState() {
    super.initState();
    currentHorse = widget.horse;
  }

  Future<void> _updateHorse(String name, File? image, String age, String breed, String birthDate, String height, String weight, String sex) async {
    // Erstelle aktualisiertes Horse-Objekt
    final updatedHorse = Horse(
      id: currentHorse.id,
      name: name,
      image: image,
      breed: breed,
      birthDate: birthDate,
      height: height,
      weight: weight,
      sex: sex,
    );

    // Speichere im Service
    await _horseService.updateHorse(updatedHorse);

    // Aktualisiere den lokalen State
    setState(() {
      currentHorse = updatedHorse;
    });

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Pferd erfolgreich aktualisiert'),
          duration: Duration(seconds: 2),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) async {
        if (!didPop) {
          // Gebe das aktualisierte Pferd zurück
          Navigator.of(context).pop(currentHorse);
        }
      },
      child: Scaffold(
        backgroundColor: Colors.white,
        body: SafeArea(
          child: Column(
            children: [
              /// =========================
              /// HEADER MIT BILD
              /// =========================
              Stack(
                children: [
                  currentHorse.image != null
                      ? Image.file(
                          currentHorse.image!,
                          width: double.infinity,
                          height: 350,
                          fit: BoxFit.cover,
                        )
                      : Container(
                          width: double.infinity,
                          height: 350,
                          color: Colors.grey[300],
                          child: Icon(
                            Icons.image_outlined,
                            size: 80,
                            color: Colors.grey[500],
                          ),
                        ),

                  /// Zurück Button
                  Positioned(
                    top: 16,
                    left: 16,
                    child: _circleIconButton(
                      icon: Icons.arrow_back,
                      onTap: () {
                        Navigator.of(context).pop(currentHorse);
                      },
                    ),
                  ),

                  /// Bearbeiten Button
                  Positioned(
                    top: 16,
                    right: 16,
                    child: _circleIconButton(
                      icon: Icons.edit,
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => AddHorseScreen(
                              horse: currentHorse,
                              onSave: (name, image, age, breed, birthDate, height, weight, sex) async {
                                await _updateHorse(name, image, age, breed, birthDate, height, weight, sex);
                                Navigator.pop(context);
                              },
                              onCancel: () => Navigator.pop(context),
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),

              /// =========================
              /// DETAILS
              /// =========================
              Expanded(
                child: Container(
                  width: double.infinity,
                  decoration: const BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.only(
                      topLeft: Radius.circular(24),
                      topRight: Radius.circular(24),
                    ),
                  ),
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        /// Name + Geschlecht
                        Row(
                          children: [
                            Text(
                              currentHorse.name,
                              style: const TextStyle(
                                fontSize: 32,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Icon(
                              currentHorse.sex == 'male'
                                  ? Icons.male
                                  : Icons.female,
                              size: 28,
                              color: Colors.grey[600],
                            ),
                          ],
                        ),

                        const SizedBox(height: 32),

                        _buildInfoRow('Rasse:', currentHorse.breed),
                        const SizedBox(height: 20),

                        _buildInfoRow('Geburtsdatum:', currentHorse.birthDate),
                        const SizedBox(height: 20),

                        _buildInfoRow(
                          'Stockmaß:',
                          currentHorse.height.isEmpty
                              ? '-'
                              : '${currentHorse.height} cm',
                        ),
                        const SizedBox(height: 20),

                        _buildInfoRow(
                          'Gewicht:',
                          currentHorse.weight.isEmpty
                              ? '-'
                              : '${currentHorse.weight} kg',
                        ),
                        const SizedBox(height: 20),

                        _buildInfoRow('Besitzer:in:', 'Lena Graßauer'),

                        const SizedBox(height: 40),

                        /// Historie
                        Align(
                          alignment: Alignment.centerRight,
                          child: GestureDetector(
                            onTap: () {
                              // TODO: Historie Screen
                            },
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  '${currentHorse.name}\'s Historie',
                                  style: TextStyle(
                                    fontSize: 16,
                                    color: Colors.grey[800],
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Icon(
                                  Icons.arrow_forward,
                                  size: 20,
                                  color: Colors.grey[800],
                                ),
                              ],
                            ),
                          ),
                        ),

                        const SizedBox(height: 32),

                        /// =========================
                        /// MESSUNG STARTEN
                        /// =========================
                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton(
                            onPressed: () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => const NewMeasurement(),
                                ),
                              );
                            },
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFFD4B5F5),
                              foregroundColor: const Color(0xFF6B4C9A),
                              padding: const EdgeInsets.symmetric(vertical: 18),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(16),
                              ),
                              elevation: 0,
                            ),
                            child: const Text(
                              'Messung starten',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// =========================
  /// HELPER WIDGETS
  /// =========================

  Widget _buildInfoRow(String label, String value) {
    return Row(
      children: [
        SizedBox(
          width: 140,
          child: Text(
            label,
            style: TextStyle(
              fontSize: 16,
              color: Colors.grey[800],
            ),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
      ],
    );
  }

  Widget _circleIconButton({
    required IconData icon,
    required VoidCallback onTap,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        shape: BoxShape.circle,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: IconButton(
        icon: Icon(icon, color: Colors.black),
        onPressed: onTap,
      ),
    );
  }
}