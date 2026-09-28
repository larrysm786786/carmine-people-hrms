class AnnouncementComment {
  const AnnouncementComment({
    required this.id,
    required this.employeeName,
    required this.body,
    required this.createdAt,
  });

  factory AnnouncementComment.fromJson(Map<String, dynamic> json) {
    return AnnouncementComment(
      id: json['id'] as String,
      employeeName: json['employeeName'] as String? ?? '',
      body: json['body'] as String? ?? '',
      createdAt: json['createdAt'] as String? ?? '',
    );
  }

  final String id;
  final String employeeName;
  final String body;
  final String createdAt;
}
