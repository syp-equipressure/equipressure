import 'package:flutter/material.dart';
import '../models/horse.dart';

class HorseChatCard extends StatefulWidget {
  final Horse horse;

  const HorseChatCard({
    super.key,
    required this.horse,
  });

  @override
  State<HorseChatCard> createState() => _HorseChatCardState();
}

class _HorseChatCardState extends State<HorseChatCard> {
  bool _isExpanded = false;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFF8E1F4),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          // Main card content
          InkWell(
            onTap: () {
              setState(() {
                _isExpanded = !_isExpanded;
              });
            },
            borderRadius: BorderRadius.circular(12),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.horse.name,
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          '${widget.horse.age} | ${widget.horse.breed}',
                          style: TextStyle(
                            color: Colors.grey[700],
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Icon(
                    _isExpanded
                        ? Icons.keyboard_arrow_up
                        : Icons.keyboard_arrow_down,
                    color: Colors.grey[700],
                  ),
                ],
              ),
            ),
          ),
          // Expanded content
          if (_isExpanded) ...[
            Divider(
              height: 1,
              color: Colors.purple[100],
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  _buildInfoRow('Rasse', widget.horse.breed),
                  const SizedBox(height: 8),
                  _buildInfoRow('Geburtsdatum', widget.horse.birthDate),
                  const SizedBox(height: 8),
                  _buildInfoRow('Gewicht', '${widget.horse.weight} kg'),
                  const SizedBox(height: 8),
                  _buildInfoRow('Stockmaß', '${widget.horse.height} cm'),
                  const SizedBox(height: 8),
                  _buildInfoRow(
                    'Geschlecht',
                    widget.horse.sex == 'male' ? 'Hengst/Wallach' : 'Stute',
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            color: Colors.grey[600],
            fontSize: 13,
          ),
        ),
        Text(
          value,
          style: const TextStyle(
            fontWeight: FontWeight.w500,
            fontSize: 13,
          ),
        ),
      ],
    );
  }
}
