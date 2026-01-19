// File: lib/screens/new_person_screen.dart
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/person.dart';
import 'package:reiterappfrontend/services/person_service.dart';

class NewPersonScreen extends StatefulWidget {
  const NewPersonScreen({super.key});

  @override
  State<NewPersonScreen> createState() => _NewPersonScreenState();
}

class _NewPersonScreenState extends State<NewPersonScreen> {
  final _formKey = GlobalKey<FormState>();

  final _firstname = TextEditingController();
  final _lastname = TextEditingController();
  final _height = TextEditingController();
  final _weight = TextEditingController();
  final _email = TextEditingController();

  bool hidden = false;
  bool _isLoading = false;

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
          'Neuen Reiter anlegen',
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
                        _buildTextField('Vorname', _firstname),
                        const SizedBox(height: 24),
                        _buildTextField('Nachname', _lastname),
                        const SizedBox(height: 24),
                        _buildNumberField('Größe', _height, 'cm'),
                        const SizedBox(height: 24),
                        _buildNumberField('Gewicht', _weight, 'kg'),
                        const SizedBox(height: 24),
                        CheckboxListTile(
                          value: hidden,
                          onChanged: (v) => setState(() => hidden = v!),
                          title: const Text(
                            'Als versteckter Benutzer anzeigen',
                            style: TextStyle(fontSize: 16),
                          ),
                          controlAffinity: ListTileControlAffinity.leading,
                          contentPadding: EdgeInsets.zero,
                          activeColor: Colors.deepPurple,
                        ),
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

  Widget _buildNumberField(String label, TextEditingController controller, String unit) {
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
          keyboardType: TextInputType.number,
          decoration: InputDecoration(
            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            suffixText: unit,
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
          validator: (v) {
            if (v == null || v.isEmpty) return 'Pflichtfeld';
            if (int.tryParse(v) == null) return 'Nur Zahlen erlaubt';
            return null;
          },
        ),
      ],
    );
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isLoading = true);

    try {
      final service = PersonService();
      final nextId = await service.getNextId();

      await service.addPerson(
        Person(
          id: nextId,
          firstname: _firstname.text,
          lastname: _lastname.text,
          heightCm: int.parse(_height.text),
          weightKg: int.parse(_weight.text),
          plz: 0,
          city: '',
          street: '',
          housenumber: 0,
          email: _email.text,
          hidden: hidden,
        ),
      );

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
    _firstname.dispose();
    _lastname.dispose();
    _height.dispose();
    _weight.dispose();
    _email.dispose();
    super.dispose();
  }
}