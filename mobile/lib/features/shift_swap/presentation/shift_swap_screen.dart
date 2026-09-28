import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth_session/auth_notifier.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/shift_swap_notifiers.dart';
import '../data/shift_swap_models.dart';
import 'request_shift_swap_screen.dart';

class ShiftSwapScreen extends ConsumerWidget {
  const ShiftSwapScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(shiftSwapHistoryNotifierProvider);
    final myId = ref.watch(authNotifierProvider).valueOrNull?.profile?.id;
    final canApprove =
        ref.watch(authNotifierProvider).valueOrNull?.profile?.hasPermission('shift_swap.approve') ?? false;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Shift Swaps'),
        actions: [
          if (canApprove)
            IconButton(
              icon: const Icon(Icons.fact_check_outlined),
              onPressed: () => context.push(RoutePaths.shiftSwapApprovals),
            ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.carmine,
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const RequestShiftSwapScreen()),
        ),
        child: const Icon(Icons.add, color: AppColors.white),
      ),
      body: AsyncValueView<PagedResult<ShiftSwapItem>>(
        value: history,
        onRetry: () => ref.invalidate(shiftSwapHistoryNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No shift swap requests yet.')),
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
                        const SizedBox(height: 2),
                        Text(
                          '${item.requesterDate} for ${item.counterpartDate}',
                          style: const TextStyle(color: AppColors.muted, fontSize: 12),
                        ),
                        const SizedBox(height: 8),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: AppColors.gray,
                                borderRadius: BorderRadius.circular(999),
                              ),
                              child: Text(
                                item.status.label,
                                style: const TextStyle(color: AppColors.muted, fontSize: 11, fontWeight: FontWeight.w700),
                              ),
                            ),
                            if (item.status == ShiftSwapStatus.requestedAcceptance &&
                                item.counterpartId == myId)
                              Row(
                                children: [
                                  TextButton(
                                    onPressed: () => ref
                                        .read(shiftSwapHistoryNotifierProvider.notifier)
                                        .respond(item.id, false),
                                    child: const Text('Decline'),
                                  ),
                                  FilledButton(
                                    onPressed: () => ref
                                        .read(shiftSwapHistoryNotifierProvider.notifier)
                                        .respond(item.id, true),
                                    child: const Text('Accept'),
                                  ),
                                ],
                              )
                            else if (item.status == ShiftSwapStatus.requestedAcceptance &&
                                item.requesterId == myId)
                              TextButton(
                                onPressed: () =>
                                    ref.read(shiftSwapHistoryNotifierProvider.notifier).cancel(item.id),
                                child: const Text('Cancel'),
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
                    onPressed: () => ref.read(shiftSwapHistoryNotifierProvider.notifier).loadNextPage(),
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
