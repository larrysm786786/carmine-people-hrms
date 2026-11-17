class EmployeeDirectoryItem {
  const EmployeeDirectoryItem({required this.id, required this.firstName, required this.lastName});

  factory EmployeeDirectoryItem.fromJson(Map<String, dynamic> json) {
    return EmployeeDirectoryItem(
      id: json['id'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
    );
  }

  final String id;
  final String firstName;
  final String lastName;

  String get fullName => '$firstName $lastName';
}
