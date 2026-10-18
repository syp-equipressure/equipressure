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
  final _birthDate = TextEditingController();
  final _city = TextEditingController();
  final _postalCode = TextEditingController();
  final _street = TextEditingController();

  DateTime? _selectedBirthDate;
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
                        _buildBirthDateField(),
                        const SizedBox(height: 24),
                        _buildNumberField('Größe', _height, 'cm'),
                        const SizedBox(height: 24),
                        _buildNumberField('Gewicht', _weight, 'kg'),
                        const SizedBox(height: 24),
                        _buildTextField('Ort', _city),
                        const SizedBox(height: 24),
                        _buildNumberField('PLZ', _postalCode, ''),
                        const SizedBox(height: 24),
                        _buildTextField('Straße und Hausnummer (optional)', _street,
                            requiredField: false),
                        const SizedBox(height: 24),
                        _buildTextField('E-Mail', _email, requiredField: false),
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

  Widget _buildTextField(
    String label,
    TextEditingController controller, {
    bool requiredField = true,
  }) {
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
          validator: (v) => requiredField && (v == null || v.isEmpty)
              ? 'Pflichtfeld'
              : null,
        ),
      ],
    );
  }

  Widget _buildBirthDateField() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Geburtsdatum',
          style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
        ),
        const SizedBox(height: 8),
        TextFormField(
          controller: _birthDate,
          readOnly: true,
          onTap: _pickBirthDate,
          decoration: InputDecoration(
            hintText: 'TT.MM.JJJJ',
            suffixIcon: const Icon(Icons.calendar_today_outlined),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
          ),
          validator: (_) => _selectedBirthDate == null ? 'Pflichtfeld' : null,
        ),
      ],
    );
  }

  Future<void> _pickBirthDate() async {
    final today = DateTime.now();
    final latestDate = DateTime(today.year, today.month, today.day - 1);
    final selected = await showDatePicker(
      context: context,
      initialDate: _selectedBirthDate ?? DateTime(2000, 1, 1),
      firstDate: DateTime(1900),
      lastDate: latestDate,
    );
    if (selected == null) return;
    setState(() {
      _selectedBirthDate = selected;
      _birthDate.text =
          '${selected.day.toString().padLeft(2, '0')}.${selected.month.toString().padLeft(2, '0')}.${selected.year}';
    });
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
      final person = Person(
        id: 0,
        firstname: _firstname.text.trim(),
        lastname: _lastname.text.trim(),
        heightCm: int.parse(_height.text),
        weightKg: int.parse(_weight.text),
        plz: int.parse(_postalCode.text),
        city: _city.text.trim(),
        street: _street.text.trim(),
        housenumber: 0,
        email: _email.text.trim(),
        dob: _selectedBirthDate,
      );
      final createdPerson = await service.addPerson(person);

      if (mounted) {
        Navigator.pop(context, createdPerson);
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
    _birthDate.dispose();
    _city.dispose();
    _postalCode.dispose();
    _street.dispose();
    super.dispose();
  }
}