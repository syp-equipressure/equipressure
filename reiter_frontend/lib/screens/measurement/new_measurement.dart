import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/models/person.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/screens/measurement/new_person.dart';
import 'package:reiterappfrontend/screens/measurement/new_saddle.dart';
import 'package:reiterappfrontend/screens/profil/devices_screen.dart';
import 'package:reiterappfrontend/services/device_connection_service.dart';
import 'package:reiterappfrontend/services/device_service.dart';
import 'package:reiterappfrontend/services/person_service.dart';
import 'package:reiterappfrontend/widgets/device_connection_button.dart';
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
  List<Device> devices = [];
  bool isLoading = true;

  Person? selectedPerson;
  Horse? selectedHorse;
  Saddle? selectedSaddle;
  Device? selectedDevice;

  final DeviceConnectionService _connection = DeviceConnectionService();

  @override
  void initState() {
    super.initState();
    selectedHorse = widget.horse;
    // "Weiter zur Messung" hängt vom Verbindungsstatus ab
    _connection.addListener(_onConnectionChanged);
    _loadData();
  }

  @override
  void dispose() {
    _connection.removeListener(_onConnectionChanged);
    super.dispose();
  }

  void _onConnectionChanged() {
    if (mounted) setState(() {});
  }

  Future<void> _loadData() async {
    setState(() => isLoading = true);

    try {
      final loadedPersons = await _personService.getPersons();
      final loadedSaddles = await _saddleService.getSaddles();
      final loadedHorses = await _horseService.getHorses();
      final personId = await _horseService.currentPersonId();
      final loadedDevices = await DeviceService().getDevices(personId.toString());
      if (!mounted) return;

      setState(() {
        persons = loadedPersons;
        saddles = loadedSaddles;
        horses = loadedHorses;
        devices = loadedDevices;
        selectedDevice = _preselectDevice(loadedDevices);
        
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
      if (!mounted) return;
      setState(() => isLoading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Fehler beim Laden: $e')),
      );
    }
  }

  /// Bereits verbundenes Gerät, sonst die bisherige Auswahl, sonst das einzige Gerät.
  Device? _preselectDevice(List<Device> loadedDevices) {
    Device? find(String? id) {
      for (final device in loadedDevices) {
        if (device.id == id) return device;
      }
      return null;
    }

    return find(_connection.device?.id) ??
        find(selectedDevice?.id) ??
        (loadedDevices.length == 1 ? loadedDevices.first : null);
  }

  void _onDeviceChanged(Device? device) {
    // eine Verbindung zu einem anderen Gerät wird getrennt
    if (_connection.device != null && _connection.device!.id != device?.id) {
      _connection.disconnect();
    }
    setState(() => selectedDevice = device);
  }

  Future<void> _openDevices() async {
    final personId = await _horseService.currentPersonId();
    if (!mounted) return;
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => DevicesScreen(
          devices: List.of(devices),
          ownerId: personId.toString(),
        ),
      ),
    );
    DeviceService().clearCache();
    await _loadData();
  }

  Widget _buildDeviceSection() {
    if (devices.isEmpty) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Messgerät',
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          Text(
            'Du hast noch kein Satteldruckmessgerät hinterlegt.',
            style: TextStyle(fontSize: 14, color: Colors.grey[700]),
          ),
          TextButton.icon(
            onPressed: _openDevices,
            icon: const Icon(Icons.add),
            label: const Text('Gerät hinzufügen'),
            style: TextButton.styleFrom(
              foregroundColor: const Color(0xFF6B4C9A),
              padding: EdgeInsets.zero,
            ),
          ),
        ],
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SelectionDropdown<Device>(
          label: 'Messgerät',
          value: selectedDevice,
          items: devices,
          getItemText: (device) => device.name == device.number
              ? device.number
              : '${device.number} · ${device.name}',
          getItemId: (device) => device.id,
          onChanged: _onDeviceChanged,
        ),
        if (selectedDevice != null) ...[
          const SizedBox(height: 12),
          DeviceConnectionButton(device: selectedDevice!),
        ],
      ],
    );
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

                  const SizedBox(height: 24),

                  // Messgerät auswählen und verbinden
                  _buildDeviceSection(),

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
                                    device: selectedDevice!,
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
           selectedSaddle != null &&
           selectedDevice != null &&
           _connection.isConnectedTo(selectedDevice!);
  }
}