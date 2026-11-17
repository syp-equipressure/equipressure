// File: lib/screens/new_saddle_screen.dart
import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/saddle.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/services/saddle_service.dart';
import 'package:reiterappfrontend/services/horse_service.dart';

/// Legt einen Sattel an, mit [existingSaddle] wird dieser bearbeitet oder gelöscht.
/// Gibt den gespeicherten Sattel zurück, nach dem Löschen null.
class NewSaddleScreen extends StatefulWidget {
  final Horse? preselectedHorse;
  final Saddle? existingSaddle;

  const NewSaddleScreen({
    super.key,
    this.preselectedHorse,
    this.existingSaddle,
  });

  @override
  State<NewSaddleScreen> createState() => _NewSaddleScreenState();
}

class _NewSaddleScreenState extends State<NewSaddleScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();

  Horse? _selectedHorse;
  List<Horse> _horses = [];
  SaddleCategory? _selectedCategory;
  List<SaddleCategory> _categories = [];
  bool _isLoading = false;

  bool get _isEditing => widget.existingSaddle != null;

  @override
  void initState() {
    super.initState();
    _selectedHorse = widget.preselectedHorse;
    _nameController.text = widget.existingSaddle?.name ?? '';
    _loadHorses();
    _loadCategories();
  }

  Future<void> _loadCategories() async {
    try {
      final categories = await SaddleService().getSaddleCategories();
      if (!mounted) return;
      final existingCategory = widget.existingSaddle?.category.toLowerCase();
      setState(() {
        _categories = categories;
        _selectedCategory ??= categories
            .where((c) => c.name.toLowerCase() == existingCategory)
            .firstOrNull;
      });
    } catch (e) {
      // Fehler beim Laden ignorieren
    }
  }

  Future<void> _loadHorses() async {
    try {
      final horses = await HorseService().getHorses();
      if (!mounted) return;
      setState(() {
        _horses = horses;
        
        // Stelle sicher, dass das vorausgewählte Pferd aus der geladenen Liste kommt
        final horseId = widget.existingSaddle?.horseId ?? widget.preselectedHorse?.id;
        if (horseId != null) {
          _selectedHorse = horses.where((h) => h.id == horseId).firstOrNull ??
              widget.preselectedHorse;
        }
      });
    } catch (e) {
      // Fehler beim Laden ignorieren
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
          onPressed: () => Navigator.pop(context),
        ),
        title: Text(
          _isEditing ? 'Sattel bearbeiten' : 'Neuen Sattel anlegen',
          style: const TextStyle(
            color: AppColors.textStrong,
            fontSize: 20,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          if (_isEditing)
            IconButton(
              tooltip: 'Sattel löschen',
              icon: Icon(Icons.delete_outline, color: AppColors.error),
              onPressed: _isLoading ? null : _confirmDelete,
            ),
        ],
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
                        _buildTextField('Name', _nameController),
                        const SizedBox(height: 24),
                        _buildCategoryDropdownField(),
                        const SizedBox(height: 24),
                        _buildHorseDropdownField(),
                        const SizedBox(height: 200),
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
            border: Border.all(color: AppColors.border),
            borderRadius: BorderRadius.circular(8),
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<String>(
              value: _selectedHorse?.id,
              isExpanded: true,
              icon: Icon(Icons.expand_more, color: AppColors.textSoft),
              hint: Text(
                'Pferd auswählen',
                style: TextStyle(color: AppColors.textMuted),
              ),
              items: _horses.map((Horse horse) {
                return DropdownMenuItem<String>(
                  value: horse.id,
                  child: Text(horse.name, overflow: TextOverflow.ellipsis),
                );
              }).toList(),
              // das Pferd eines bestehenden Sattels kann nicht geändert werden
              onChanged: _isEditing
                  ? null
                  : (String? newValue) {
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

  Widget _buildCategoryDropdownField() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Kategorie',
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          decoration: BoxDecoration(
            border: Border.all(color: AppColors.border),
            borderRadius: BorderRadius.circular(8),
          ),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<int>(
              value: _selectedCategory?.id,
              isExpanded: true,
              icon: Icon(Icons.expand_more, color: AppColors.textSoft),
              hint: Text(
                'Kategorie auswählen',
                style: TextStyle(color: AppColors.textMuted),
              ),
              items: _categories.map((SaddleCategory category) {
                return DropdownMenuItem<int>(
                  value: category.id,
                  child: Text(category.name),
                );
              }).toList(),
              onChanged: (int? newValue) {
                if (newValue != null) {
                  setState(() {
                    _selectedCategory = _categories.firstWhere((c) => c.id == newValue);
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
    if (_selectedCategory == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Bitte Kategorie auswählen')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      final service = SaddleService();
      
      final saddle = Saddle(
        id: widget.existingSaddle?.id ?? DateTime.now().millisecondsSinceEpoch.toString(),
        name: _nameController.text,
        category: _selectedCategory!.name,
        horseId: _selectedHorse!.id,
      );

      final savedSaddle =
          _isEditing ? await service.updateSaddle(saddle) : await service.addSaddle(saddle);

      if (mounted) {
        Navigator.pop(context, savedSaddle); // Gib den erstellten Sattel zurück
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

  Future<void> _confirmDelete() async {
    final saddle = widget.existingSaddle!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Sattel löschen'),
        content: Text('Möchtest du den Sattel "${saddle.name}" wirklich löschen?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            child: const Text('Löschen'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _isLoading = true);
    try {
      await SaddleService().deleteSaddle(saddle);
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (!mounted) return;
      setState(() => _isLoading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            e is SaddleInUseException ? e.toString() : 'Sattel konnte nicht gelöscht werden.',
          ),
        ),
      );
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }
}