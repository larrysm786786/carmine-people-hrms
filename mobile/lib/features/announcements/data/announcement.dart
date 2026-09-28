class Announcement {
  const Announcement({required this.id, required this.title, required this.body, required this.publishAt});

  factory Announcement.fromJson(Map<String, dynamic> json) {
    return Announcement(
      id: json['id'] as String,
      title: json['title'] as String,
      body: json['body'] as String? ?? '',
      publishAt: json['publishAt'] as String? ?? '',
    );
  }

  final String id;
  final String title;
  final String body;
  final String publishAt;
}
