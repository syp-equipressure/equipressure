class Saddle {
  final String id;
  final String name;
  final String category;
  final String horseId;

  Saddle({
    required this.id,
    required this.name,
    required this.category,
    required this.horseId
  });

  factory Saddle.fromJson(Map<String, dynamic> json) {
    return Saddle(
      id: (json['id'] ?? '').toString(),
      name: (json['name'] ?? '').toString(),
      category: (json['categoryName'] ?? json['category'] ?? '').toString(),
      horseId: (json['horseId'] ?? '').toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'category': category,
      'horseId' : horseId
    };
  }
}