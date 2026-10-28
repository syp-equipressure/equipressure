class Device {
  final String id;
  final String name;
  final String number;

  Device({required this.id, required this.name, required this.number});

  factory Device.fromJson(Map<String, dynamic> json) {
    final rawId = json['id'] ?? json['deviceId'] ?? json['device_id'];
    if (rawId == null) {
      throw const FormatException('Invalid device response: missing device ID');
    }
    final id = rawId.toString();
    final number =
        json['number'] ?? json['serialNumber'] ?? json['serial_number'] ?? id;
    final name =
        json['name'] ??
        json['deviceName'] ??
        json['device_name'] ??
        json['categoryName'] ??
        json['category_name'] ??
        'Satteldruckmessgerät';

    return Device(id: id, name: name.toString(), number: number.toString());
  }

  Map<String, dynamic> toJson() {
    return {'id': id, 'name': name, 'number': number};
  }
}

class DeviceCategory {
  final int id;
  final String name;
  /// Wie viele Personen das Gerät außer dem Eigentümer nutzen dürfen
  final int numOfAllowedPeople;

  DeviceCategory({
    required this.id,
    required this.name,
    required this.numOfAllowedPeople,
  });

  factory DeviceCategory.fromJson(Map<String, dynamic> json) {
    return DeviceCategory(
      id: (json['id'] as num).toInt(),
      name: (json['name'] ?? '').toString(),
      numOfAllowedPeople: (json['numOfAllowedPeople'] as num?)?.toInt() ?? 0,
    );
  }
}
