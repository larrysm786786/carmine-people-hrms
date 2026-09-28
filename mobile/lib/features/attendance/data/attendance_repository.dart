import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'attendance_record.dart';

class AttendanceRepository {
  AttendanceRepository(this._client);

  final ApiClient _client;

  Future<AttendanceRecord?> fetchToday() async {
    final data = await _client.get('/attendance/today');
    if (data == null || data.isEmpty) return null;
    return AttendanceRecord.fromJson(data);
  }

  Future<Map<String, dynamic>> checkIn({
    required double latitude,
    required double longitude,
    required double accuracyMeters,
    String? faceEmbedding,
    String? wifiSsid,
  }) async {
    final data = await _client.post('/attendance/check-in', data: {
      'latitude': latitude,
      'longitude': longitude,
      'accuracyMeters': accuracyMeters,
      'faceEmbedding': ?faceEmbedding,
      'wifiSsid': ?wifiSsid,
    });
    return data ?? const {};
  }

  Future<Map<String, dynamic>> checkOut({
    required double latitude,
    required double longitude,
    required double accuracyMeters,
  }) async {
    final data = await _client.post('/attendance/check-out', data: {
      'latitude': latitude,
      'longitude': longitude,
      'accuracyMeters': accuracyMeters,
    });
    return data ?? const {};
  }

  Future<void> breakStart() => _client.post('/attendance/break-start');

  Future<void> breakEnd() => _client.post('/attendance/break-end');

  Future<PagedResult<AttendanceRecord>> fetchHistory({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/attendance', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromRawPage(raw, AttendanceRecord.fromJson);
  }

  Future<Map<String, dynamic>> fetchOfficeWifi() async {
    final data = await _client.get('/attendance/office-wifi');
    return data ?? const {};
  }

  Future<void> regularize(String attendanceId, String reason) {
    return _client.post('/attendance/$attendanceId/regularize', data: {'reason': reason});
  }
}

final attendanceRepositoryProvider = Provider<AttendanceRepository>((ref) {
  return AttendanceRepository(ref.watch(apiClientProvider));
});
