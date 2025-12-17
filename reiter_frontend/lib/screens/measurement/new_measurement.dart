import 'package:flutter/material.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import '../../widgets/sidenav.dart';

class NewMeasurement extends StatelessWidget {
  const NewMeasurement({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
  drawer: const SideNav(),
  appBar: const CustomAppBar(
    title: 'EquiPressure',
  ),
      body: const Center(
        child: Text('messen... '),
      ),
    );
  }
}
