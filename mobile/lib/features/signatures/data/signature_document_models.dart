class SignatureDocument {
  const SignatureDocument({required this.id, required this.title, required this.content});

  factory SignatureDocument.fromJson(Map<String, dynamic> json) {
    return SignatureDocument(
      id: json['id'] as String,
      title: json['title'] as String,
      content: json['content'] as String,
    );
  }

  final String id;
  final String title;
  final String content;
}
