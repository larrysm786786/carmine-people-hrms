import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'performance_review_models.dart';

class PerformanceReviewRepository {
  PerformanceReviewRepository(this._client);

  final ApiClient _client;

  Future<PagedResult<PerformanceReview>> fetchMine({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/performance-reviews/mine', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, PerformanceReview.fromJson);
  }

  Future<void> submitSelfReview(String reviewId, {required int rating, String? comments}) {
    return _client.put('/performance-reviews/$reviewId/self-review', data: {
      'rating': rating,
      'comments': ?comments,
    });
  }
}

final performanceReviewRepositoryProvider = Provider<PerformanceReviewRepository>((ref) {
  return PerformanceReviewRepository(ref.watch(apiClientProvider));
});
