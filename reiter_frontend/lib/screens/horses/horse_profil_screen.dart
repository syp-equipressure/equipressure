import 'dart:io';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'horses_screen.dart';

class HorseProfileScreen extends StatelessWidget {
  final Horse horse;

  const HorseProfileScreen({
    super.key,
    required this.horse,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Column(
          children: [
            /// =========================
            /// HEADER MIT BILD
            /// =========================
            Stack(
              children: [
                horse.image != null
                    ? Image.file(
                        horse.image!,
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
                    onTap: () => Navigator.pop(context),
                  ),
                ),

                /// Bearbeiten Button
                Positioned(
                  top: 16,
                  right: 16,
                  child: _circleIconButton(
                    icon: Icons.edit,
                    onTap: () {
                      // TODO: Pferd bearbeiten
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
                            horse.name,
                            style: const TextStyle(
                              fontSize: 32,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(width: 8),
                          Icon(
                            horse.sex == 'male'
                                ? Icons.male
                                : Icons.female,
                            size: 28,
                            color: Colors.grey[600],
                          ),
                        ],
                      ),

                      const SizedBox(height: 32),

                      _buildInfoRow('Rasse:', horse.breed),
                      const SizedBox(height: 20),

                      _buildInfoRow('Geburtsdatum:', horse.birthDate),
                      const SizedBox(height: 20),

                      _buildInfoRow(
                        'Stockmaß:',
                        horse.height.isEmpty
                            ? '-'
                            : '${horse.height} cm',
                      ),
                      const SizedBox(height: 20),

                      _buildInfoRow(
                        'Gewicht:',
                        horse.weight.isEmpty
                            ? '-'
                            : '${horse.weight} kg',
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
                                '${horse.name}\'s Historie',
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
