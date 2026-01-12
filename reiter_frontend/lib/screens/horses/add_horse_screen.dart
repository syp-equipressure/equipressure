import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:image_picker/image_picker.dart';
import 'package:reiterappfrontend/models/horse.dart';
import 'package:reiterappfrontend/widgets/dashed_border.dart';

class AddHorseScreen extends StatefulWidget {
  final void Function(String name, File? image, String age, String breed, String birthDate, String height, String weight, String sex) onSave;
  final VoidCallback onCancel;
  final VoidCallback? onDelete; // Optional: Nur im Edit-Modus
  final Horse? horse;

  const AddHorseScreen({
    super.key,
    required this.onSave,
    required this.onCancel,
    this.onDelete,
    this.horse,
  });

  @override
  State<AddHorseScreen> createState() => _AddHorseScreenState();
}

class _AddHorseScreenState extends State<AddHorseScreen> {
  final TextEditingController nameController = TextEditingController();
  final TextEditingController breedController = TextEditingController();
  final TextEditingController dayController = TextEditingController();
  final TextEditingController monthController = TextEditingController();
  final TextEditingController yearController = TextEditingController();
  final TextEditingController heightController = TextEditingController();
  final TextEditingController weightController = TextEditingController();

  File? selectedImage;
  String selectedSex = 'female';
  String? dateErrorMessage;
  bool _isPickingImage = false;

  final ImagePicker _picker = ImagePicker();

  @override
  void initState() {
    super.initState();
    
    if (widget.horse != null) {
      final horse = widget.horse!;
      
      nameController.text = horse.name;
      breedController.text = horse.breed == 'Unbekannt' ? '' : horse.breed;
      selectedSex = horse.sex;
      selectedImage = horse.image;
      
      if (horse.birthDate.isNotEmpty) {
        final parts = horse.birthDate.split('.');
        if (parts.length == 3) {
          dayController.text = parts[0];
          monthController.text = parts[1];
          yearController.text = parts[2];
        }
      }
      
      heightController.text = horse.height;
      weightController.text = horse.weight;
    }
  }

