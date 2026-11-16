import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/expense_models.dart';
import '../data/expense_repository.dart';

class ExpenseHistoryNotifier extends AsyncNotifier<PagedResult<Expense>> {
  @override
  Future<PagedResult<Expense>> build() {
    return ref.watch(expenseRepositoryProvider).fetchMine(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(expenseRepositoryProvider).fetchMine(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }
}

final expenseHistoryNotifierProvider =
    AsyncNotifierProvider<ExpenseHistoryNotifier, PagedResult<Expense>>(ExpenseHistoryNotifier.new);

class ExpenseSubmitNotifier extends AsyncNotifier<void> {
  @override
  Future<void> build() async {}

  Future<bool> submit({
    required String category,
    required double amount,
    required String expenseDate,
    String? description,
  }) async {
    state = const AsyncLoading();
    final result = await AsyncValue.guard(() async {
      await ref.read(expenseRepositoryProvider).submit(
            category: category,
            amount: amount,
            expenseDate: expenseDate,
            description: description,
          );
    });
    state = result;
    if (!result.hasError) ref.invalidate(expenseHistoryNotifierProvider);
    return !result.hasError;
  }
}

final expenseSubmitNotifierProvider =
    AsyncNotifierProvider<ExpenseSubmitNotifier, void>(ExpenseSubmitNotifier.new);

class ExpenseApprovalsNotifier extends AsyncNotifier<PagedResult<Expense>> {
  @override
  Future<PagedResult<Expense>> build() {
    return ref.watch(expenseRepositoryProvider).fetchPendingApprovals(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(expenseRepositoryProvider).fetchPendingApprovals(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> approve(String expenseId, {String? comment}) async {
    await ref.read(expenseRepositoryProvider).approve(expenseId, comment: comment);
    ref.invalidateSelf();
  }

  Future<void> reject(String expenseId, {String? comment}) async {
    await ref.read(expenseRepositoryProvider).reject(expenseId, comment: comment);
    ref.invalidateSelf();
  }
}

final expenseApprovalsNotifierProvider =
    AsyncNotifierProvider<ExpenseApprovalsNotifier, PagedResult<Expense>>(ExpenseApprovalsNotifier.new);
