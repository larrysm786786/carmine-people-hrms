import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/attendance_support/wifi_checkin_service.dart';
import '../data/attendance_record.dart';
import '../data/attendance_repository.dart';

class TodayAttendanceState {
  const TodayAttendanceState({this.record});

  final AttendanceRecord? record;

  bool get isCheckedIn => record?.isCheckedIn ?? false;
}

class TodayAttendanceNotifier extends AsyncNotifier<TodayAttendanceState> {
  @override
  Future<TodayAttendanceState> build() async {
    final today = await ref.read(attendanceRepositoryProvider).fetchToday();
    return TodayAttendanceState(record: today);
  }

  Future<void> checkIn({String? faceEmbedding}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final wifiService = ref.read(wifiCheckinServiceProvider);
      final position = await wifiService.currentPosition();
      final wifiName = await wifiService.currentWifiName();
      final json = await ref.read(attendanceRepositoryProvider).checkIn(
            latitude: position.latitude,
            longitude: position.longitude,
            accuracyMeters: position.accuracy,
            faceEmbedding: faceEmbedding,
            wifiSsid: wifiName,
          );
      return TodayAttendanceState(record: AttendanceRecord.fromJson(json));
    });
  }

  Future<void> checkOut() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final position = await ref.read(wifiCheckinServiceProvider).currentPosition();
      final json = await ref.read(attendanceRepositoryProvider).checkOut(
            latitude: position.latitude,
            longitude: position.longitude,
            accuracyMeters: position.accuracy,
          );
      return TodayAttendanceState(record: AttendanceRecord.fromJson(json));
    });
  }

  Future<void> breakStart() async {
    final previous = state.valueOrNull;
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      await ref.read(attendanceRepositoryProvider).breakStart();
      return previous ?? const TodayAttendanceState();
    });
  }

  Future<void> breakEnd() async {
    final previous = state.valueOrNull;
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      await ref.read(attendanceRepositoryProvider).breakEnd();
      return previous ?? const TodayAttendanceState();
    });
  }
}

final todayAttendanceNotifierProvider =
    AsyncNotifierProvider<TodayAttendanceNotifier, TodayAttendanceState>(
  TodayAttendanceNotifier.new,
);
