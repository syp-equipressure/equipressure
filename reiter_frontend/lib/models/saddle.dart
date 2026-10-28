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

class SaddleCategory {
  final int id;
  final String name;

  SaddleCategory({
    required this.id,
    required this.name,
  });

  factory SaddleCategory.fromJson(Map<String, dynamic> json) {
    return SaddleCategory(
      id: (json['id'] as num).toInt(),
      name: (json['name'] ?? '').toString(),
    );
  }
}