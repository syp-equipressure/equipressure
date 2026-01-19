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
      id: json['id'],
      name: json['name'],
      category: json['category'],
      horseId: json['horseId']
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