enum AttendanceStatus { present, absent, late, halfDay, leave, holiday, weekOff, wfh, pendingRegularization }

AttendanceStatus _statusFromJson(dynamic value) {
  final index = value is int ? value : int.tryParse(value?.toString() ?? '') ?? 0;
  if (index < 0 || index >= AttendanceStatus.values.length) return AttendanceStatus.present;
  return AttendanceStatus.values[index];
}

extension AttendanceStatusLabel on AttendanceStatus {
  String get label {
    switch (this) {
      case AttendanceStatus.present:
        return 'Present';
      case AttendanceStatus.absent:
        return 'Absent';
      case AttendanceStatus.late:
        return 'Late';
      case AttendanceStatus.halfDay:
        return 'Half Day';
      case AttendanceStatus.leave:
        return 'Leave';
      case AttendanceStatus.holiday:
        return 'Holiday';
      case AttendanceStatus.weekOff:
        return 'Week Off';
      case AttendanceStatus.wfh:
        return 'WFH';
      case AttendanceStatus.pendingRegularization:
        return 'Pending Regularization';
    }
  }
}

class AttendanceRecord {
  const AttendanceRecord({
    required this.id,
    required this.date,
    this.checkInAt,
    this.checkOutAt,
    required this.status,
    this.isCheckedIn = false,
  });

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) {
    return AttendanceRecord(
      id: json['id'] as String,
      date: json['date'] as String,
      checkInAt: json['checkInAt'] as String?,
      checkOutAt: json['checkOutAt'] as String?,
      status: _statusFromJson(json['status']),
      isCheckedIn: json['isCheckedIn'] as bool? ?? false,
    );
  }

  final String id;
  final String date;
  // First check-in of the day — stays fixed even after re-entering the office later today.
  final String? checkInAt;
  // Most recent check-out of the day — keeps advancing until the final check-out.
  final String? checkOutAt;
  final AttendanceStatus status;
  final bool isCheckedIn;
}
