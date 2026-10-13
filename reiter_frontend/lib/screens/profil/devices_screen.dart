import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:reiterappfrontend/models/device.dart';
import 'package:reiterappfrontend/services/device_service.dart';
import 'device_group_screen.dart';

class DevicesScreen extends StatefulWidget {
  final List<Device> devices;
  final String ownerId;

  const DevicesScreen({
    super.key,
    required this.devices,
    required this.ownerId,
  });

  @override
  State<DevicesScreen> createState() => _DevicesScreenState();
}

class _DevicesScreenState extends State<DevicesScreen> {
  int? expandedIndex;

  void _scanQRCode() {
    Navigator.of(context)
        .push(
          MaterialPageRoute(
            builder: (context) => _QRCodeScannerScreen(
              onCodeScanned: (code) {
                Navigator.pop(context, code);
              },
            ),
          ),
        )
        .then((result) {
          if (result != null) {
            _handleScannedCode(result);
          }
        });
  }

  Future<void> _handleScannedCode(String code) async {
    final categoryController = TextEditingController();
    final formKey = GlobalKey<FormState>();
    final categoryId = await showDialog<int>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Gerät hinzufügen'),
        content: Form(
          key: formKey,
          child: TextFormField(
            controller: categoryController,
            autofocus: true,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Gerätekategorie-ID',
              helperText: 'Das Backend bietet keine Kategorienliste an.',
            ),
            validator: (value) {
              final id = int.tryParse(value?.trim() ?? '');
              return id == null || id <= 0
                  ? 'Bitte eine positive Kategorien-ID eingeben.'
                  : null;
            },
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Abbrechen'),
          ),
          FilledButton(
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(
                  context,
                  int.parse(categoryController.text.trim()),
                );
              }
            },
            child: const Text('Hinzufügen'),
          ),
        ],
      ),
    );
    categoryController.dispose();
    if (categoryId == null || !mounted) return;

    try {
      final device = await DeviceService.addDevice(
        deviceId: code,
        ownerId: int.parse(widget.ownerId),
        categoryId: categoryId,
      );
      if (!mounted) return;
      setState(() => widget.devices.add(device));
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Gerät wurde hinzugefügt.')));
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Gerät konnte nicht hinzugefügt werden: $error'),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'Meine Satteldruckmessgeräte',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w500,
            fontSize: 16,
          ),
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: List.generate(
          widget.devices.length,
          (index) => Column(
            children: [
              _buildDeviceItem(index: index, device: widget.devices[index]),
              if (index < widget.devices.length - 1)
                Divider(height: 1, color: Colors.grey[300]),
            ],
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _scanQRCode,
        backgroundColor: const Color.fromARGB(255, 178, 149, 230),
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildDeviceItem({required int index, required Device device}) {
    final isExpanded = expandedIndex == index;

    return Column(
      children: [
        InkWell(
          onTap: () {
            setState(() {
              expandedIndex = isExpanded ? null : index;
            });
          },
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            child: Row(
              children: [
                Expanded(
                  child: RichText(
                    text: TextSpan(
                      style: const TextStyle(fontSize: 15, color: Colors.black),
                      children: [
                        const TextSpan(
                          text: 'Gerät: ',
                          style: TextStyle(fontWeight: FontWeight.w600),
                        ),
                        TextSpan(
                          text: device.name,
                          style: const TextStyle(fontWeight: FontWeight.w400),
                        ),
                      ],
                    ),
                  ),
                ),
                Icon(
                  isExpanded ? Icons.keyboard_arrow_up : Icons.chevron_right,
                  color: Colors.black54,
                  size: 24,
                ),
              ],
            ),
          ),
        ),
        if (isExpanded)
          Container(
            color: Colors.grey[50],
            padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Gerätenummer
                _buildInfoRow('Gerätenummer:', device.number),
                const SizedBox(height: 16),

                // Gerätegruppe
                InkWell(
                  onTap: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => DeviceGroupScreen(
                          deviceId: device.id,
                          deviceName: device.name,
                        ),
                      ),
                    );
                  },
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        const Text(
                          'Gerätegruppe',
                          style: TextStyle(
                            fontWeight: FontWeight.w600,
                            fontSize: 15,
                          ),
                        ),
                        const Spacer(),
                        Icon(
                          Icons.chevron_right,
                          color: Colors.black54,
                          size: 20,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 24),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(fontSize: 15, color: Colors.black87),
          ),
        ),
      ],
    );
  }
}

class _QRCodeScannerScreen extends StatefulWidget {
  final Function(String) onCodeScanned;

  const _QRCodeScannerScreen({required this.onCodeScanned});

  @override
  State<_QRCodeScannerScreen> createState() => _QRCodeScannerScreenState();
}

class _QRCodeScannerScreenState extends State<_QRCodeScannerScreen> {
  late MobileScannerController _controller;

  @override
  void initState() {
    super.initState();
    _controller = MobileScannerController();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.white),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          'QR-Code scannen',
          style: TextStyle(color: Colors.white),
        ),
      ),
      body: MobileScanner(
        controller: _controller,
        onDetect: (capture) {
          final List<Barcode> barcodes = capture.barcodes;
          if (barcodes.isNotEmpty) {
            final barcode = barcodes.first;
            final code = barcode.rawValue ?? '';
            if (code.isNotEmpty) {
              widget.onCodeScanned(code);
            }
          }
        },
        errorBuilder: (context, error) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.camera_alt, color: Colors.white, size: 48),
                const SizedBox(height: 16),
                const Text(
                  'Kamera wird benötigt',
                  style: TextStyle(color: Colors.white, fontSize: 16),
                ),
                const SizedBox(height: 24),
                ElevatedButton(
                  onPressed: () {
                    Navigator.pop(context);
                  },
                  child: const Text('Zurück'),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
