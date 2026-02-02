import 'dart:io';

class Horse {
  final String id;
  final String name;
  final File? image;
  final String? imagePath; 
  final String breed;
  final String birthDate; // Format: DD.MM.YYYY
  final String weight;
  final String height;
  final String sex;
  
  // Stalladresse Felder
  final String? stableCity;
  final String? stablePostalCode;
  final String? stableStreet;
  final String? stableHouseNumber;
  final double? stableLatitude;
  final double? stableLongitude;

  Horse({
    required this.id,
    required this.name,
    this.image,
    this.imagePath,
    required this.breed,
    required this.birthDate,
    required this.weight,
    required this.height,
    required this.sex,
    this.stableCity,
    this.stablePostalCode,
    this.stableStreet,
    this.stableHouseNumber,
    this.stableLatitude,
    this.stableLongitude,
  });

  bool get hasStableLocation => stableLatitude != null && stableLongitude != null;

  /// Berechnet das Alter aus dem Geburtsdatum
  String get age {
    if (birthDate.isEmpty) return '';
    
    try {
      // Parse DD.MM.YYYY
      final parts = birthDate.split('.');
      if (parts.length != 3) return '';
      
      final day = int.parse(parts[0]);
      final month = int.parse(parts[1]);
      final year = int.parse(parts[2]);
      
      final birth = DateTime(year, month, day);
      final now = DateTime.now();
      
      int age = now.year - birth.year;
      
      // Adjust if birthday hasn't occurred yet this year
      if (now.month < birth.month || 
          (now.month == birth.month && now.day < birth.day)) {
        age--;
      }
      
      return '${age}yo';
    } catch (e) {
      return '';
    }
  }

  /// ← NEU: Hilfsmethode für vollständige Adresse
  String get fullStableAddress {
    if (stableStreet == null || stableStreet!.isEmpty) {
      return '-';
    }
    
    final street = '${stableStreet ?? ''} ${stableHouseNumber ?? ''}'.trim();
    final cityLine = '${stablePostalCode ?? ''} ${stableCity ?? ''}'.trim();
    
    if (cityLine.isEmpty) {
      return street;
    }
    
    return '$street\n$cityLine';
  }

  /// ← NEU: Prüft ob Adresse vorhanden ist
  bool get hasStableAddress {
    return (stableStreet != null && stableStreet!.isNotEmpty) ||
           (stableCity != null && stableCity!.isNotEmpty);
  }

  /// Erstellt ein Horse-Objekt aus JSON
  factory Horse.fromJson(Map<String, dynamic> json) {
    return Horse(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      imagePath: json['imagePath'],
      image: json['imagePath'] != null ? File(json['imagePath']) : null,
      breed: json['breed'] ?? 'Unbekannt',
      birthDate: json['birthDate'] ?? '',
      weight: json['weight'] ?? '',
      height: json['height'] ?? '',
      sex: json['sex'] ?? 'female',
      stableCity: json['stableCity'],
      stablePostalCode: json['stablePostalCode'],
      stableStreet: json['stableStreet'],
      stableHouseNumber: json['stableHouseNumber'],
      stableLatitude: json['stableLatitude']?.toDouble(),
      stableLongitude: json['stableLongitude']?.toDouble(),
    );
  }

  /// Konvertiert ein Horse-Objekt zu JSON
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'imagePath': imagePath ?? image?.path,
      'breed': breed,
      'birthDate': birthDate,
      'weight': weight,
      'height': height,
      'sex': sex,
      'stableCity': stableCity,
      'stablePostalCode': stablePostalCode,
      'stableStreet': stableStreet,
      'stableHouseNumber': stableHouseNumber,
      'stableLatitude': stableLatitude,
      'stableLongitude': stableLongitude,
    };
  }

  /// Erstellt eine Kopie mit geänderten Werten
  Horse copyWith({
    String? id,
    String? name,
    File? image,
    String? imagePath,
    String? breed,
    String? birthDate,
    String? weight,
    String? height,
    String? sex,
    String? stableCity,
    String? stablePostalCode,
    String? stableStreet,
    String? stableHouseNumber,
    double? stableLatitude,
    double? stableLongitude,
  }) {
    return Horse(
      id: id ?? this.id,
      name: name ?? this.name,
      image: image ?? this.image,
      imagePath: imagePath ?? this.imagePath,
      breed: breed ?? this.breed,
      birthDate: birthDate ?? this.birthDate,
      weight: weight ?? this.weight,
      height: height ?? this.height,
      sex: sex ?? this.sex,
      stableCity: stableCity ?? this.stableCity,
      stablePostalCode: stablePostalCode ?? this.stablePostalCode,
      stableStreet: stableStreet ?? this.stableStreet,
      stableHouseNumber: stableHouseNumber ?? this.stableHouseNumber,
      stableLatitude: stableLatitude ?? this.stableLatitude,
      stableLongitude: stableLongitude ?? this.stableLongitude,
    );
  }
}