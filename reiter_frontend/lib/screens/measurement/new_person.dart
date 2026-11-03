// File: lib/screens/new_person_screen.dart
import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/person.dart';
import 'package:reiterappfrontend/services/person_service.dart';

/// Legt einen eigenen Reiter für Messungen an oder bearbeitet ihn ([existingRider]).
/// Gibt den gespeicherten Reiter zurück, nach dem Löschen null.
class NewPersonScreen extends StatefulWidget {
  final Person? existingRider;

  const NewPersonScreen({super.key, this.existingRider});

  @override
  State<NewPersonScreen> createState() => _NewPersonScreenState();
}

class _NewPersonScreenState extends State<NewPersonScreen> {
  final _formKey = GlobalKey<FormState>();

  final _firstname = TextEditingController();
  final _lastname = TextEditingController();
  final _height = TextEditingController();
  final _weight = TextEditingController();

  bool _isLoading = false;

  bool get _isEditing => widget.existingRider != null;

  @override
  void initState() {
    super.initState();
    final rider = widget.existingRider;
    if (rider != null) {
      _firstname.text = rider.firstname;
      _lastname.text = rider.lastname;
      _height.text = rider.heightCm.toString();
      _weight.text = rider.weightKg.toString();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        backgroundColor: AppColors.surface,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textStrong),
          onPressed: () => Navigator.pop(context, widget.existingRider),
        ),
        title: Text(
          _isEditing ? 'Reiter bearbeiten' : 'Neuen Reiter anlegen',
          style: const TextStyle(
            color: AppColors.textStrong,
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
                    color: AppColors.surface,
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
                        const SizedBox(height: 32),
                        SizedBox(
                          width: double.infinity,
                          height: 50,
                          child: ElevatedButton(
                            onPressed: _save,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppColors.purple100,
                              foregroundColor: AppColors.textStrong,
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
                        if (_isEditing) ...[
                          const SizedBox(height: 12),
                          SizedBox(
                            width: double.infinity,
                            height: 50,
                            child: TextButton.icon(
                              onPressed: _delete,
                              style: TextButton.styleFrom(foregroundColor: Colors.red),
                              icon: const Icon(Icons.delete_outline, size: 20),
                              label: const Text('Reiter löschen'),
                            ),
                          ),
                        ],
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
              borderSide: BorderSide(color: AppColors.border),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: BorderSide(color: AppColors.border),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: const BorderSide(color: AppColors.primary),
            ),
          ),
          validator: (v) => v == null || v.trim().isEmpty ? 'Pflichtfeld' : null,
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
              borderSide: BorderSide(color: AppColors.border),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: BorderSide(color: AppColors.border),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(8),
              borderSide: const BorderSide(color: AppColors.primary),
            ),
          ),
          validator: (v) {
            if (v == null || v.isEmpty) return 'Pflichtfeld';
            final value = int.tryParse(v);
            if (value == null) return 'Nur Zahlen erlaubt';
            if (value <= 0) return 'Muss größer als 0 sein';
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
      final firstname = _firstname.text.trim();
      final lastname = _lastname.text.trim();
      final heightCm = int.parse(_height.text);
      final weightKg = int.parse(_weight.text);
      final rider = widget.existingRider;
      final savedRider = rider == null
          ? await service.addRider(firstname, lastname, heightCm, weightKg)
          : await service.updateRider(rider.id, firstname, lastname, heightCm, weightKg);

      if (mounted) {
        Navigator.pop(context, savedRider);
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

  Future<void> _delete() async {
    final rider = widget.existingRider!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Reiter löschen'),
        content: Text(
          '${rider.fullName} wird mit allen Messungen endgültig gelöscht. '
          'Das kann nicht rückgängig gemacht werden.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Endgültig löschen'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _isLoading = true);

    try {
      await PersonService().deleteRider(rider.id);

      if (mounted) {
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler beim Löschen: $e')),
        );
      }
    }
  }

  @override
  void dispose() {
    _firstname.dispose();
    _lastname.dispose();
    _height.dispose();
    _weight.dispose();
    super.dispose();
  }
}
