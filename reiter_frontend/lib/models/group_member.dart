class GroupMember {
  final String id;
  final String name;
  final String? imageUrl;
  final bool isCurrentUser;
  final bool isOwner;

  GroupMember({
    required this.id,
    required this.name,
    this.imageUrl,
    required this.isCurrentUser,
    required this.isOwner,
  });

  factory GroupMember.fromJson(Map<String, dynamic> json) {
    final firstName =
        json['firstName'] ?? json['firstname'] ?? json['first_name'];
    final lastName = json['lastName'] ?? json['lastname'] ?? json['last_name'];
    final name =
        json['name'] ??
        json['displayName'] ??
        json['display_name'] ??
        [
          firstName,
          lastName,
        ].where((part) => part != null && part.toString().isNotEmpty).join(' ');
    final idValue = json['id'] ?? json['person_id'] ?? json['personId'];
    if (idValue == null || name.toString().trim().isEmpty) {
      throw const FormatException('Invalid device user response');
    }

    return GroupMember(
      id: idValue.toString(),
      name: name.toString(),
      imageUrl: (json['imageUrl'] ?? json['image_url']) as String?,
      isCurrentUser: json['isCurrentUser'] as bool? ?? false,
      isOwner: json['isOwner'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'imageUrl': imageUrl,
      'isCurrentUser': isCurrentUser,
      'isOwner': isOwner,
    };
  }
}
