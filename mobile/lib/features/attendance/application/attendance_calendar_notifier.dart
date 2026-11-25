import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/attendance_record.dart';
import '../data/attendance_repository.dart';

final attendanceCalendarNotifierProvider =
    FutureProvider.family<List<AttendanceRecord>, DateTime>((ref, month) {
  return ref.watch(attendanceRepositoryProvider).fetchForMonth(month);
});
