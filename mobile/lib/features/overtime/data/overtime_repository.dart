import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'overtime_models.dart';

class OvertimeRepository {
  OvertimeRepository(this._client);

  final ApiClient _client;

  Future<void> request({required String date, required double hours, String? reason}) {
    return _client.post('/overtime', data: {
      'date': date,
      'hours': hours,
      'reason': ?reason,
    });
  }

  Future<PagedResult<OvertimeRequestItem>> fetchMine({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/overtime', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, OvertimeRequestItem.fromJson);
  }

  Future<PagedResult<OvertimeRequestItem>> fetchPendingApprovals({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/overtime/team-pending', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromRawPage(raw, OvertimeRequestItem.fromJson);
  }

  Future<void> approve(String id, {String? comment}) {
    return _client.put('/overtime/$id/approve', data: {'comment': ?comment});
  }

  Future<void> reject(String id, {String? comment}) {
    return _client.put('/overtime/$id/reject', data: {'comment': ?comment});
  }

  Future<void> cancel(String id) {
    return _client.put('/overtime/$id/cancel');
  }
}

final overtimeRepositoryProvider = Provider<OvertimeRepository>((ref) {
  return OvertimeRepository(ref.watch(apiClientProvider));
});
