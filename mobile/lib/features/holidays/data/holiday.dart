class Holiday {
  const Holiday({required this.id, required this.name, required this.date});

  factory Holiday.fromJson(Map<String, dynamic> json) {
    return Holiday(
      id: json['id'] as String,
      name: json['name'] as String,
      date: json['date'] as String,
    );
  }

  final String id;
  final String name;
  final String date;
}
