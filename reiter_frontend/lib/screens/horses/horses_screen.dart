import 'dart:io';

import 'package:flutter/material.dart';
import '../../widgets/sidenav.dart';
import 'empty_horse_screen.dart';
import 'add_horse_screen.dart';

enum HorsesView { empty, add, list }

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
  HorsesView view = HorsesView.empty;

  final List<Horse> horses = [];

  void _addHorse(String name, File? image, String age, String breed) {
    setState(() {
      horses.add(Horse(name: name, image: image, age: age, breed: breed));
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
          : AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        title: const Text(
          'EquiPressure',
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: Colors.black,
          ),
        ),
        centerTitle: true,
        leading: Builder(
          builder: (context) => IconButton(
            icon: const Icon(Icons.menu, color: Colors.black),
            onPressed: () => Scaffold.of(context).openDrawer(),
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.account_circle_outlined, color: Colors.deepPurple[300]),
            onPressed: () {},
          ),
        ],
      ),
      body: _buildBody(),
      floatingActionButton: view == HorsesView.list
          ? FloatingActionButton(
        backgroundColor: Colors.deepPurple[300],
        onPressed: () => setState(() => view = HorsesView.add),
        child: const Icon(Icons.add, color: Colors.white),
      )
          : null,
    );
  }

  Widget _buildBody() {
    if (horses.isEmpty && view != HorsesView.add) {
      view = HorsesView.empty;
    }

    switch (view) {
      case HorsesView.empty:
        return HorseEmptyScreen(
          onAddPressed: () => setState(() => view = HorsesView.add),
        );

      case HorsesView.add:
        return AddHorseScreen(
          onSave: _addHorse,
          onCancel: () =>
              setState(() => view = horses.isEmpty ? HorsesView.empty : HorsesView.list),
        );

      case HorsesView.list:
        return _horseGrid();
    }
  }

  Widget _horseGrid() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.only(bottom: 16),
            child: Center(
              child: Text(
                'Meine Pferde',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          Expanded(
            child: GridView.builder(
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 16,
                crossAxisSpacing: 16,
                childAspectRatio: 0.75,
              ),
              itemCount: horses.length,
              itemBuilder: (context, index) {
                return _buildHorseCard(horses[index]);
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHorseCard(Horse horse) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                color: Colors.grey[200],
                borderRadius: const BorderRadius.vertical(
                  top: Radius.circular(16),
                ),
              ),
              child: ClipRRect(
                borderRadius: const BorderRadius.vertical(
                  top: Radius.circular(16),
                ),
                child: horse.image != null
                    ? Image.file(
                  horse.image!,
                  width: double.infinity,
                  fit: BoxFit.cover,
                )
                    : Center(
                  child: Icon(
                    Icons.image_outlined,
                    size: 50,
                    color: Colors.grey[400],
                  ),
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
}