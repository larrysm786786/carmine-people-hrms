import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/shift_swap_models.dart';
import '../data/shift_swap_repository.dart';

class ShiftSwapHistoryNotifier extends AsyncNotifier<PagedResult<ShiftSwapItem>> {
  @override
  Future<PagedResult<ShiftSwapItem>> build() {
    return ref.watch(shiftSwapRepositoryProvider).fetchMine(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(shiftSwapRepositoryProvider).fetchMine(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> respond(String id, bool accept) async {
    await ref.read(shiftSwapRepositoryProvider).respond(id, accept);
    ref.invalidateSelf();
  }

  Future<void> cancel(String id) async {
    await ref.read(shiftSwapRepositoryProvider).cancel(id);
    ref.invalidateSelf();
  }
}

final shiftSwapHistoryNotifierProvider =
    AsyncNotifierProvider<ShiftSwapHistoryNotifier, PagedResult<ShiftSwapItem>>(
  ShiftSwapHistoryNotifier.new,
);

class ShiftSwapRequestNotifier extends AsyncNotifier<void> {
  @override
  Future<void> build() async {}

  Future<bool> submit({
    required String counterpartId,
    required String requesterDate,
    required String counterpartDate,
    String? reason,
  }) async {
    state = const AsyncLoading();
    final result = await AsyncValue.guard(
      () => ref.read(shiftSwapRepositoryProvider).request(
            counterpartId: counterpartId,
            requesterDate: requesterDate,
            counterpartDate: counterpartDate,
            reason: reason,
          ),
    );
    state = result;
    return !result.hasError;
  }
}

final shiftSwapRequestNotifierProvider =
    AsyncNotifierProvider<ShiftSwapRequestNotifier, void>(ShiftSwapRequestNotifier.new);

class ShiftSwapApprovalsNotifier extends AsyncNotifier<PagedResult<ShiftSwapItem>> {
  @override
  Future<PagedResult<ShiftSwapItem>> build() {
    return ref.watch(shiftSwapRepositoryProvider).fetchPendingApprovals(page: 1);
  }

  Future<void> approve(String id, {String? comment}) async {
    await ref.read(shiftSwapRepositoryProvider).approve(id, comment: comment);
    ref.invalidateSelf();
  }

  Future<void> reject(String id, {String? comment}) async {
    await ref.read(shiftSwapRepositoryProvider).reject(id, comment: comment);
    ref.invalidateSelf();
  }
}

final shiftSwapApprovalsNotifierProvider =
    AsyncNotifierProvider<ShiftSwapApprovalsNotifier, PagedResult<ShiftSwapItem>>(
  ShiftSwapApprovalsNotifier.new,
);
