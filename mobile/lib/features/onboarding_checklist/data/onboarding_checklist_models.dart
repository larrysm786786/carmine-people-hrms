class OnboardingChecklistItem {
  const OnboardingChecklistItem({
    required this.id,
    required this.title,
    required this.description,
    required this.isCompleted,
  });

  factory OnboardingChecklistItem.fromJson(Map<String, dynamic> json) {
    return OnboardingChecklistItem(
      id: json['id'] as String,
      title: json['title'] as String,
      description: json['description'] as String?,
      isCompleted: json['isCompleted'] as bool? ?? false,
    );
  }

  final String id;
  final String title;
  final String? description;
  final bool isCompleted;
}
