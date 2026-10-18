class Person {
  final int id;

  final String firstname;
  final String lastname;

  final int heightCm;
  final int weightKg;

  final int plz;
  final String city;
  final String street;
  final int housenumber;

  final String email;
  final String? websiteLink;
  final String? description;
  final DateTime? dob;

  final bool hidden;

  const Person({
    required this.id,
    required this.firstname,
    required this.lastname,
    required this.heightCm,
    required this.weightKg,
    required this.plz,
    required this.city,
    required this.street,
    required this.housenumber,
    required this.email,
    this.websiteLink,
    this.description,
    this.dob,
    this.hidden = false,
  });

  factory Person.fromJson(Map<String, dynamic> json) {
    return Person(
      id: json['person_id'],
      firstname: json['firstname'],
      lastname: json['lastname'],
      heightCm: json['height_cm'],
      weightKg: json['weight_kg'],
      plz: json['plz'],
      city: json['city'],
      street: json['street'],
      housenumber: json['housenumber'],
      email: json['email'],
      websiteLink: json['website_link'],
      description: json['description'],
      dob: json['dob'] != null ? DateTime.parse(json['dob']) : null,
      hidden: json['hidden'] ?? false,
    );
  }

  Map<String, dynamic> toJson() => {
        'person_id': id,
        'firstname': firstname,
        'lastname': lastname,
        'height_cm': heightCm,
        'weight_kg': weightKg,
        'plz': plz,
        'city': city,
        'street': street,
        'housenumber': housenumber,
        'email': email,
        'website_link': websiteLink,
        'description': description,
        'dob': dob?.toIso8601String(),
        'hidden': hidden,
      };

  String get fullName => '$firstname $lastname';
}
