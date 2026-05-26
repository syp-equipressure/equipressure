import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/horses/add_horse_screen.dart';
import 'package:reiterappfrontend/screens/horses/horse_history.dart';
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

  Future<void> _updateHorse(
    String name,
    File? image,
    String age,
    String breed,
    String birthDate,
    String height,
    String weight,
    String sex,
    String stableCity,      // ← NEU
    String stablePostalCode, // ← NEU
    String stableStreet,     // ← NEU
    String stableHouseNumber, // ← NEU
  ) async {
    final updatedHorse = Horse(
      id: currentHorse.id,
      name: name,
      image: image,
      breed: breed,
      birthDate: birthDate,
      height: height,
      weight: weight,
      sex: sex,
      stableCity: stableCity.isEmpty ? null : stableCity,           // ← NEU
      stablePostalCode: stablePostalCode.isEmpty ? null : stablePostalCode, // ← NEU
      stableStreet: stableStreet.isEmpty ? null : stableStreet,     // ← NEU
      stableHouseNumber: stableHouseNumber.isEmpty ? null : stableHouseNumber, // ← NEU
    );

    await _horseService.updateHorse(updatedHorse);

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
          Navigator.of(context).pop(currentHorse);
        }
      },
      child: Scaffold(
        backgroundColor: Colors.white,
        body: SafeArea(
          child: Column(
            children: [
              Stack(
                children: [
                  currentHorse.image != null
                      ? Image.file(
                          currentHorse.image!,
                          width: double.infinity,
                          height: 350,
                          fit: BoxFit.cover,
                        )
                      : currentHorse.assetImage != null
                          ? Image.asset(
                              currentHorse.assetImage!,
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
                  Positioned(
                    top: 16,
                    right: 16,
                    child: _circleIconButton(
                      icon: Icons.edit,
                      onTap: () async {
                        final navigator = Navigator.of(context);
                        final scaffoldMessenger = ScaffoldMessenger.of(context);

                        final result = await navigator.push(
                          MaterialPageRoute(
                            builder: (_) => AddHorseScreen(
                              horse: currentHorse,
                              onSave: (name, image, age, breed, birthDate, height, weight, sex, stableCity, stablePostalCode, stableStreet, stableHouseNumber) async {
                                final nav = Navigator.of(context);
                                await _updateHorse(name, image, age, breed, birthDate, height, weight, sex, stableCity, stablePostalCode, stableStreet, stableHouseNumber);
                                if (mounted) nav.pop();
                              },
                              onCancel: () => Navigator.pop(context),

                              onDelete: () async {
                                final nav = Navigator.of(context);
                                await _horseService.deleteHorse(currentHorse.id);
                                if (mounted) {
                                  nav.pop();
                                  nav.pop('deleted');
                                }
                              },
                            ),
                          ),
                        );

                        if (result == 'deleted' && mounted) {
                          scaffoldMessenger.showSnackBar(
                            const SnackBar(
                              content: Text('Pferd wurde gelöscht'),
                              duration: Duration(seconds: 2),
                            ),
                          );
                        }
                      },
                    ),
                  ),
                ],
              ),
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
                        
                        // ← NEU: Stalladresse anzeigen
                        _buildInfoRow(
                          'Adresse:',
                          currentHorse.fullStableAddress,
                        ),
                        
                        const SizedBox(height: 40),

                        /// Historie Button
                        Align(
                          alignment: Alignment.centerRight,
                          child: InkWell(
                            onTap: () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (context) => HorseHistoryScreen(horse: currentHorse),
                                ),
                              );
                            },
                            borderRadius: BorderRadius.circular(8),
                            child: Padding(
                              padding: const EdgeInsets.all(8.0),
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
                        ),

                        const SizedBox(height: 32),

                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton(
                            onPressed: () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => NewMeasurementScreen(
                                    horse: currentHorse,
                                  ),
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

  Widget _buildInfoRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
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
            color: Colors.black.withValues(alpha: 0.1),
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