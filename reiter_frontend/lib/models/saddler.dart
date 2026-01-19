class Saddler {
  final String id;
  final String name;
  final String? imagePath;

  Saddler({
    required this.id,
    required this.name,
    this.imagePath,
  });

  factory Saddler.fromJson(Map<String, dynamic> json) {
    return Saddler(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      imagePath: json['imagePath'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'imagePath': imagePath,
    };
  }
}
