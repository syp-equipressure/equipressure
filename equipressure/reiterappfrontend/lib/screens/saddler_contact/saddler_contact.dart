import 'package:flutter/material.dart';
import '../../widgets/sidenav.dart';

class SaddlerContact extends StatelessWidget {
  const SaddlerContact({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: const SideNav(),
      appBar: AppBar(
        title: const Text('Sattler:innen',
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
        child: Text('ganz viele Kontakte'),
      ),
    );
  }
}
