import 'package:flutter/material.dart';
import '../../widgets/sidenav.dart';

class FindSaddler extends StatelessWidget {
  const FindSaddler({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: const SideNav(),
      appBar: AppBar(
        title: const Text('Sattler:in finden',
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
        child: Text('Sattler:in finden'),
      ),
    );
  }
}
