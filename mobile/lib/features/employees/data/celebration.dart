class Celebration {
  const Celebration({
    required this.employeeId,
    required this.employeeName,
    this.profileImageUrl,
    required this.date,
    required this.kind,
    required this.yearsCount,
  });

  factory Celebration.fromJson(Map<String, dynamic> json) {
    return Celebration(
      employeeId: json['employeeId'] as String,
      employeeName: json['employeeName'] as String,
      profileImageUrl: json['profileImageUrl'] as String?,
      date: json['date'] as String,
      kind: json['kind'] as String,
      yearsCount: json['yearsCount'] as int? ?? 0,
    );
  }

  final String employeeId;
  final String employeeName;
  final String? profileImageUrl;
  final String date;
  final String kind;
  final int yearsCount;

  bool get isBirthday => kind == 'Birthday';
}
