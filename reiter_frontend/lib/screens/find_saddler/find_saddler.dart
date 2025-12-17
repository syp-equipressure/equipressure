import 'package:flutter/material.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart' show CustomAppBar;
import '../../widgets/sidenav.dart';

class FindSaddler extends StatelessWidget {
  const FindSaddler({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
  drawer: const SideNav(),
  appBar: const CustomAppBar(
    title: 'EquiPressure',
  ),
  body: const Center(
        child: Text('Sattler:in finden'),
      ),
    );
  }
}
