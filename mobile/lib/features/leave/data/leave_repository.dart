import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'leave_models.dart';

class LeaveRepository {
  LeaveRepository(this._client);

  final ApiClient _client;

  Future<List<LeaveType>> fetchTypes() async {
    final items = await _client.getList('/leaves/types');
    return items.map((e) => LeaveType.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<LeaveBalance>> fetchBalances() async {
    final items = await _client.getList('/leaves/balances');
    return items.map((e) => LeaveBalance.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<PagedResult<LeaveApplication>> fetchHistory({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/leaves', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, LeaveApplication.fromJson);
  }

  Future<void> apply({
    required String leaveTypeId,
    required String startDate,
    required String endDate,
    String? reason,
    String? attachmentUrl,
  }) async {
    await _client.post('/leaves', data: {
      'leaveTypeId': leaveTypeId,
      'startDate': startDate,
      'endDate': endDate,
      'reason': ?reason,
      'attachmentUrl': ?attachmentUrl,
    });
  }

  Future<String> uploadAttachment(String filePath) async {
    final data = await _client.postMultipart('/documents/upload/leave-attachment', filePath);
    return data?['id'] as String? ?? '';
  }

  Future<void> cancel(String leaveId) {
    return _client.put('/leaves/$leaveId/cancel');
  }

  Future<List<LeaveApplication>> fetchTeamLeaves({required String from, required String to}) async {
    final raw = await _client.getPage('/leaves', queryParameters: {
      'status': 2, // Approved
      'page': 1,
      'pageSize': 100,
    });
    final all = PagedResult.fromRawPage(raw, LeaveApplication.fromJson).items;
    return all.where((l) => l.endDate.compareTo(from) >= 0 && l.startDate.compareTo(to) <= 0).toList();
  }

  Future<PagedResult<LeaveApplication>> fetchPendingApprovals({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/leaves/team-pending', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromRawPage(raw, LeaveApplication.fromJson);
  }

  Future<void> approve(String leaveId, {String? comment}) {
    return _client.put('/leaves/$leaveId/approve', data: {'comment': ?comment});
  }

  Future<void> reject(String leaveId, {String? comment}) {
    return _client.put('/leaves/$leaveId/reject', data: {'comment': ?comment});
  }
}

final leaveRepositoryProvider = Provider<LeaveRepository>((ref) {
  return LeaveRepository(ref.watch(apiClientProvider));
});
