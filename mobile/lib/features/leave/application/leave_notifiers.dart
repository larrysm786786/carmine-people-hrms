import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/leave_models.dart';
import '../data/leave_repository.dart';

class LeaveTypesNotifier extends AsyncNotifier<List<LeaveType>> {
  @override
  Future<List<LeaveType>> build() {
    return ref.watch(leaveRepositoryProvider).fetchTypes();
  }
}

final leaveTypesNotifierProvider =
    AsyncNotifierProvider<LeaveTypesNotifier, List<LeaveType>>(LeaveTypesNotifier.new);

class LeaveBalancesNotifier extends AsyncNotifier<List<LeaveBalance>> {
  @override
  Future<List<LeaveBalance>> build() {
    return ref.watch(leaveRepositoryProvider).fetchBalances();
  }
}

final leaveBalancesNotifierProvider =
    AsyncNotifierProvider<LeaveBalancesNotifier, List<LeaveBalance>>(LeaveBalancesNotifier.new);

class LeaveHistoryNotifier extends AsyncNotifier<PagedResult<LeaveApplication>> {
  @override
  Future<PagedResult<LeaveApplication>> build() {
    return ref.watch(leaveRepositoryProvider).fetchHistory(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(leaveRepositoryProvider).fetchHistory(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> cancel(String leaveId) async {
    await ref.read(leaveRepositoryProvider).cancel(leaveId);
    ref.invalidateSelf();
  }
}

final leaveHistoryNotifierProvider =
    AsyncNotifierProvider<LeaveHistoryNotifier, PagedResult<LeaveApplication>>(
  LeaveHistoryNotifier.new,
);

class LeaveApplyNotifier extends AsyncNotifier<void> {
  @override
  Future<void> build() async {}

  Future<bool> submit({
    required String leaveTypeId,
    required String startDate,
    required String endDate,
    String? reason,
    String? attachmentFilePath,
  }) async {
    state = const AsyncLoading();
    final result = await AsyncValue.guard(() async {
      String? attachmentUrl;
      if (attachmentFilePath != null) {
        final documentId =
            await ref.read(leaveRepositoryProvider).uploadAttachment(attachmentFilePath);
        attachmentUrl = '/documents/$documentId/content';
      }
      await ref.read(leaveRepositoryProvider).apply(
            leaveTypeId: leaveTypeId,
            startDate: startDate,
            endDate: endDate,
            reason: reason,
            attachmentUrl: attachmentUrl,
          );
    });
    state = result;
    return !result.hasError;
  }
}

final leaveApplyNotifierProvider =
    AsyncNotifierProvider<LeaveApplyNotifier, void>(LeaveApplyNotifier.new);

class LeaveApprovalsNotifier extends AsyncNotifier<PagedResult<LeaveApplication>> {
  @override
  Future<PagedResult<LeaveApplication>> build() {
    return ref.watch(leaveRepositoryProvider).fetchPendingApprovals(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage =
        await ref.read(leaveRepositoryProvider).fetchPendingApprovals(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> approve(String leaveId, {String? comment}) async {
    await ref.read(leaveRepositoryProvider).approve(leaveId, comment: comment);
    ref.invalidateSelf();
  }

  Future<void> reject(String leaveId, {String? comment}) async {
    await ref.read(leaveRepositoryProvider).reject(leaveId, comment: comment);
    ref.invalidateSelf();
  }
}

final leaveApprovalsNotifierProvider =
    AsyncNotifierProvider<LeaveApprovalsNotifier, PagedResult<LeaveApplication>>(
  LeaveApprovalsNotifier.new,
);

class LeaveCalendarNotifier extends FamilyAsyncNotifier<List<LeaveApplication>, DateTime> {
  @override
  Future<List<LeaveApplication>> build(DateTime month) {
    final from = DateTime(month.year, month.month, 1);
    final to = DateTime(month.year, month.month + 1, 0);
    return ref.watch(leaveRepositoryProvider).fetchTeamLeaves(
          from: from.toIso8601String().split('T').first,
          to: to.toIso8601String().split('T').first,
        );
  }
}

final leaveCalendarNotifierProvider =
    AsyncNotifierProviderFamily<LeaveCalendarNotifier, List<LeaveApplication>, DateTime>(
  LeaveCalendarNotifier.new,
);
