import 'package:reiterappfrontend/theme/app_colors.dart';
import 'dart:ui' as ui;
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_map_cancellable_tile_provider/flutter_map_cancellable_tile_provider.dart';
import 'package:latlong2/latlong.dart' show LatLng;
import 'package:url_launcher/url_launcher.dart';
import '../../models/saddler.dart';
import '../../services/saddler_service.dart';
import '../../services/chat_service.dart';
import '../saddler_contact/chat_screen.dart';

class SaddlerProfileScreen extends StatefulWidget {
  final Saddler saddler;

  /// Aus dem Chat geöffnet: "Chat" führt dann zurück statt einen zweiten Chat zu öffnen.
  final bool openedFromChat;

  const SaddlerProfileScreen({
    super.key,
    required this.saddler,
    this.openedFromChat = false,
  });

  @override
  State<SaddlerProfileScreen> createState() => _SaddlerProfileScreenState();
}

class _SaddlerProfileScreenState extends State<SaddlerProfileScreen> {
  final SaddlerService _saddlerService = SaddlerService();
  final ChatService _chatService = ChatService();
  late Saddler _saddler;

  @override
  void initState() {
    super.initState();
    _saddler = widget.saddler;
  }

  Future<void> _toggleFavorite() async {
    await _saddlerService.toggleFavorite(_saddler.id);
    final updated = await _saddlerService.getSaddlerById(_saddler.id);
    if (updated != null) {
      setState(() {
        _saddler = updated;
      });
    }
  }

  Future<void> _launchUrl(String url) async {
    final uri = Uri.parse(url);
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }

  void _startChat() {
    if (widget.openedFromChat) {
      Navigator.pop(context);
      return;
    }
    // der Chat entsteht im Backend mit der ersten Nachricht
    final chat = _chatService.chatWith(_saddler.id, _saddler.name);
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ChatScreen(chat: chat),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      appBar: AppBar(
        backgroundColor: AppColors.surface,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textStrong),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: Icon(
              _saddler.isFavorite ? Icons.favorite : Icons.favorite_border,
              color: _saddler.isFavorite ? AppColors.error : AppColors.textSoft,
            ),
            onPressed: _toggleFavorite,
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildHeader(),
            _buildInfoSection(),
            if (_saddler.hasLocation) _buildMapPreview(),
            const SizedBox(height: 100),
          ],
        ),
      ),
      bottomNavigationBar: _buildContactButton(),
    );
  }

  Widget _buildHeader() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          // Logo/Avatar
          Container(
            width: 100,
            height: 100,
            decoration: BoxDecoration(
              color: AppColors.successLight,
              shape: BoxShape.circle,
              border: Border.all(
                color: AppColors.successLight,
                width: 2,
              ),
            ),
            child: _saddler.logoPath != null
                ? ClipOval(
                    child: Image.asset(
                      _saddler.logoPath!,
                      fit: BoxFit.cover,
                    ),
                  )
                : Icon(
                    Icons.eco,
                    size: 50,
                    color: AppColors.success,
                  ),
          ),
          const SizedBox(height: 16),
          Text(
            _saddler.name,
            style: const TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.bold,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  Widget _buildInfoSection() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Address
          _buildInfoRow(
            icon: Icons.location_on_outlined,
            label: _saddler.shortAddress,
          ),
          const SizedBox(height: 12),

          // Website
          if (_saddler.website != null)
            InkWell(
              onTap: () => _launchUrl(_saddler.website!),
              child: _buildInfoRow(
                icon: Icons.language,
                label: _formatWebsite(_saddler.website!),
                isLink: true,
              ),
            ),
          const SizedBox(height: 12),

          // Brands
          if (_saddler.allBrands || _saddler.brands.isNotEmpty) ...[
            _buildInfoRow(
              icon: Icons.sell_outlined,
              label: _saddler.allBrands ? 'Alle Marken' : _saddler.brands.join(', '),
            ),
            const SizedBox(height: 12),
          ],

          const SizedBox(height: 8),

          // Description
          if (_saddler.description != null) ...[
            const Text(
              'Beschreibung:',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              _saddler.description!,
              style: TextStyle(
                fontSize: 14,
                color: AppColors.text,
                height: 1.5,
              ),
            ),
            const SizedBox(height: 24),
          ],
        ],
      ),
    );
  }

  Widget _buildInfoRow({
    required IconData icon,
    required String label,
    bool isLink = false,
  }) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(
          icon,
          size: 20,
          color: AppColors.textSoft,
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Text(
            label,
            style: TextStyle(
              fontSize: 14,
              color: isLink ? AppColors.primary : AppColors.text,
              decoration: isLink ? TextDecoration.underline : null,
            ),
          ),
        ),
      ],
    );
  }

  String _formatWebsite(String url) {
    return url
        .replaceFirst('https://', '')
        .replaceFirst('http://', '')
        .replaceFirst('www.', '');
  }

  Widget _buildMapPreview() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Standort',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: SizedBox(
              height: 180,
              child: FlutterMap(
                options: MapOptions(
                  initialCenter: LatLng(_saddler.latitude!, _saddler.longitude!),
                  initialZoom: 14.0,
                  interactionOptions: const InteractionOptions(
                    flags: InteractiveFlag.none,
                  ),
                ),
                children: [
                  TileLayer(
                    urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                    userAgentPackageName: 'com.equipressure.app',
                    tileProvider: CancellableNetworkTileProvider(),
                  ),
                  MarkerLayer(
                    markers: [
                      Marker(
                        point: LatLng(_saddler.latitude!, _saddler.longitude!),
                        width: 40,
                        height: 50,
                        child: Column(
                          children: [
                            Container(
                              width: 36,
                              height: 36,
                              decoration: BoxDecoration(
                                color: AppColors.error,
                                shape: BoxShape.circle,
                                border: Border.all(color: AppColors.surface, width: 2),
                              ),
                              child: const Icon(
                                Icons.location_on,
                                color: AppColors.surface,
                                size: 20,
                              ),
                            ),
                            CustomPaint(
                              size: const Size(10, 7),
                              painter: _MarkerTrianglePainter(
                                color: AppColors.error,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContactButton() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        boxShadow: [
          BoxShadow(
            color: AppColors.shadow(0.05),
            blurRadius: 10,
            offset: const Offset(0, -4),
          ),
        ],
      ),
      child: SafeArea(
        child: ElevatedButton(
          onPressed: _startChat,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: AppColors.onPrimary,
            padding: const EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            elevation: 0,
          ),
          child: const Text(
            'Kontaktieren',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ),
    );
  }
}

class _MarkerTrianglePainter extends CustomPainter {
  final Color color;

  _MarkerTrianglePainter({required this.color});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.fill;

    final path = ui.Path()
      ..moveTo(size.width / 2, size.height)
      ..lineTo(0, 0)
      ..lineTo(size.width, 0)
      ..close();

    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
