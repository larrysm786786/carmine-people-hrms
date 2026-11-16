enum PerformanceReviewStatus { pendingSelfReview, pendingManagerReview, completed }

PerformanceReviewStatus _statusFromJson(dynamic value) {
  final index = value is int ? value : int.tryParse(value?.toString() ?? '') ?? 0;
  if (index < 0 || index >= PerformanceReviewStatus.values.length) {
    return PerformanceReviewStatus.pendingSelfReview;
  }
  return PerformanceReviewStatus.values[index];
}

extension PerformanceReviewStatusLabel on PerformanceReviewStatus {
  String get label {
    switch (this) {
      case PerformanceReviewStatus.pendingSelfReview:
        return 'Pending Self-Review';
      case PerformanceReviewStatus.pendingManagerReview:
        return 'Pending Manager Review';
      case PerformanceReviewStatus.completed:
        return 'Completed';
    }
  }
}

class PerformanceReview {
  const PerformanceReview({
    required this.id,
    required this.period,
    required this.selfRating,
    required this.managerRating,
    required this.managerComments,
    required this.status,
  });

  factory PerformanceReview.fromJson(Map<String, dynamic> json) {
    return PerformanceReview(
      id: json['id'] as String,
      period: json['period'] as String,
      selfRating: json['selfRating'] as int?,
      managerRating: json['managerRating'] as int?,
      managerComments: json['managerComments'] as String?,
      status: _statusFromJson(json['status']),
    );
  }

  final String id;
  final String period;
  final int? selfRating;
  final int? managerRating;
  final String? managerComments;
  final PerformanceReviewStatus status;
}
