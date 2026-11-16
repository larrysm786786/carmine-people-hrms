import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/performance_review_notifiers.dart';
import '../data/performance_review_models.dart';

class PerformanceReviewsScreen extends ConsumerWidget {
  const PerformanceReviewsScreen({super.key});

  Future<void> _submitSelfReview(BuildContext context, WidgetRef ref, PerformanceReview review) async {
    int rating = 0;
    final commentsController = TextEditingController();

    final submitted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: Text('Self-review: ${review.period}'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: [
                  for (var i = 1; i <= 5; i++)
                    GestureDetector(
                      onTap: () => setState(() => rating = i),
                      child: Icon(
                        i <= rating ? Icons.star : Icons.star_border,
                        color: AppColors.carmine,
                        size: 32,
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 16),
              TextField(
                controller: commentsController,
                decoration: const InputDecoration(labelText: 'Comments (optional)'),
                maxLines: 3,
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancel')),
            FilledButton(
              onPressed: rating == 0 ? null : () => Navigator.of(context).pop(true),
              child: const Text('Submit'),
            ),
          ],
        ),
      ),
    );

    if (submitted != true || rating == 0) return;
    await ref.read(performanceReviewHistoryNotifierProvider.notifier).submitSelfReview(
          review.id,
          rating: rating,
          comments: commentsController.text.trim().isEmpty ? null : commentsController.text.trim(),
        );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(performanceReviewHistoryNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Performance Reviews')),
      body: AsyncValueView<PagedResult<PerformanceReview>>(
        value: history,
        onRetry: () => ref.invalidate(performanceReviewHistoryNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No performance reviews yet.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final review in paged.items)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    onTap: review.status == PerformanceReviewStatus.pendingSelfReview
                        ? () => _submitSelfReview(context, ref, review)
                        : null,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                review.period,
                                style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: AppColors.gray,
                                borderRadius: BorderRadius.circular(999),
                              ),
                              child: Text(
                                review.status.label,
                                style: const TextStyle(color: AppColors.muted, fontSize: 11, fontWeight: FontWeight.w700),
                              ),
                            ),
                          ],
                        ),
                        if (review.selfRating != null || review.managerRating != null) ...[
                          const SizedBox(height: 8),
                          Text(
                            [
                              if (review.selfRating != null) 'Self: ${review.selfRating}/5',
                              if (review.managerRating != null) 'Manager: ${review.managerRating}/5',
                            ].join(' · '),
                            style: const TextStyle(color: AppColors.muted, fontSize: 12),
                          ),
                        ],
                        if (review.status == PerformanceReviewStatus.pendingSelfReview) ...[
                          const SizedBox(height: 8),
                          const Text(
                            'Tap to complete your self-assessment',
                            style: TextStyle(color: AppColors.carmine, fontSize: 12, fontWeight: FontWeight.w600),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              if (paged.hasNextPage)
                Center(
                  child: TextButton(
                    onPressed: () => ref.read(performanceReviewHistoryNotifierProvider.notifier).loadNextPage(),
                    child: const Text('Load more'),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
