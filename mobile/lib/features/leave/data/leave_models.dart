enum LeaveApplicationStatus { pending, managerApproved, approved, rejected, cancelled }

LeaveApplicationStatus _statusFromJson(dynamic value) {
  final index = value is int ? value : int.tryParse(value?.toString() ?? '') ?? 0;
  if (index < 0 || index >= LeaveApplicationStatus.values.length) {
    return LeaveApplicationStatus.pending;
  }
  return LeaveApplicationStatus.values[index];
}

extension LeaveApplicationStatusLabel on LeaveApplicationStatus {
  String get label {
    switch (this) {
      case LeaveApplicationStatus.pending:
        return 'Pending';
      case LeaveApplicationStatus.managerApproved:
        return 'Manager Approved';
      case LeaveApplicationStatus.approved:
        return 'Approved';
      case LeaveApplicationStatus.rejected:
        return 'Rejected';
      case LeaveApplicationStatus.cancelled:
        return 'Cancelled';
    }
  }
}

class LeaveType {
  const LeaveType({required this.id, required this.name});

  factory LeaveType.fromJson(Map<String, dynamic> json) {
    return LeaveType(id: json['id'] as String, name: json['name'] as String);
  }

  final String id;
  final String name;
}

class LeaveBalance {
  const LeaveBalance({
    required this.leaveTypeId,
    required this.leaveTypeName,
    required this.available,
    required this.used,
    required this.pending,
  });

  factory LeaveBalance.fromJson(Map<String, dynamic> json) {
    return LeaveBalance(
      leaveTypeId: json['leaveTypeId'] as String,
      leaveTypeName: json['leaveTypeName'] as String,
      available: (json['available'] as num).toDouble(),
      used: (json['used'] as num).toDouble(),
      pending: (json['pending'] as num).toDouble(),
    );
  }

  final String leaveTypeId;
  final String leaveTypeName;
  final double available;
  final double used;
  final double pending;
}

class LeaveApplication {
  const LeaveApplication({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.leaveTypeName,
    required this.startDate,
    required this.endDate,
    required this.status,
  });

  factory LeaveApplication.fromJson(Map<String, dynamic> json) {
    return LeaveApplication(
      id: json['id'] as String,
      employeeId: json['employeeId'] as String? ?? '',
      employeeName: json['employeeName'] as String? ?? '',
      leaveTypeName: json['leaveTypeName'] as String,
      startDate: json['startDate'] as String,
      endDate: json['endDate'] as String,
      status: _statusFromJson(json['status']),
    );
  }

  final String id;
  final String employeeId;
  final String employeeName;
  final String leaveTypeName;
  final String startDate;
  final String endDate;
  final LeaveApplicationStatus status;

  bool get isPending =>
      status == LeaveApplicationStatus.pending || status == LeaveApplicationStatus.managerApproved;
}
