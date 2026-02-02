class Saddler {
  final String id;
  final String name;
  final String? imagePath;
  final String? logoPath;
  final String? street;
  final String? houseNumber;
  final String? postalCode;
  final String? city;
  final double? latitude;
  final double? longitude;
  final String? website;
  final String? email;
  final String? phone;
  final String? description;
  final bool isFavorite;

  Saddler({
    required this.id,
    required this.name,
    this.imagePath,
    this.logoPath,
    this.street,
    this.houseNumber,
    this.postalCode,
    this.city,
    this.latitude,
    this.longitude,
    this.website,
    this.email,
    this.phone,
    this.description,
    this.isFavorite = false,
  });

  String get fullAddress {
    final parts = <String>[];
    if (street != null) {
      parts.add(houseNumber != null ? '$street $houseNumber' : street!);
    }
    if (postalCode != null && city != null) {
      parts.add('$postalCode $city');
    } else if (city != null) {
      parts.add(city!);
    }
    return parts.join(', ');
  }

  String get shortAddress {
    if (street != null && postalCode != null && city != null) {
      return '$street ${houseNumber ?? ''}, $postalCode $city';
    }
    return fullAddress;
  }

  bool get hasLocation => latitude != null && longitude != null;

  Saddler copyWith({
    String? id,
    String? name,
    String? imagePath,
    String? logoPath,
    String? street,
    String? houseNumber,
    String? postalCode,
    String? city,
    double? latitude,
    double? longitude,
    String? website,
    String? email,
    String? phone,
    String? description,
    bool? isFavorite,
  }) {
    return Saddler(
      id: id ?? this.id,
      name: name ?? this.name,
      imagePath: imagePath ?? this.imagePath,
      logoPath: logoPath ?? this.logoPath,
      street: street ?? this.street,
      houseNumber: houseNumber ?? this.houseNumber,
      postalCode: postalCode ?? this.postalCode,
      city: city ?? this.city,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      website: website ?? this.website,
      email: email ?? this.email,
      phone: phone ?? this.phone,
      description: description ?? this.description,
      isFavorite: isFavorite ?? this.isFavorite,
    );
  }

  factory Saddler.fromJson(Map<String, dynamic> json) {
    return Saddler(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      imagePath: json['imagePath'],
      logoPath: json['logoPath'],
      street: json['street'],
      houseNumber: json['houseNumber'],
      postalCode: json['postalCode'],
      city: json['city'],
      latitude: json['latitude']?.toDouble(),
      longitude: json['longitude']?.toDouble(),
      website: json['website'],
      email: json['email'],
      phone: json['phone'],
      description: json['description'],
      isFavorite: json['isFavorite'] ?? false,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'imagePath': imagePath,
      'logoPath': logoPath,
      'street': street,
      'houseNumber': houseNumber,
      'postalCode': postalCode,
      'city': city,
      'latitude': latitude,
      'longitude': longitude,
      'website': website,
      'email': email,
      'phone': phone,
      'description': description,
      'isFavorite': isFavorite,
    };
  }
}
