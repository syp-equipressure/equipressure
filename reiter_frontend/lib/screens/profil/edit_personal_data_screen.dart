import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:reiterappfrontend/models/user_data.dart';
import 'package:reiterappfrontend/services/person_service.dart';
import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:reiterappfrontend/widgets/person_avatar.dart';

/// Ändert die persönlichen Daten und das Profilbild. Die E-Mail ist zugleich der Login
/// in Keycloak und kann daher hier nicht geändert werden.
class EditPersonalDataScreen extends StatefulWidget {
  final UserData userData;

  const EditPersonalDataScreen({super.key, required this.userData});

  @override
  State<EditPersonalDataScreen> createState() => _EditPersonalDataScreenState();
}

class _EditPersonalDataScreenState extends State<EditPersonalDataScreen> {
  final _formKey = GlobalKey<FormState>();
  final ImagePicker _picker = ImagePicker();

  late final TextEditingController _firstName;
  late final TextEditingController _lastName;
  late final TextEditingController _street;
  late final TextEditingController _postalCode;
  late final TextEditingController _city;
  late final TextEditingController _height;
  late final TextEditingController _weight;
  late final TextEditingController _birthDate;

  /// Geburtsdatum im Format YYYY-MM-DD
  late String _dateOfBirth;

  File? _selectedImage;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final data = widget.userData;
    _firstName = TextEditingController(text: data.firstName);
    _lastName = TextEditingController(text: data.lastName);
    _street = TextEditingController(text: data.street);
    _postalCode = TextEditingController(text: data.postalCode);
    _city = TextEditingController(text: data.city);
    _height = TextEditingController(text: data.heightCm);
    _weight = TextEditingController(text: data.weightKg);
    _dateOfBirth = data.dateOfBirth.split('T').first;
    _birthDate = TextEditingController(text: _formatDate(_dateOfBirth));
  }

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _street.dispose();
    _postalCode.dispose();
    _city.dispose();
    _height.dispose();
    _weight.dispose();
    _birthDate.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    final image = await _picker.pickImage(source: ImageSource.gallery, maxWidth: 1600, imageQuality: 85);
    if (image != null) setState(() => _selectedImage = File(image.path));
  }

  static String _formatDate(String isoDate) {
    final parts = isoDate.split('-');
    return parts.length == 3 ? '${parts[2]}.${parts[1]}.${parts[0]}' : '';
  }

  Future<void> _pickBirthDate() async {
    final now = DateTime.now();
    final date = await showDatePicker(
      context: context,
      initialDate: DateTime.tryParse(_dateOfBirth) ?? DateTime(now.year - 20),
      firstDate: DateTime(1900),
      lastDate: now.subtract(const Duration(days: 1)),
    );
    if (date == null) return;

    final month = date.month.toString().padLeft(2, '0');
    final day = date.day.toString().padLeft(2, '0');
    setState(() {
      _dateOfBirth = '${date.year}-$month-$day';
      _birthDate.text = _formatDate(_dateOfBirth);
    });
  }

  static double? _parseNumber(String text) => double.tryParse(text.trim().replaceAll(',', '.'));

  static String? _required(String? value) =>
      value == null || value.trim().isEmpty ? 'Pflichtfeld' : null;

  static String? _positiveNumber(String? value) {
    final number = _parseNumber(value ?? '');
    return number == null || number <= 0 ? 'Bitte eine Zahl größer 0 eingeben' : null;
  }

  Future<void> _save() async {
    if (_saving || !_formKey.currentState!.validate()) return;

    setState(() => _saving = true);
    final personId = widget.userData.id;
    try {
      await PersonService.updatePerson(personId, {
        'firstName': _firstName.text.trim(),
        'lastName': _lastName.text.trim(),
        'height': _parseNumber(_height.text),
        'weight': _parseNumber(_weight.text),
        'dateOfBirth': _dateOfBirth,
        'address': {
          'addressName': _street.text.trim().isEmpty ? null : _street.text.trim(),
          'cityName': _city.text.trim(),
          'plz': _postalCode.text.trim(),
        },
      });
      if (_selectedImage != null) {
        await PersonService.uploadImage(personId, _selectedImage!);
      }
      if (mounted) Navigator.pop(context, true);
    } catch (error) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Daten konnten nicht gespeichert werden: $error')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.purple300,
      appBar: AppBar(
        backgroundColor: AppColors.purple300,
        shape: const Border(),
        elevation: 0,
        title: const Text('Daten bearbeiten'),
      ),
      body: Container(
        margin: const EdgeInsets.symmetric(horizontal: 16),
        decoration: const BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.only(
            topLeft: Radius.circular(30),
            topRight: Radius.circular(30),
          ),
        ),
        child: Form(
          key: _formKey,
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              Center(child: _buildImagePicker()),
              const SizedBox(height: 24),
              _buildField(_firstName, 'Vorname', validator: _required),
              _buildField(_lastName, 'Nachname', validator: _required),
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: TextFormField(
                  controller: _birthDate,
                  readOnly: true,
                  validator: _required,
                  onTap: _pickBirthDate,
                  decoration: const InputDecoration(
                    labelText: 'Geburtsdatum',
                    suffixIcon: Icon(Icons.calendar_today_outlined),
                  ),
                ),
              ),
              _buildField(_street, 'Straße und Hausnummer'),
              _buildField(_postalCode, 'PLZ', validator: _required, number: true),
              _buildField(_city, 'Ort', validator: _required),
              _buildField(_height, 'Größe in cm', validator: _positiveNumber, number: true),
              _buildField(_weight, 'Gewicht in kg', validator: _positiveNumber, number: true),
              const SizedBox(height: 8),
              FilledButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('Speichern'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildImagePicker() {
    return InkWell(
      customBorder: const CircleBorder(),
      onTap: _saving ? null : _pickImage,
      child: Stack(
        alignment: Alignment.bottomRight,
        children: [
          _selectedImage != null
              ? CircleAvatar(radius: 56, backgroundImage: FileImage(_selectedImage!))
              : PersonAvatar(
                  personId: widget.userData.id,
                  imageUpdatedAt: widget.userData.imageUpdatedAt,
                  name: widget.userData.name,
                  radius: 56,
                ),
          const CircleAvatar(
            radius: 16,
            backgroundColor: AppColors.primary,
            child: Icon(Icons.photo_camera_outlined, size: 18, color: AppColors.onPrimary),
          ),
        ],
      ),
    );
  }

  Widget _buildField(
    TextEditingController controller,
    String label, {
    String? Function(String?)? validator,
    bool number = false,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextFormField(
        controller: controller,
        validator: validator,
        keyboardType: number ? const TextInputType.numberWithOptions(decimal: true) : null,
        decoration: InputDecoration(labelText: label),
      ),
    );
  }
}
