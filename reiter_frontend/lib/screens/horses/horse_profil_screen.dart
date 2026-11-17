import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/screens/horses/add_horse_screen.dart';
import 'package:reiterappfrontend/screens/horses/horse_history.dart';
import 'package:reiterappfrontend/screens/measurement/new_measurement.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/chat_service.dart';
import 'package:reiterappfrontend/services/horse_service.dart';
import 'package:reiterappfrontend/widgets/share_with_saddler_sheet.dart';
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
    final changedHorse = Horse(
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

    final messenger = ScaffoldMessenger.of(context);
    try {
      final updatedHorse = await _horseService.updateHorse(changedHorse);
      if (!mounted) return;
      setState(() {
        currentHorse = updatedHorse;
      });
      messenger.showSnackBar(
        const SnackBar(
          content: Text('Pferd erfolgreich aktualisiert'),
          duration: Duration(seconds: 2),
        ),
      );
    } catch (e) {
      messenger.showSnackBar(
        SnackBar(content: Text(_errorText(e))),
      );
      // das Formular bleibt offen, damit nichts verloren geht
      rethrow;
    }
  }

  static String _errorText(Object error) =>
      error.toString().replaceFirst(RegExp(r'^(Exception|Invalid argument\(s\)): '), '');

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
        backgroundColor: AppColors.bgPage,
        // bleibt immer sichtbar, das Profil scrollt dahinter
        bottomNavigationBar: _buildActions(),
        body: SafeArea(
          child: SingleChildScrollView(
            child: Column(
            children: [
              Stack(
                children: [
                  currentHorse.image != null
                      ? Image.file(
                          currentHorse.image!,
                          width: double.infinity,
                          height: 300,
                          fit: BoxFit.cover,
                        )
                      : _horseService.imageUrl(currentHorse) != null
                          ? Image.network(
                              _horseService.imageUrl(currentHorse)!,
                              width: double.infinity,
                              height: 300,
                              fit: BoxFit.cover,
                              errorBuilder: (_, _, _) => Container(
                                width: double.infinity,
                                height: 300,
                                color: AppColors.border,
                              ),
                            )
                      : currentHorse.assetImage != null
                          ? Image.asset(
                              currentHorse.assetImage!,
                              width: double.infinity,
                              height: 300,
                              fit: BoxFit.cover,
                            )
                          : Container(
                              width: double.infinity,
                              height: 300,
                              color: AppColors.border,
                              child: Icon(
                                Icons.image_outlined,
                                size: 80,
                                color: AppColors.textMuted,
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
                    right: 72,
                    child: _circleIconButton(
                      icon: Icons.send_outlined,
                      onTap: () => shareWithSaddler(
                        context,
                        title: '${currentHorse.name} senden an',
                        share: (contactId) => ChatService().shareHorse(contactId, currentHorse.id),
                      ),
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
                                try {
                                  await _updateHorse(name, image, age, breed, birthDate, height, weight, sex, stableCity, stablePostalCode, stableStreet, stableHouseNumber);
                                } catch (_) {
                                  return; // Fehler wird schon angezeigt, Formular bleibt offen
                                }
                                if (mounted) nav.pop();
                              },
                              onCancel: () => Navigator.pop(context),

                              onDelete: () async {
                                final nav = Navigator.of(context);
                                try {
                                  await _horseService.deleteHorse(currentHorse.id);
                                } catch (e) {
                                  scaffoldMessenger.showSnackBar(
                                    SnackBar(content: Text(_errorText(e))),
                                  );
                                  return;
                                }
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
              Container(
                  width: double.infinity,
                  decoration: const BoxDecoration(
                    color: AppColors.bgPage,
                    borderRadius: BorderRadius.only(
                      topLeft: Radius.circular(24),
                      topRight: Radius.circular(24),
                    ),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 20, 16, 24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Flexible(
                              child: Text(
                                currentHorse.name,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),
                            _buildSexChip(),
                          ],
                        ),
                        const SizedBox(height: 16),
                        Container(
                          decoration: BoxDecoration(
                            color: AppColors.surface,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: AppColors.borderSoft),
                          ),
                          child: Column(
                            children: [
                              _buildInfoRow('Rasse', currentHorse.breed),
                              _buildInfoRow('Geburtsdatum', currentHorse.birthDate),
                              _buildInfoRow(
                                'Stockmaß',
                                currentHorse.height.isEmpty ? '-' : '${currentHorse.height} cm',
                              ),
                              _buildInfoRow(
                                'Gewicht',
                                currentHorse.weight.isEmpty ? '-' : '${currentHorse.weight} kg',
                              ),
                              _buildInfoRow('Adresse', currentHorse.fullStableAddress, isLast: true),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildActions() {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(top: BorderSide(color: AppColors.borderSoft)),
      ),
      child: Row(
        children: [
          Expanded(
            child: OutlinedButton.icon(
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => HorseHistoryScreen(horse: currentHorse)),
              ),
              icon: const Icon(Icons.history),
              label: const Text('Historie'),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: ElevatedButton(
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => NewMeasurementScreen(horse: currentHorse)),
              ),
              child: const Text(
                'Messung starten',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSexChip() {
    final male = currentHorse.sex == 'male';

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: AppColors.purple50,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            male ? Icons.male : Icons.female,
            size: 16,
            color: male ? AppColors.male : AppColors.female,
          ),
          const SizedBox(width: 4),
          Text(
            male ? 'Männlich' : 'Weiblich',
            style: const TextStyle(fontSize: 13, color: AppColors.purple700),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow(String label, String value, {bool isLast = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 13),
      decoration: BoxDecoration(
        border: isLast ? null : const Border(bottom: BorderSide(color: AppColors.borderSoft)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: const TextStyle(fontSize: 14, color: AppColors.textSoft),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w500),
            ),
          ),
        ],
      ),
    );
  }

  Widget _circleIconButton({
    required IconData icon,
    required VoidCallback onTap,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        shape: BoxShape.circle,
        boxShadow: [
          BoxShadow(
            color: AppColors.shadow(0.1),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: IconButton(
        icon: Icon(icon, color: AppColors.textStrong),
        onPressed: onTap,
      ),
    );
  }
}