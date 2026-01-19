class Measurement {
  final String id;
  final String horseId;
  final String horseName;
  final DateTime date;
  final String owner;
  final String rider;
  final String weight;
  final String height;
  final String saddleName;
  final String notes;
  final MeasurementImages images;

  Measurement({
    required this.id,
    required this.horseId,
    required this.horseName,
    required this.date,
    required this.owner,
    required this.rider,
    required this.weight,
    required this.height,
    required this.saddleName,
    required this.notes,
    required this.images,
  });

  factory Measurement.fromJson(Map<String, dynamic> json) {
    return Measurement(
      id: json['id'],
      horseId: json['horseId'],
      horseName: json['horseName'],
      date: DateTime.parse(json['date']),
      owner: json['owner'],
      rider: json['rider'],
      weight: json['weight'],
      height: json['height'],
      saddleName: json['saddleName'],
      notes: json['notes'],
      images: MeasurementImages.fromJson(json['images']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'horseId': horseId,
      'horseName': horseName,
      'date': date.toIso8601String(),
      'owner': owner,
      'rider': rider,
      'weight': weight,
      'height': height,
      'saddleName': saddleName,
      'notes': notes,
      'images': images.toJson(),
    };
  }
}

class MeasurementImages {
  final String normal;
  final String filtered;
  final String profile;

  MeasurementImages({
    required this.normal,
    required this.filtered,
    required this.profile,
  });

  factory MeasurementImages.fromJson(Map<String, dynamic> json) {
    return MeasurementImages(
      normal: json['normal'],
      filtered: json['filtered'],
      profile: json['profile'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'normal': normal,
      'filtered': filtered,
      'profile': profile,
    };
  }
}
