class PulseSurvey {
  const PulseSurvey({required this.id, required this.question});

  factory PulseSurvey.fromJson(Map<String, dynamic> json) {
    return PulseSurvey(id: json['id'] as String, question: json['question'] as String);
  }

  final String id;
  final String question;
}
