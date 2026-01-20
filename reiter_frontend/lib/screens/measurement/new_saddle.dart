// File: lib/screens/new_saddle_screen.dart
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/saddle_service.dart';
import 'package:reiterappfrontend/services/horse_service.dart';

class NewSaddleScreen extends StatefulWidget {
  final Horse? preselectedHorse;
  
  const NewSaddleScreen({
    super.key,
    this.preselectedHorse,
  });

  @override
  State<NewSaddleScreen> createState() => _NewSaddleScreenState();
}

class _NewSaddleScreenState extends State<NewSaddleScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _categoryController = TextEditingController();
  
  Horse? _selectedHorse;
  List<Horse> _horses = [];
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _selectedHorse = widget.preselectedHorse;
    _loadHorses();
  }

  Future<void> _loadHorses() async {
    try {
      final horses = await HorseService().getHorses();
      setState(() {
        _horses = horses;
        
        // Stelle sicher, dass das vorausgewählte Pferd aus der geladenen Liste kommt
        if (widget.preselectedHorse != null) {
          _selectedHorse = horses.firstWhere(
            (h) => h.id == widget.preselectedHorse!.id,
            orElse: () => widget.preselectedHorse!,
          );
        }
      });
    } catch (e) {
      // Fehler beim Laden ignorieren
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[100],
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'Neuen Sattel anlegen',
          style: TextStyle(
            color: Colors.black,
            fontSize: 20,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Container(
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  padding: const EdgeInsets.all(24),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        _buildTextField('Name', _nameController),
                        const SizedBox(height: 24),
                        _buildTextField('Kategorie', _categoryController),
                        const SizedBox(height: 24),
                        _buildHorseDropdownField(),
                        const SizedBox(height: 200),
                        SizedBox(
                          width: double.infinity,
                          height: 50,
                          child: ElevatedButton(
                            onPressed: _save,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: Colors.deepPurple[100],
                              foregroundColor: Colors.black,
                              elevation: 0,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(8),
                              ),
                            ),
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: const [
                                Icon(Icons.save_outlined, size: 20),
                                SizedBox(width: 8),
                                Text(
                                  'Speichern',
                                  style: TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
    );
  }

  Widget _buildTextField(String label, TextEditingController controller) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 8),
        TextFormField(
          controller: controller,
          decoration: InputDecoration(
            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: BorderSide(color: Colors.grey[300]!),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: BorderSide(color: Colors.grey[300]!),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: const BorderSide(color: Colors.deepPurple),
            ),
          ),
          validator: (v) => v == null || v.isEmpty ? 'Pflichtfeld' : null,
        ),
      ],
    );
  }

  Widget _buildHorseDropdownField() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Pferd',
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey[300]!),
            borderRadius: BorderRadius.circular(8),
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: _selectedHorse?.id,
              isExpanded: true,
              icon: Icon(Icons.expand_more, color: Colors.grey[600]),
              hint: Text(
                'Pferd auswählen',
                style: TextStyle(color: Colors.grey[400]),
              ),
              items: _horses.map((Horse horse) {
                return DropdownMenuItem<String>(
                  value: horse.id,
                  child: Text(horse.name),
                );
              }).toList(),
              onChanged: (String? newValue) {
                if (newValue != null) {
                  setState(() {
                    _selectedHorse = _horses.firstWhere((h) => h.id == newValue);
                  });
                }
              },
            ),
          ),
        ),
      ],
    );
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedHorse == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Bitte Pferd auswählen')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      final service = SaddleService();
      
      final saddle = Saddle(
        id: DateTime.now().millisecondsSinceEpoch.toString(),
        name: _nameController.text,
        category: _categoryController.text,
        horseId: _selectedHorse!.id,
      );

      await service.addSaddle(saddle);

      if (mounted) {
        Navigator.pop(context, saddle); // Gib den erstellten Sattel zurück
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler beim Speichern: $e')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _categoryController.dispose();
    super.dispose();
  }
}