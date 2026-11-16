import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/performance_review_models.dart';
import '../data/performance_review_repository.dart';

class PerformanceReviewHistoryNotifier extends AsyncNotifier<PagedResult<PerformanceReview>> {
  @override
  Future<PagedResult<PerformanceReview>> build() {
    return ref.watch(performanceReviewRepositoryProvider).fetchMine(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(performanceReviewRepositoryProvider).fetchMine(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> submitSelfReview(String reviewId, {required int rating, String? comments}) async {
    await ref.read(performanceReviewRepositoryProvider).submitSelfReview(reviewId, rating: rating, comments: comments);
    ref.invalidateSelf();
  }
}

final performanceReviewHistoryNotifierProvider =
    AsyncNotifierProvider<PerformanceReviewHistoryNotifier, PagedResult<PerformanceReview>>(
  PerformanceReviewHistoryNotifier.new,
);
