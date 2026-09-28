import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/overtime_models.dart';
import '../data/overtime_repository.dart';

class OvertimeHistoryNotifier extends AsyncNotifier<PagedResult<OvertimeRequestItem>> {
  @override
  Future<PagedResult<OvertimeRequestItem>> build() {
    return ref.watch(overtimeRepositoryProvider).fetchMine(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(overtimeRepositoryProvider).fetchMine(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> cancel(String id) async {
    await ref.read(overtimeRepositoryProvider).cancel(id);
    ref.invalidateSelf();
  }
}

final overtimeHistoryNotifierProvider =
    AsyncNotifierProvider<OvertimeHistoryNotifier, PagedResult<OvertimeRequestItem>>(
  OvertimeHistoryNotifier.new,
);

class OvertimeRequestNotifier extends AsyncNotifier<void> {
  @override
  Future<void> build() async {}

  Future<bool> submit({required String date, required double hours, String? reason}) async {
    state = const AsyncLoading();
    final result = await AsyncValue.guard(
      () => ref.read(overtimeRepositoryProvider).request(date: date, hours: hours, reason: reason),
    );
    state = result;
    return !result.hasError;
  }
}

final overtimeRequestNotifierProvider =
    AsyncNotifierProvider<OvertimeRequestNotifier, void>(OvertimeRequestNotifier.new);

class OvertimeApprovalsNotifier extends AsyncNotifier<PagedResult<OvertimeRequestItem>> {
  @override
  Future<PagedResult<OvertimeRequestItem>> build() {
    return ref.watch(overtimeRepositoryProvider).fetchPendingApprovals(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage =
        await ref.read(overtimeRepositoryProvider).fetchPendingApprovals(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> approve(String id, {String? comment}) async {
    await ref.read(overtimeRepositoryProvider).approve(id, comment: comment);
    ref.invalidateSelf();
  }

  Future<void> reject(String id, {String? comment}) async {
    await ref.read(overtimeRepositoryProvider).reject(id, comment: comment);
    ref.invalidateSelf();
  }
}

final overtimeApprovalsNotifierProvider =
    AsyncNotifierProvider<OvertimeApprovalsNotifier, PagedResult<OvertimeRequestItem>>(
  OvertimeApprovalsNotifier.new,
);
