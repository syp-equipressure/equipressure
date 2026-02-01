import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/models/person.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/screens/measurement/new_person.dart';
import 'package:reiterappfrontend/screens/measurement/new_saddle.dart';
import 'package:reiterappfrontend/services/person_service.dart';
import 'package:reiterappfrontend/services/saddle_service.dart';
import 'package:reiterappfrontend/services/horse_service.dart';
import 'package:reiterappfrontend/widgets/app_bar.dart';
import 'package:reiterappfrontend/widgets/measurement_dropdown.dart';
import 'package:reiterappfrontend/widgets/measurement_infobox.dart';
import 'package:reiterappfrontend/widgets/sidenav.dart';
import 'package:reiterappfrontend/widgets/info_button.dart'; // ← NEU: InfoButton importieren
import 'package:reiterappfrontend/screens/measurement/measurement_input_screen.dart';

class NewMeasurementScreen extends StatefulWidget {
  final Horse? horse;

  const NewMeasurementScreen({
    super.key,
    this.horse,
  });

  @override
  State<NewMeasurementScreen> createState() => _NewMeasurementScreenState();
}

class _NewMeasurementScreenState extends State<NewMeasurementScreen> {
  final PersonService _personService = PersonService();
  final SaddleService _saddleService = SaddleService();
  final HorseService _horseService = HorseService();

  List<Person> persons = [];
  List<Saddle> saddles = [];
  List<Horse> horses = [];
  bool isLoading = true;

  Person? selectedPerson;
  Horse? selectedHorse;
  Saddle? selectedSaddle;

  @override
  void initState() {
    super.initState();
    selectedHorse = widget.horse;
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => isLoading = true);

    try {
      final loadedPersons = await _personService.getPersons();
      final loadedSaddles = await _saddleService.getSaddles();
      final loadedHorses = await _horseService.getHorses();

      setState(() {
        persons = loadedPersons;
        saddles = loadedSaddles;
        horses = loadedHorses;
        
        // Wichtig: Setze selectedHorse nochmal nach dem Laden
        // damit das Dropdown das richtige Objekt aus der horses-Liste findet
        if (widget.horse != null) {
          selectedHorse = horses.firstWhere(
            (h) => h.id == widget.horse!.id,
            orElse: () => widget.horse!,
          );
        }
        
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

  void _showAddRiderDialog() {
   
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NewPersonScreen()
      ),
    ).then((newPerson) async {
      await _loadData();
      
      if (newPerson != null && newPerson is Person) {
        setState(() {
          selectedPerson = persons.firstWhere(
            (p) => p.id == newPerson.id,
            orElse: () => newPerson,
          );
        });
      }
    });
  }

  void _showAddSaddleDialog() {
    if (selectedHorse == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Bitte wähle zuerst ein Pferd aus'),
        ),
      );
      return;
    }

    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NewSaddleScreen(preselectedHorse: selectedHorse),
      ),
    ).then((newSaddle) async {
      await _loadData();
      
      if (newSaddle != null && newSaddle is Saddle) {
        setState(() {
          selectedSaddle = saddles.firstWhere(
            (s) => s.id == newSaddle.id,
            orElse: () => newSaddle,
          );
        });
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    // Filtere Sättel für das ausgewählte Pferd
    final saddlesForHorse = selectedHorse == null
        ? <Saddle>[]
        : saddles.where((s) => s.horseId == selectedHorse!.id).toList();

    return Scaffold(
      backgroundColor: Colors.white,
      drawer: const SideNav(),
      appBar: const CustomAppBar(title: 'EquiPressure'),
      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ← HIER: Row mit Text und InfoButton (zentriert)
                  Center(
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Text(
                          'Neue Messung',
                          style: TextStyle(
                            fontSize: 28,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(width: 8),
                        InfoButton(
                          infoText: 'Hier können Sie eine neue Messung für Reiter:in und Pferd erstellen. '
                              'Wählen Sie die entsprechenden Daten aus und starten Sie die Messung.',
                          size: 24,
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 32),

                  // Reiter:in Dropdown
                  SelectionDropdown<Person>(
                    label: 'Reiter:in',
                    value: selectedPerson,
                    items: persons,
                    getItemText: (person) => person.fullName,
                    getItemId: (person) => person.id.toString(),
                    onChanged: (person) {
                      setState(() {
                        selectedPerson = person;
                      });
                    },
                    onAddNew: _showAddRiderDialog,
                    addNewText: 'Reiter:in hinzufügen',
                  ),

                  if (selectedPerson != null) ...[
                    const SizedBox(height: 12),
                    InfoBoxes(
                      value1: selectedPerson!.heightCm.toString(),
                      unit1: 'cm',
                      value2: selectedPerson!.weightKg.toString(),
                      unit2: 'kg',
                    ),
                  ],

                  const SizedBox(height: 24),

                  // Pferd Dropdown
                  SelectionDropdown<Horse>(
                    label: 'Pferd',
                    value: selectedHorse,
                    items: horses,
                    getItemText: (horse) => horse.name,
                    getItemId: (horse) => horse.id,
                    onChanged: (horse) {
                      setState(() {
                        selectedHorse = horse;
                        // Reset Sattel wenn Pferd geändert wird
                        selectedSaddle = null;
                      });
                    },
                  ),

                  if (selectedHorse != null) ...[
                    const SizedBox(height: 12),
                    InfoBoxes(
                      value1: selectedHorse!.height,
                      unit1: 'cm',
                      value2: selectedHorse!.weight,
                      unit2: 'kg',
                    ),
                  ],

                  const SizedBox(height: 24),

                  // Sattel Dropdown
                  SelectionDropdown<Saddle>(
                    label: 'Sattel',
                    value: selectedSaddle != null && 
                           saddlesForHorse.any((s) => s.id == selectedSaddle!.id)
                        ? selectedSaddle
                        : null,
                    items: saddlesForHorse,
                    getItemText: (saddle) => saddle.name,
                    getItemId: (saddle) => saddle.id,
                    onChanged: (saddle) {
                      setState(() {
                        selectedSaddle = saddle;
                      });
                    },
                    enabled: selectedHorse != null,
                    onAddNew: _showAddSaddleDialog,
                    addNewText: 'Sattel hinzufügen',
                  ),

                  if (selectedSaddle != null) ...[
                    const SizedBox(height: 12),
                    SingleInfoBox(text: 'Kategorie ${selectedSaddle!.category}'),
                  ],

                  const SizedBox(height: 48),

                  // Weiter zur Messung Button
                  SizedBox(
                    width: double.infinity,
                    height: 50,
                    child: ElevatedButton(
                      onPressed: _canProceed()
                          ? () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => MeasurementInputScreen(
                                    user: selectedPerson!,
                                    horse: selectedHorse!,
                                    saddle: selectedSaddle!,
                                  ),
                                ),
                              );
                            }
                          : null,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFD4B5F5),
                        foregroundColor: const Color(0xFF6B4C9A),
                        disabledBackgroundColor: Colors.grey[300],
                        disabledForegroundColor: Colors.grey[500],
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                        elevation: 0,
                      ),
                      child: const Text(
                        'Weiter zur Messung',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  bool _canProceed() {
    return selectedPerson != null && 
           selectedHorse != null && 
           selectedSaddle != null;
  }
}