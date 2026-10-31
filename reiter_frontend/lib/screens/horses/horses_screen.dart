import 'package:reiterappfrontend/theme/app_colors.dart';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/horse_service.dart';
import 'package:reiterappfrontend/screens/horses/horse_profil_screen.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import '../../widgets/dashed_border.dart';
import '../../widgets/sidenav.dart';
import 'add_horse_screen.dart';

enum HorsesView { add, list }

class HorsesScreen extends StatefulWidget {
  const HorsesScreen({super.key});

  @override
  State<HorsesScreen> createState() => _HorsesScreenState();
}

class _HorsesScreenState extends State<HorsesScreen> {
  HorsesView view = HorsesView.list;
  List<Horse> horses = [];
  bool isLoading = true;
  final HorseService _horseService = HorseService();

  @override
  void initState() {
    super.initState();
    _loadHorses();
  }

  Future<void> _loadHorses() async {
    setState(() => isLoading = true);

    try {
      final loadedHorses = await _horseService.getHorses();
      setState(() {
        horses = loadedHorses;
        isLoading = false;
      });
    } catch (e) {
      setState(() => isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('Fehler beim Laden: $e')));
      }
    }
  }

  // ✅ AKTUALISIERT: Jetzt mit 12 Parametern (inkl. Stalladresse)
  Future<void> _addHorse(
    String name,
    File? image,
    String age,
    String breed,
    String birthDate,
    String weight,
    String height,
    String sex,
    String stableCity,
    String stablePostalCode,
    String stableStreet,
    String stableHouseNumber,
  ) async {
    final newHorse = Horse(
      id: '',
      name: name.trim(),
      image: image,
      breed: breed.trim(),
      birthDate: birthDate,
      weight: weight,
      height: height,
      sex: sex,
      stableCity: stableCity.trim(),
      stablePostalCode: stablePostalCode.trim(),
      stableStreet: stableStreet.trim().isEmpty ? null : stableStreet.trim(),
      stableHouseNumber: stableHouseNumber.trim().isEmpty
          ? null
          : stableHouseNumber.trim(),
    );

    try {
      var savedHorse = await _horseService.addHorse(newHorse);
      if (image != null) {
        // das Pferd ist schon gespeichert, ein Fehler beim Bild darf es nicht verwerfen
        try {
          savedHorse = await _horseService.uploadImage(savedHorse, image);
        } catch (error) {
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Das Pferd ist gespeichert, das Bild aber nicht: $error')),
            );
          }
        }
      }
      if (!mounted) return;
      setState(() {
        horses = [...horses, savedHorse];
        view = HorsesView.list;
        isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Pferd konnte nicht gespeichert werden: $error'),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.bgPanel,
      drawer: const SideNav(),
      appBar: view == HorsesView.add
          ? null
          : const CustomAppBar(title: 'EquiPressure'),
      body: view == HorsesView.add
          ? _addView()
          : isLoading
          ? const Center(child: CircularProgressIndicator())
          : _listView(),
      floatingActionButton: horses.isNotEmpty && view == HorsesView.list
          ? FloatingActionButton(
              backgroundColor: AppColors.primary,
              onPressed: () => setState(() => view = HorsesView.add),
              child: const Icon(Icons.add),
            )
          : null,
    );
  }

  Widget _listView() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          const Text(
            'Meine Pferde',
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 40),
          Expanded(child: horses.isEmpty ? _emptyBox() : _horseGrid()),
        ],
      ),
    );
  }

  Widget _emptyBox() {
    return Center(
      child: GestureDetector(
        onTap: () => setState(() => view = HorsesView.add),
        child: DashedBorder(
          borderRadius: BorderRadius.circular(20),
          child: Container(
            width: 280,
            height: 280,
            alignment: Alignment.center,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  width: 64,
                  height: 64,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(color: AppColors.textMuted, width: 2),
                  ),
                  child: Icon(Icons.add, size: 32, color: AppColors.textSoft),
                ),
                const SizedBox(height: 20),
                Text(
                  'Füge ein Pferd hinzu um mit\n'
                  'den Messungen starten zu\nkönnen',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: AppColors.textSoft, height: 1.5),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _horseGrid() {
    return GridView.builder(
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 16,
        crossAxisSpacing: 16,
        childAspectRatio: 0.75,
      ),
      itemCount: horses.length,
      itemBuilder: (_, i) => _horseCard(horses[i], i),
    );
  }

  Widget _horseCard(Horse horse, int index) {
    return GestureDetector(
      onTap: () async {
        // Warte auf das Ergebnis vom HorseProfileScreen
        final result = await Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => HorseProfileScreen(horse: horse)),
        );

        // Falls das Pferd aktualisiert wurde, lade die Liste neu
        if (result == 'deleted') {
          await _loadHorses();
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Pferd wurde gelöscht'),
                duration: Duration(seconds: 2),
              ),
            );
          }
        } else if (result != null && result is Horse) {
          await _loadHorses();
        }
      },
      child: Container(
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(20),
          boxShadow: [
            BoxShadow(
              blurRadius: 12,
              offset: const Offset(0, 4),
              color: AppColors.shadow(0.05),
            ),
          ],
        ),
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: ClipRRect(
                borderRadius: BorderRadius.circular(16),
                child: SizedBox(
                  width: double.infinity,
                  child: horse.image != null
                      ? Image.file(horse.image!, fit: BoxFit.cover)
                      : _horseService.imageUrl(horse) != null
                      ? Image.network(
                          _horseService.imageUrl(horse)!,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => Container(color: AppColors.borderSoft),
                        )
                      : horse.assetImage != null
                      ? Image.asset(horse.assetImage!, fit: BoxFit.cover)
                      : Container(
                          color: AppColors.borderSoft,
                          child: Icon(
                            Icons.image_outlined,
                            size: 48,
                            color: AppColors.textMuted,
                          ),
                        ),
                ),
              ),
            ),
            const SizedBox(height: 12),
            // ✅ Name wird bei Überlänge mit ... abgekürzt
            Text(
              horse.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              softWrap: false,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
            const SizedBox(height: 4),
            // ✅ Alter | Rasse wird bei Überlänge mit ... abgekürzt
            Text(
              '${_calculateAge(horse.birthDate)} | ${horse.breed}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              softWrap: false,
              style: TextStyle(fontSize: 12, color: AppColors.textSoft),
            ),
          ],
        ),
      ),
    );
  }

  String _calculateAge(String birthDate) {
    if (birthDate.isEmpty) return '?J';

    try {
      final parts = birthDate.split('.');
      if (parts.length == 3) {
        final day = int.parse(parts[0]);
        final month = int.parse(parts[1]);
        final year = int.parse(parts[2]);

        final birth = DateTime(year, month, day);
        final now = DateTime.now();
        final age = now.year - birth.year;

        return '$age J';
      }
    } catch (e) {
      return '? J';
    }

    return '? J';
  }

  Widget _addView() {
    return AddHorseScreen(
      onSave: _addHorse,
      onCancel: () => setState(() => view = HorsesView.list),
    );
  }
}
