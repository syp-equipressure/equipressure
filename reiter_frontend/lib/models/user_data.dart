class UserData {
  final String id;
  final String name;
  final String imageUrl;
  final String birthDate;
  final String address;
  final String email;
  final String height;
  final String weight;

  // die einzelnen Werte zum Bearbeiten
  final String firstName;
  final String lastName;
  final String street;
  final String postalCode;
  final String city;
  final String heightCm;
  final String weightKg;

  /// Wann das Profilbild im Backend gesetzt wurde, null ohne Bild
  final String? imageUpdatedAt;

  UserData({
    required this.id,
    required this.name,
    required this.imageUrl,
    required this.birthDate,
    required this.address,
    required this.email,
    required this.height,
    required this.weight,
    this.firstName = '',
    this.lastName = '',
    this.street = '',
    this.postalCode = '',
    this.city = '',
    this.heightCm = '',
    this.weightKg = '',
    this.imageUpdatedAt,
  });

  factory UserData.fromJson(Map<String, dynamic> json) {
    return UserData(
      id: json['id'] as String,
      name: json['name'] as String,
      imageUrl: json['imageUrl'] as String,
      birthDate: json['birthDate'] as String,
      address: json['address'] as String,
      email: json['email'] as String,
      height: json['height'] as String,
      weight: json['weight'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'imageUrl': imageUrl,
      'birthDate': birthDate,
      'address': address,
      'email': email,
      'height': height,
      'weight': weight,
    };
  }
}
