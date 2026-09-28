import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/leave_notifiers.dart';
import '../data/leave_models.dart';

class LeaveApprovalsScreen extends ConsumerWidget {
  const LeaveApprovalsScreen({super.key});

  Future<void> _decide(BuildContext context, WidgetRef ref, LeaveApplication leave, bool approve) async {
    final commentController = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(approve ? 'Approve leave' : 'Reject leave'),
        content: TextField(
          controller: commentController,
          decoration: const InputDecoration(labelText: 'Comment (optional)'),
          maxLines: 2,
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancel')),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(approve ? 'Approve' : 'Reject'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    final notifier = ref.read(leaveApprovalsNotifierProvider.notifier);
    if (approve) {
      await notifier.approve(leave.id, comment: commentController.text.trim());
    } else {
      await notifier.reject(leave.id, comment: commentController.text.trim());
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final approvals = ref.watch(leaveApprovalsNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Leave Approvals')),
      body: AsyncValueView<PagedResult<LeaveApplication>>(
        value: approvals,
        onRetry: () => ref.invalidate(leaveApprovalsNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No pending leave requests.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final leave in paged.items)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Container(
                              width: 40,
                              height: 40,
                              decoration: const BoxDecoration(
                                color: AppColors.gray,
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(Icons.person_outline, color: AppColors.carmine, size: 18),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    leave.employeeName,
                                    style: const TextStyle(
                                      color: AppColors.muted,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  Text(
                                    '${leave.leaveTypeName} · ${leave.startDate} — ${leave.endDate}',
                                    style: const TextStyle(color: AppColors.muted, fontSize: 12),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: () => _decide(context, ref, leave, false),
                                style: OutlinedButton.styleFrom(foregroundColor: AppColors.carmine),
                                child: const Text('Reject'),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: FilledButton(
                                onPressed: () => _decide(context, ref, leave, true),
                                child: const Text('Approve'),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              if (paged.hasNextPage)
                Center(
                  child: TextButton(
                    onPressed: () => ref.read(leaveApprovalsNotifierProvider.notifier).loadNextPage(),
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