  Future<void> _pickImage() async {
    if (_isPickingImage) return;
    
    setState(() {
      _isPickingImage = true;
    });

    try {
      final XFile? image = await _picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 600,
      );
      if (image != null) {
        setState(() {
          selectedImage = File(image.path);
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Fehler beim Laden des Bildes: $e'),
            duration: const Duration(seconds: 2),
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _isPickingImage = false;
        });
      }
    }
  }

  bool _isValidDate(int day, int month, int year) {
    if (year < 1990 || year > DateTime.now().year) {
      return false;
    }

    if (month < 1 || month > 12) {
      return false;
    }

    final daysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    if (month == 2 && _isLeapYear(year)) {
      if (day < 1 || day > 29) {
        return false;
      }
    } else {
      if (day < 1 || day > daysInMonth[month - 1]) {
        return false;
      }
    }

    final inputDate = DateTime(year, month, day);
    if (inputDate.isAfter(DateTime.now())) {
      return false;
    }

    return true;
  }

  bool _isLeapYear(int year) {
    return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
  }

  void _validateDate() {
    setState(() {
      dateErrorMessage = null;
    });

    if (dayController.text.isEmpty || monthController.text.isEmpty || yearController.text.isEmpty) {
      return;
    }

    try {
      final day = int.parse(dayController.text);
      final month = int.parse(monthController.text);
      final year = int.parse(yearController.text);

      if (!_isValidDate(day, month, year)) {
        setState(() {
          dateErrorMessage = 'Ungültiges Datum';
        });
      }
    } catch (e) {
      setState(() {
        dateErrorMessage = 'Ungültiges Datum';
      });
    }
  }

  String _calculateAge() {
    if (dayController.text.isEmpty || monthController.text.isEmpty || yearController.text.isEmpty) {
      return '';
    }

    try {
      final day = int.parse(dayController.text);
      final month = int.parse(monthController.text);
      final year = int.parse(yearController.text);

      if (!_isValidDate(day, month, year)) {
        return '';
      }

      final birthDate = DateTime(year, month, day);
      final now = DateTime.now();
      final age = now.year - birthDate.year;
      return '${age}yo';
    } catch (e) {
      return '';
    }
  }

  String _formatBirthDate() {
    if (dayController.text.isEmpty || monthController.text.isEmpty || yearController.text.isEmpty) {
      return '';
    }
    
    final day = dayController.text.padLeft(2, '0');
    final month = monthController.text.padLeft(2, '0');
    final year = yearController.text;
    
    return '$day.$month.$year';
  }

  void _showDeleteConfirmation() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Pferd löschen'),
        content: Text('Möchtest du ${widget.horse!.name} wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Abbrechen'),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context); // Dialog schließen
              if (widget.onDelete != null) {
                widget.onDelete!();
              }
            },
            style: TextButton.styleFrom(
              foregroundColor: Colors.red,
            ),
            child: const Text('Löschen'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isEditMode = widget.horse != null;
    
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: widget.onCancel,
        ),
        title: Text(
          isEditMode ? 'Pferd bearbeiten' : 'Neues Pferd anlegen',
          textAlign: TextAlign.center,
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: Colors.black,
          ),
        ),
        centerTitle: false,
        actions: isEditMode
            ? [
                IconButton(
                  icon: const Icon(Icons.delete_outline, color: Colors.red),
                  onPressed: _showDeleteConfirmation,
                  tooltip: 'Pferd löschen',
                ),
              ]
            : null,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Bild Upload mit DashedBorder Widget
            GestureDetector(
              onTap: _pickImage,
              child: DashedBorder(
                color: Colors.grey[300]!,
                strokeWidth: 2,
                borderRadius: BorderRadius.circular(12),
                child: Container(
                  height: 160,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(12),
                    color: Colors.grey[50],
                  ),
                  child: selectedImage == null
                      ? Center(
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.upload_outlined, size: 40, color: Colors.grey[400]),
                              const SizedBox(height: 8),
                              Text(
                                'Lade ein Bild hoch',
                                style: TextStyle(color: Colors.grey[500], fontSize: 14),
                              ),
                            ],
                          ),
                        )
                      : ClipRRect(
                          borderRadius: BorderRadius.circular(12),
                          child: Image.file(
                            selectedImage!,
                            width: double.infinity,
                            height: 160,
                            fit: BoxFit.cover,
                          ),
                        ),
                ),
              ),
            ),

            const SizedBox(height: 24),

            // Name
            const Text(
              'Name',
              style: TextStyle(
                fontWeight: FontWeight.w600,
                fontSize: 14,
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: nameController,
              decoration: InputDecoration(
                filled: true,
                fillColor: Colors.white,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide(color: Colors.blue[400]!, width: 2),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide(color: Colors.blue[400]!, width: 2),
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
              ),
            ),

            const SizedBox(height: 20),

            // Rasse und Sex
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Rasse',
                        style: TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                        ),
                      ),
                      const SizedBox(height: 8),
                      TextField(
                        controller: breedController,
                        decoration: InputDecoration(
                          filled: true,
                          fillColor: Colors.white,
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(color: Colors.blue[400]!, width: 2),
                          ),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 16),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Sex',
                      style: TextStyle(
                        fontWeight: FontWeight.w600,
                        fontSize: 14,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        _buildSexButton('female', Icons.female),
                        const SizedBox(width: 8),
                        _buildSexButton('male', Icons.male),
                      ],
                    ),
                  ],
                ),
              ],
            ),

            const SizedBox(height: 20),

            // Geburtsdatum mit Validierung
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Geburtsdatum',
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 14,
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: dayController,
                        keyboardType: TextInputType.number,
                        textAlign: TextAlign.center,
                        maxLength: 2,
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                        ],
                        onChanged: (value) {
                          if (value.length == 2) {
                            FocusScope.of(context).nextFocus();
                          }
                          _validateDate();
                        },
                        decoration: InputDecoration(
                          hintText: 'DD',
                          hintStyle: TextStyle(color: Colors.grey[400]),
                          filled: true,
                          fillColor: Colors.white,
                          counterText: '',
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.grey[300]!,
                              width: 1,
                            ),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.grey[300]!,
                              width: 1,
                            ),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.blue[400]!,
                              width: 2,
                            ),
                          ),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 12,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: TextField(
                        controller: monthController,
                        keyboardType: TextInputType.number,
                        textAlign: TextAlign.center,
                        maxLength: 2,
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                        ],
                        onChanged: (value) {
                          if (value.length == 2) {
                            FocusScope.of(context).nextFocus();
                          }
                          _validateDate();
                        },
                        decoration: InputDecoration(
                          hintText: 'MM',
                          hintStyle: TextStyle(color: Colors.grey[400]),
                          filled: true,
                          fillColor: Colors.white,
                          counterText: '',
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.grey[300]!,
                              width: 1,
                            ),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.grey[300]!,
                              width: 1,
                            ),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.blue[400]!,
                              width: 2,
                            ),
                          ),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 12,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      flex: 2,
                      child: TextField(
                        controller: yearController,
                        keyboardType: TextInputType.number,
                        textAlign: TextAlign.center,
                        maxLength: 4,
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                        ],
                        onChanged: (value) {
                          _validateDate();
                          setState(() {});
                        },
                        decoration: InputDecoration(
                          hintText: 'YYYY',
                          hintStyle: TextStyle(color: Colors.grey[400]),
                          filled: true,
                          fillColor: Colors.white,
                          counterText: '',
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.grey[300]!,
                              width: 1,
                            ),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.grey[300]!,
                              width: 1,
                            ),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(8),
                            borderSide: BorderSide(
                              color: dateErrorMessage != null ? Colors.red : Colors.blue[400]!,
                              width: 2,
                            ),
                          ),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 12,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                if (dateErrorMessage != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8, left: 4),
                    child: Text(
                      dateErrorMessage!,
                      style: const TextStyle(
                        color: Colors.red,
                        fontSize: 12,
                      ),
                    ),
                  ),
              ],
            ),

            const SizedBox(height: 20),

            // Stockmaß und Gewicht
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Stockmaß',
                        style: TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          Expanded(
                            child: TextField(
                              controller: heightController,
                              keyboardType: TextInputType.number,
                              inputFormatters: [
                                FilteringTextInputFormatter.digitsOnly,
                              ],
                              decoration: InputDecoration(
                                filled: true,
                                fillColor: Colors.white,
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(8),
                                  borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                                ),
                                enabledBorder: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(8),
                                  borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                                ),
                                focusedBorder: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(8),
                                  borderSide: BorderSide(color: Colors.blue[400]!, width: 2),
                                ),
                                contentPadding: const EdgeInsets.symmetric(
                                  horizontal: 16,
                                  vertical: 12,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'cm',
                            style: TextStyle(
                              fontSize: 14,
                              color: Colors.grey[600],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 24),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Gewicht',
                        style: TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          Expanded(
                            child: TextField(
                              controller: weightController,
                              keyboardType: TextInputType.number,
                              inputFormatters: [
                                FilteringTextInputFormatter.digitsOnly,
                              ],
                              decoration: InputDecoration(
                                filled: true,
                                fillColor: Colors.white,
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(8),
                                  borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                                ),
                                enabledBorder: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(8),
                                  borderSide: BorderSide(color: Colors.grey[300]!, width: 1),
                                ),
                                focusedBorder: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(8),
                                  borderSide: BorderSide(color: Colors.blue[400]!, width: 2),
                                ),
                                contentPadding: const EdgeInsets.symmetric(
                                  horizontal: 16,
                                  vertical: 12,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'kg',
                            style: TextStyle(
                              fontSize: 14,
                              color: Colors.grey[600],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),

            const SizedBox(height: 32),

            // Speichern Button
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton(
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.purple[100],
                  foregroundColor: Colors.black87,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  elevation: 0,
                ),
                onPressed: () {
                  if (nameController.text.isEmpty) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Bitte Name ausfüllen'),
                        duration: Duration(seconds: 2),
                      ),
                    );
                    return;
                  }

                  if (dayController.text.isEmpty || monthController.text.isEmpty || yearController.text.isEmpty) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Bitte Geburtsdatum ausfüllen'),
                        duration: Duration(seconds: 2),
                      ),
                    );
                    return;
                  }

                  if (dateErrorMessage != null) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Bitte gültiges Datum eingeben'),
                        duration: Duration(seconds: 2),
                      ),
                    );
                    return;
                  }

                  String age = _calculateAge();
                  String breed = breedController.text.isEmpty ? 'Unbekannt' : breedController.text;
                  String birthDate = _formatBirthDate();
                  String height = heightController.text;
                  String weight = weightController.text;
                  
                  widget.onSave(
                    nameController.text,
                    selectedImage,
                    age,
                    breed,
                    birthDate,
                    height,
                    weight,
                    selectedSex,
                  );
                },
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.save_outlined, size: 18, color: Colors.black87),
                    const SizedBox(width: 8),
                    const Text(
                      'Speichern',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                        color: Colors.black87,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSexButton(String sex, IconData icon) {
    final isSelected = selectedSex == sex;
    return GestureDetector(
      onTap: () => setState(() => selectedSex = sex),
      child: Container(
        width: 44,
        height: 44,
        decoration: BoxDecoration(
          color: isSelected ? Colors.grey[300] : Colors.grey[100],
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: isSelected ? Colors.grey[400]! : Colors.grey[300]!,
            width: 1,
          ),
        ),
        child: Icon(
          icon,
          color: Colors.black87,
          size: 20,
        ),
      ),
    );
  }
}