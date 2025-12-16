import 'package:flutter/material.dart';
import '../../widgets/sidenav.dart';

class NewMeasurement extends StatelessWidget {
  const NewMeasurement({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: const SideNav(),
      appBar: AppBar(
        title: const Text('Neue Messung',
          style: TextStyle(
            fontWeight: FontWeight.bold,
          ),
        ),
        centerTitle: true,
        leading: Builder(
          builder: (context) => IconButton(
            icon: const Icon(Icons.menu),
            onPressed: () => Scaffold.of(context).openDrawer(),
          ),
        ),
      ),
      body: const Center(
        child: Text('messen...'),
      ),
    );
  }
}
