import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import '../models/horse.dart';
import '../models/measurement.dart';
import '../services/measurement_service.dart';

/// Wählt eine einzelne Messung eines Pferdes zum Teilen aus.
class MeasurementSelectionDialog extends StatefulWidget {
  final List<Horse> horses;
  final ValueChanged<Measurement> onMeasurementSelected;

  const MeasurementSelectionDialog({
    super.key,
    required this.horses,
    required this.onMeasurementSelected,
  });

  @override
  State<MeasurementSelectionDialog> createState() =>
      _MeasurementSelectionDialogState();
}

class _MeasurementSelectionDialogState
    extends State<MeasurementSelectionDialog> {
  final MeasurementService _measurementService = MeasurementService();

  Horse? _horse;
  List<Measurement> _measurements = [];
  bool _isLoading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.horses.length == 1) _selectHorse(widget.horses.first);
  }

  Future<void> _selectHorse(Horse horse) async {
    setState(() {
      _horse = horse;
      _measurements = [];
      _isLoading = true;
      _error = null;
    });
    try {
      final measurements =
          await _measurementService.getMeasurementsForHorse(horse.id);
      if (!mounted || _horse != horse) return;
      setState(() {
        _measurements = measurements;
        _isLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Messungen konnten nicht geladen werden.';
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: AppColors.surface,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Container(
        constraints: const BoxConstraints(maxWidth: 400, maxHeight: 520),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Messung auswählen',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
              child: DropdownButtonFormField<Horse>(
                initialValue: _horse,
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Pferd',
                  border: OutlineInputBorder(),
                ),
                items: widget.horses
                    .map((horse) => DropdownMenuItem(
                          value: horse,
                          child: Text(horse.name, overflow: TextOverflow.ellipsis),
                        ))
                    .toList(),
                onChanged: (horse) {
                  if (horse != null) _selectHorse(horse);
                },
              ),
            ),
            const Divider(height: 1),
            Flexible(child: _buildList()),
          ],
        ),
      ),
    );
  }

  Widget _buildList() {
    if (_horse == null) {
      return _buildHint('Wähle zuerst ein Pferd aus.');
    }
    if (_isLoading) {
      return const Padding(
        padding: EdgeInsets.all(24),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_error != null) return _buildHint(_error!);
    if (_measurements.isEmpty) {
      return _buildHint('${_horse!.name} hat noch keine Messungen.');
    }

    return ListView.separated(
      shrinkWrap: true,
      itemCount: _measurements.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final measurement = _measurements[index];
        final date = measurement.date;
        return ListTile(
          leading: const Icon(Icons.analytics_outlined),
          title: Text('Messung vom ${date.day}.${date.month}.${date.year}'),
          subtitle: Text(
            [measurement.rider, measurement.saddleName]
                .where((part) => part.isNotEmpty)
                .join(' · '),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          trailing: const Icon(Icons.send, size: 20),
          onTap: () {
            Navigator.pop(context);
            widget.onMeasurementSelected(measurement);
          },
        );
      },
    );
  }

  Widget _buildHint(String text) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: TextStyle(color: AppColors.textSoft),
      ),
    );
  }
}
