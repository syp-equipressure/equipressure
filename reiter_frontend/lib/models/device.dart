class Device {
  final String id;
  final String name;
  final String number;

  Device({
    required this.id,
    required this.name,
    required this.number,
  });

  factory Device.fromJson(Map<String, dynamic> json) {
    return Device(
      id: json['id'] as String,
      name: json['name'] as String,
      number: json['number'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'number': number,
    };
  }
}
