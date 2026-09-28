enum OvertimeRequestStatus { pending, approved, rejected, cancelled }

OvertimeRequestStatus _statusFromJson(dynamic value) {
  final index = value is int ? value : int.tryParse(value?.toString() ?? '') ?? 0;
  if (index < 0 || index >= OvertimeRequestStatus.values.length) return OvertimeRequestStatus.pending;
  return OvertimeRequestStatus.values[index];
}

extension OvertimeRequestStatusLabel on OvertimeRequestStatus {
  String get label {
    switch (this) {
      case OvertimeRequestStatus.pending:
        return 'Pending';
      case OvertimeRequestStatus.approved:
        return 'Approved';
      case OvertimeRequestStatus.rejected:
        return 'Rejected';
      case OvertimeRequestStatus.cancelled:
        return 'Cancelled';
    }
  }
}

class OvertimeRequestItem {
  const OvertimeRequestItem({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.date,
    required this.hours,
    this.reason,
    required this.status,
    this.decisionComment,
  });

  factory OvertimeRequestItem.fromJson(Map<String, dynamic> json) {
    return OvertimeRequestItem(
      id: json['id'] as String,
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      date: json['date'] as String,
      hours: (json['hours'] as num).toDouble(),
      reason: json['reason'] as String?,
      status: _statusFromJson(json['status']),
      decisionComment: json['decisionComment'] as String?,
    );
  }

  final String id;
  final String employeeId;
  final String employeeName;
  final String date;
  final double hours;
  final String? reason;
  final OvertimeRequestStatus status;
  final String? decisionComment;

  bool get isPending => status == OvertimeRequestStatus.pending;
}
