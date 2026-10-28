import 'package:flutter/material.dart';
import '../../services/account_service.dart';
import '../../theme/app_colors.dart';
import '../../theme/app_text_styles.dart';
import '../../widgets/common/app_button.dart';
import '../../widgets/common/app_text_field.dart';
import 'widgets/page_indicator.dart';

class ProfileSetupScreen extends StatefulWidget {
  final VoidCallback onNext;

  const ProfileSetupScreen({super.key, required this.onNext});

  @override
  State<ProfileSetupScreen> createState() => _ProfileSetupScreenState();
}

class _ProfileSetupScreenState extends State<ProfileSetupScreen> {
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _day = TextEditingController();
  final _month = TextEditingController();
  final _year = TextEditingController();
  final _city = TextEditingController();
  final _plz = TextEditingController();
  final _street = TextEditingController();
  final _houseNumber = TextEditingController();
  final _height = TextEditingController();
  final _weight = TextEditingController();

  String _email = '';
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _prefillFromAccount();
  }

  /// Name und E-Mail vom Google/Microsoft/GitHub-Konto übernehmen
  Future<void> _prefillFromAccount() async {
    try {
      final identity = await AccountService.loadIdentity();
      if (!mounted) return;
      setState(() {
        if (_firstName.text.isEmpty) _firstName.text = identity.firstName;
        if (_lastName.text.isEmpty) _lastName.text = identity.lastName;
        _email = identity.email;
      });
    } catch (_) {
      // nicht schlimm, dann füllt man die Felder selbst aus
    }
  }

  Future<void> _save() async {
    final dateOfBirth = _parseDate();
    final height = double.tryParse(_height.text.replaceAll(',', '.'));
    final weight = double.tryParse(_weight.text.replaceAll(',', '.'));
    final fields = [_firstName, _lastName, _city, _plz, _street];
    if (fields.any((c) => c.text.trim().isEmpty) ||
        dateOfBirth == null ||
        height == null ||
        weight == null ||
        height <= 0 ||
        weight <= 0) {
      _showError('Bitte fülle alle Felder richtig aus.');
      return;
    }

    setState(() => _saving = true);
    try {
      await AccountService.createAccount(
        firstName: _firstName.text.trim(),
        lastName: _lastName.text.trim(),
        dateOfBirth: dateOfBirth,
        height: height,
        weight: weight,
        street: '${_street.text.trim()} ${_houseNumber.text.trim()}'.trim(),
        plz: _plz.text.trim(),
        city: _city.text.trim(),
      );
      widget.onNext();
    } on AccountApiException catch (e) {
      _showError(e.isConflict
          ? 'Mit dieser E-Mail-Adresse gibt es bereits ein Profil.'
          : 'Bitte überprüfe deine Angaben.');
    } catch (_) {
      _showError('Speichern hat nicht funktioniert. Bitte versuche es noch einmal.');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  DateTime? _parseDate() {
    final day = int.tryParse(_day.text);
    final month = int.tryParse(_month.text);
    final year = int.tryParse(_year.text);
    if (day == null || month == null || year == null) return null;
    final date = DateTime(year, month, day);
    // ungültige Daten wie 31.02. rollen in den nächsten Monat
    if (date.day != day || date.month != month || date.isAfter(DateTime.now())) {
      return null;
    }
    return date;
  }

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  void dispose() {
    for (final controller in [
      _firstName, _lastName, _day, _month, _year, _city, _plz, _street,
      _houseNumber, _height, _weight,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.background,
      child: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 32),
              const Center(
                child: Text('Dein Profil konfigurieren', style: AppTextStyles.h1),
              ),
              if (_email.isNotEmpty) ...[
                const SizedBox(height: 8),
                Center(
                  child: Text(
                    'Angemeldet als $_email',
                    style: AppTextStyles.labelSmall,
                  ),
                ),
              ],
              const SizedBox(height: 32),
              AppTextField(label: 'Vorname', controller: _firstName),
              const SizedBox(height: 16),
              AppTextField(label: 'Nachname', controller: _lastName),
              const SizedBox(height: 16),
              Text('Geburtsdatum', style: AppTextStyles.label),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: AppTextFieldSmall(
                      hint: 'DD',
                      controller: _day,
                      keyboardType: TextInputType.number,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: AppTextFieldSmall(
                      hint: 'MM',
                      controller: _month,
                      keyboardType: TextInputType.number,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: AppTextFieldSmall(
                      hint: 'YYYY',
                      controller: _year,
                      keyboardType: TextInputType.number,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(flex: 2, child: AppTextField(label: 'Ort', controller: _city)),
                  const SizedBox(width: 16),
                  Expanded(
                    flex: 1,
                    child: AppTextField(
                      label: 'PLZ',
                      controller: _plz,
                      keyboardType: TextInputType.number,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(flex: 2, child: AppTextField(label: 'Straße', controller: _street)),
                  const SizedBox(width: 16),
                  Expanded(
                    flex: 1,
                    child: AppTextField(label: 'Hausnr', controller: _houseNumber),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: AppTextFieldWithSuffix(
                      label: 'Größe',
                      suffix: 'cm',
                      controller: _height,
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: AppTextFieldWithSuffix(
                      label: 'Gewicht',
                      suffix: 'kg',
                      controller: _weight,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              const Text(
                'Größe und Gewicht brauchen wir, um die Messergebnisse richtig auszuwerten.',
                style: AppTextStyles.labelSmall,
              ),
              const SizedBox(height: 32),
              if (_saving)
                const Center(child: CircularProgressIndicator())
              else
                AppButton(
                  label: 'Speichern',
                  icon: Icons.save_outlined,
                  onPressed: _save,
                ),
              const SizedBox(height: 32),
              const Center(
                child: PageIndicator(currentPage: 2, totalPages: 5),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}
