import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/shift_swap_notifiers.dart';
import '../data/shift_swap_models.dart';

class ShiftSwapApprovalsScreen extends ConsumerWidget {
  const ShiftSwapApprovalsScreen({super.key});

  Future<void> _decide(BuildContext context, WidgetRef ref, ShiftSwapItem item, bool approve) async {
    final commentController = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(approve ? 'Approve shift swap' : 'Reject shift swap'),
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
    final notifier = ref.read(shiftSwapApprovalsNotifierProvider.notifier);
    if (approve) {
      await notifier.approve(item.id, comment: commentController.text.trim());
    } else {
      await notifier.reject(item.id, comment: commentController.text.trim());
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final approvals = ref.watch(shiftSwapApprovalsNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Shift Swap Approvals')),
      body: AsyncValueView<PagedResult<ShiftSwapItem>>(
        value: approvals,
        onRetry: () => ref.invalidate(shiftSwapApprovalsNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No pending shift swaps.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final item in paged.items)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${item.requesterName} ↔ ${item.counterpartName}',
                          style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                        ),
                        Text(
                          '${item.requesterDate} for ${item.counterpartDate}',
                          style: const TextStyle(color: AppColors.muted, fontSize: 12),
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: () => _decide(context, ref, item, false),
                                style: OutlinedButton.styleFrom(foregroundColor: AppColors.carmine),
                                child: const Text('Reject'),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: FilledButton(
                                onPressed: () => _decide(context, ref, item, true),
                                child: const Text('Approve'),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
