import 'dart:io';

import 'package:flutter/material.dart';
import '../../widgets/sidenav.dart';
import 'empty_horse_screen.dart';
import 'add_horse_screen.dart';

enum HorsesView { empty, add, list }

class HorsesScreen extends StatefulWidget {
  const HorsesScreen({super.key});

  @override
  State<HorsesScreen> createState() => _HorsesScreenState();
}

class _HorsesScreenState extends State<HorsesScreen> {
  HorsesView view = HorsesView.empty;

  final List<String> horses = [];

  void _addHorse(String name, File? image) {
    setState(() {
      horses.add(name); // optional: Du könntest auch das Bild speichern
      view = HorsesView.list;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: const SideNav(),
      appBar: AppBar(
        title: const Text(
          'Meine Pferde',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        centerTitle: true,
        leading: Builder(
          builder: (context) => IconButton(
            icon: const Icon(Icons.menu),
            onPressed: () => Scaffold.of(context).openDrawer(),
          ),
        ),
      ),
      body: _buildBody(),
      floatingActionButton: view == HorsesView.list
          ? FloatingActionButton(
        onPressed: () => setState(() => view = HorsesView.add),
        child: const Icon(Icons.add),
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
    return GridView.builder(
      padding: const EdgeInsets.all(16),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 0.8,
      ),
      itemCount: horses.length,
      itemBuilder: (context, index) {
        return Card(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            children: [
              Expanded(
                child: Container(
                  decoration: BoxDecoration(
                    color: Colors.grey[300],
                    borderRadius: const BorderRadius.vertical(
                      top: Radius.circular(16),
                    ),
                  ),
                  child: const Icon(Icons.image, size: 50),
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(8),
                child: Text(
                  horses[index],
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
