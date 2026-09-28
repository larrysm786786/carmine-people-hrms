import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'shift_swap_models.dart';

class ShiftSwapRepository {
  ShiftSwapRepository(this._client);

  final ApiClient _client;

  Future<void> request({
    required String counterpartId,
    required String requesterDate,
    required String counterpartDate,
    String? reason,
  }) {
    return _client.post('/shift-swaps', data: {
      'counterpartId': counterpartId,
      'requesterDate': requesterDate,
      'counterpartDate': counterpartDate,
      'reason': ?reason,
    });
  }

  Future<PagedResult<ShiftSwapItem>> fetchMine({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/shift-swaps', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, ShiftSwapItem.fromJson);
  }

  Future<PagedResult<ShiftSwapItem>> fetchPendingApprovals({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/shift-swaps/team-pending', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromRawPage(raw, ShiftSwapItem.fromJson);
  }

  Future<void> respond(String id, bool accept) {
    return _client.put('/shift-swaps/$id/respond', queryParameters: {'accept': accept});
  }

  Future<void> approve(String id, {String? comment}) {
    return _client.put('/shift-swaps/$id/approve', data: {'comment': ?comment});
  }

  Future<void> reject(String id, {String? comment}) {
    return _client.put('/shift-swaps/$id/reject', data: {'comment': ?comment});
  }

  Future<void> cancel(String id) {
    return _client.put('/shift-swaps/$id/cancel');
  }
}

final shiftSwapRepositoryProvider = Provider<ShiftSwapRepository>((ref) {
  return ShiftSwapRepository(ref.watch(apiClientProvider));
});
