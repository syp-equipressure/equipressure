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
    return GroupMember(
      id: json['id'] as String,
      name: json['name'] as String,
      imageUrl: json['imageUrl'] as String?,
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
