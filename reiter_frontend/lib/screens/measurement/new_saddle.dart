// File: lib/screens/new_saddle_screen.dart
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/saddle_service.dart';
import 'package:reiterappfrontend/services/horse_service.dart';

class NewSaddleScreen extends StatefulWidget {
  const NewSaddleScreen({super.key});

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
    _loadHorses();
  }

  Future<void> _loadHorses() async {
    try {
      final horses = await HorseService().getHorses();
      setState(() {
        _horses = horses;
      });
    } catch (e) {
      print('Fehler beim Laden der Pferde: $e');
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
        InkWell(
          onTap: () => _showHorsePicker(),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              border: Border.all(color: Colors.grey[300]!),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  _selectedHorse?.name ?? '',
                  style: TextStyle(
                    fontSize: 16,
                    color: _selectedHorse == null ? Colors.grey[400] : Colors.black,
                  ),
                ),
                const Icon(Icons.chevron_right, color: Colors.grey),
              ],
            ),
          ),
        ),
      ],
    );
  }

  void _showHorsePicker() {
    showModalBottomSheet(
      context: context,
      builder: (context) => Container(
        padding: const EdgeInsets.symmetric(vertical: 16),
        child: _horses.isEmpty
            ? const Center(child: Text('Keine Pferde verfügbar'))
            : ListView.builder(
                shrinkWrap: true,
                itemCount: _horses.length,
                itemBuilder: (context, index) {
                  final horse = _horses[index];
                  return ListTile(
                    title: Text(horse.name),
                    onTap: () {
                      setState(() => _selectedHorse = horse);
                      Navigator.pop(context);
                    },
                  );
                },
              ),
      ),
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
        Navigator.pop(context);
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