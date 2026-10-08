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
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler beim Laden: $e')),
        );
      }
    }
  }

  // ✅ AKTUALISIERT: Jetzt mit 12 Parametern (inkl. Stalladresse)
  void _addHorse(
    String name,
    File? image,
    String age,
    String breed,
    String birthDate,
    String weight,
    String height,
    String sex,
    String stableCity,       // ← NEU
    String stablePostalCode, // ← NEU
    String stableStreet,     // ← NEU
    String stableHouseNumber,// ← NEU
  ) async {
    final newHorse = Horse(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      name: name,
      image: image,
      breed: breed,
      birthDate: birthDate,
      weight: weight,
      height: height,
      sex: sex,
      stableCity: stableCity.isEmpty ? null : stableCity,
      stablePostalCode: stablePostalCode.isEmpty ? null : stablePostalCode,
      stableStreet: stableStreet.isEmpty ? null : stableStreet,
      stableHouseNumber: stableHouseNumber.isEmpty ? null : stableHouseNumber,
    );

    await _horseService.addHorse(newHorse);
    
    setState(() {
      view = HorsesView.list;
    });
    
    // Lade die Pferde neu, um die Liste zu aktualisieren
    await _loadHorses();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[50],
      drawer: const SideNav(),
      appBar: view == HorsesView.add
          ? null
          : const CustomAppBar(
              title: 'EquiPressure',
            ),
      body: view == HorsesView.add 
          ? _addView() 
          : isLoading 
              ? const Center(child: CircularProgressIndicator())
              : _listView(),
      floatingActionButton: horses.isNotEmpty && view == HorsesView.list
          ? FloatingActionButton(
              backgroundColor: const Color.fromARGB(255, 178, 149, 230),
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
          Expanded(
            child: horses.isEmpty ? _emptyBox() : _horseGrid(),
          ),
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
                    border: Border.all(color: Colors.grey[400]!, width: 2),
                  ),
                  child: Icon(Icons.add, size: 32, color: Colors.grey[600]),
                ),
                const SizedBox(height: 20),
                Text(
                  'Füge ein Pferd hinzu um mit\n'
                  'den Messungen starten zu\nkönnen',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: Colors.grey[600],
                    height: 1.5,
                  ),
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
          MaterialPageRoute(
            builder: (_) => HorseProfileScreen(horse: horse),
          ),
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
        }
        else if (result != null && result is Horse) {
          await _loadHorses();
        }
      },
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          boxShadow: [
            BoxShadow(
              blurRadius: 12,
              offset: const Offset(0, 4),
              color: Colors.black.withValues(alpha: 0.05),
            ),
          ],
        ),
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: AspectRatio(
                aspectRatio: 1,
                child: horse.image != null
                    ? Image.file(horse.image!, fit: BoxFit.cover)
                    : horse.assetImage != null
                        ? Image.asset(horse.assetImage!, fit: BoxFit.cover)
                        : Container(
                            color: Colors.grey[200],
                            child: Icon(Icons.image_outlined,
                                size: 48, color: Colors.grey[400]),
                          ),
              ),
            ),
            const SizedBox(height: 12),
            Text(
              horse.name,
              style: const TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 16,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              '${_calculateAge(horse.birthDate)} | ${horse.breed}',
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey[600],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _calculateAge(String birthDate) {
    if (birthDate.isEmpty) return '?yo';
    
    try {
      final parts = birthDate.split('.');
      if (parts.length == 3) {
        final day = int.parse(parts[0]);
        final month = int.parse(parts[1]);
        final year = int.parse(parts[2]);
        
        final birth = DateTime(year, month, day);
        final now = DateTime.now();
        final age = now.year - birth.year;
        
        return '${age}yo';
      }
    } catch (e) {
      return '?yo';
    }
    
    return '?yo';
  }

  Widget _addView() {
    return AddHorseScreen(
      onSave: _addHorse,
      onCancel: () => setState(() => view = HorsesView.list),
    );
  }
}