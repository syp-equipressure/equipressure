import 'dart:io';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import '../../widgets/dashed_border.dart';
import '../../widgets/sidenav.dart';
import 'add_horse_screen.dart';

enum HorsesView { add, list }

class Horse {
  final String name;
  final File? image;
  final String age;
  final String breed;

  Horse({
    required this.name,
    this.image,
    required this.age,
    required this.breed,
  });
}

class HorsesScreen extends StatefulWidget {
  const HorsesScreen({super.key});

  @override
  State<HorsesScreen> createState() => _HorsesScreenState();
}

class _HorsesScreenState extends State<HorsesScreen> {
  HorsesView view = HorsesView.list;
  final List<Horse> horses = [];

  void _addHorse(String name, File? image, String age, String breed) {
    setState(() {
      horses.add(Horse(
        name: name,
        image: image,
        age: age,
        breed: breed,
      ));
      view = HorsesView.list;
    });
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
      body: view == HorsesView.add ? _addView() : _listView(),
      floatingActionButton: horses.isNotEmpty && view == HorsesView.list
          ? FloatingActionButton(
              backgroundColor: Colors.deepPurple[300],
              onPressed: () => setState(() => view = HorsesView.add),
              child: const Icon(Icons.add),
            )
          : null,
    );
  }

  /// =========================
  /// LIST VIEW (Header + Content)
  /// =========================
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

  /// =========================
  /// EMPTY BOX
  /// =========================
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

  /// =========================
  /// GRID
  /// =========================
  Widget _horseGrid() {
    return GridView.builder(
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 16,
        crossAxisSpacing: 16,
        childAspectRatio: 0.75,
      ),
      itemCount: horses.length,
      itemBuilder: (_, i) => _horseCard(horses[i]),
    );
  }

  Widget _horseCard(Horse horse) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            blurRadius: 10,
            offset: const Offset(0, 2),
            color: Colors.black.withOpacity(0.05),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: ClipRRect(
              borderRadius:
                  const BorderRadius.vertical(top: Radius.circular(16)),
              child: horse.image != null
                  ? Image.file(
                      horse.image!,
                      fit: BoxFit.cover,
                      width: double.infinity,
                    )
                  : Container(
                      color: Colors.grey[200],
                      child: Icon(
                        Icons.image_outlined,
                        size: 50,
                        color: Colors.grey[400],
                      ),
                    ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  horse.name,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${horse.age} | ${horse.breed}',
                  style: TextStyle(
                    color: Colors.grey[600],
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }


  Widget _addView() {
    return AddHorseScreen(
      onSave: _addHorse,
      onCancel: () => setState(() => view = HorsesView.list),
    );
  }
}