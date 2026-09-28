class ActiveBreak {
  const ActiveBreak({required this.id, required this.breakStartAt});

  factory ActiveBreak.fromJson(Map<String, dynamic> json) {
    return ActiveBreak(
      id: json['id'] as String,
      breakStartAt: json['breakStartAt'] as String,
    );
  }

  final String id;
  final String breakStartAt;
}

class TodaySchedule {
  const TodaySchedule({
    this.shiftName,
    this.startTime,
    this.endTime,
    required this.gracePeriodMinutes,
    this.activeBreak,
    required this.totalBreakMinutesToday,
  });

  factory TodaySchedule.fromJson(Map<String, dynamic> json) {
    return TodaySchedule(
      shiftName: json['shiftName'] as String?,
      startTime: json['startTime'] as String?,
      endTime: json['endTime'] as String?,
      gracePeriodMinutes: json['gracePeriodMinutes'] as int? ?? 0,
      activeBreak: json['activeBreak'] != null
          ? ActiveBreak.fromJson(json['activeBreak'] as Map<String, dynamic>)
          : null,
      totalBreakMinutesToday: json['totalBreakMinutesToday'] as int? ?? 0,
    );
  }

  final String? shiftName;
  final String? startTime;
  final String? endTime;
  final int gracePeriodMinutes;
  final ActiveBreak? activeBreak;
  final int totalBreakMinutesToday;

  bool get isOnBreak => activeBreak != null;
}

class MonthlySummary {
  const MonthlySummary({
    required this.present,
    required this.absent,
    required this.leave,
    required this.late,
    required this.workingHours,
  });

  factory MonthlySummary.fromJson(Map<String, dynamic> json) {
    return MonthlySummary(
      present: json['present'] as int? ?? 0,
      absent: json['absent'] as int? ?? 0,
      leave: json['leave'] as int? ?? 0,
      late: json['late'] as int? ?? 0,
      workingHours: json['workingHours'] as int? ?? 0,
    );
  }

  final int present;
  final int absent;
  final int leave;
  final int late;
  final int workingHours;
}

class HomeSummary {
  const HomeSummary({required this.schedule, required this.monthlySummary});

  factory HomeSummary.fromJson(Map<String, dynamic> json) {
    return HomeSummary(
      schedule: TodaySchedule.fromJson(json['schedule'] as Map<String, dynamic>? ?? const {}),
      monthlySummary:
          MonthlySummary.fromJson(json['monthlySummary'] as Map<String, dynamic>? ?? const {}),
    );
  }

  final TodaySchedule schedule;
  final MonthlySummary monthlySummary;
}
