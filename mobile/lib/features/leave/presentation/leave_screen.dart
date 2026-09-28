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
import '../../../core/widgets/section_header.dart';
import '../../../core/widgets/stat_pill.dart';
import '../application/leave_notifiers.dart';
import '../data/leave_models.dart';
import 'apply_leave_screen.dart';

class LeaveScreen extends ConsumerWidget {
  const LeaveScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final balances = ref.watch(leaveBalancesNotifierProvider);
    final history = ref.watch(leaveHistoryNotifierProvider);
    final canApprove =
        ref.watch(authNotifierProvider).valueOrNull?.profile?.hasPermission('leave.approve') ?? false;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Leave'),
        actions: [
          IconButton(
            icon: const Icon(Icons.calendar_month_outlined),
            onPressed: () => context.push(RoutePaths.leaveCalendar),
          ),
          if (canApprove)
            IconButton(
              icon: const Icon(Icons.fact_check_outlined),
              onPressed: () => context.push(RoutePaths.leaveApprovals),
            ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.carmine,
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const ApplyLeaveScreen()),
        ),
        child: const Icon(Icons.add, color: AppColors.white),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
        children: [
          const SectionHeader(title: 'Leave Balances'),
          const SizedBox(height: 10),
          AsyncValueView<List<LeaveBalance>>(
            value: balances,
            onRetry: () => ref.invalidate(leaveBalancesNotifierProvider),
            data: (list) {
              if (list.isEmpty) {
                return const PremiumCard(child: EmptyStateView(message: 'No balances available.'));
              }
              return PremiumCard(
                padding: const EdgeInsets.all(16),
                child: Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    for (final b in list)
                      SizedBox(
                        width: 100,
                        child: StatPill(value: '${b.available}', label: b.leaveTypeName),
                      ),
                  ],
                ),
              );
            },
          ),
          const SizedBox(height: 24),
          const SectionHeader(title: 'History'),
          const SizedBox(height: 10),
          AsyncValueView<PagedResult<LeaveApplication>>(
            value: history,
            onRetry: () => ref.invalidate(leaveHistoryNotifierProvider),
            data: (paged) {
              if (paged.items.isEmpty) {
                return const PremiumCard(child: EmptyStateView(message: 'No leave applications yet.'));
              }
              return Column(
                children: [
                  for (final leave in paged.items)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: PremiumCard(
                        padding: const EdgeInsets.all(16),
                        onTap: leave.isPending
                            ? () => ref.read(leaveHistoryNotifierProvider.notifier).cancel(leave.id)
                            : null,
                        child: Row(
                          children: [
                            Container(
                              width: 40,
                              height: 40,
                              decoration: const BoxDecoration(
                                color: AppColors.gray,
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(
                                Icons.beach_access_outlined,
                                color: AppColors.carmine,
                                size: 18,
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    leave.leaveTypeName,
                                    style: const TextStyle(
                                      color: AppColors.muted,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${leave.startDate} — ${leave.endDate}',
                                    style: const TextStyle(color: AppColors.muted, fontSize: 12),
                                  ),
                                ],
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: AppColors.gray,
                                borderRadius: BorderRadius.circular(999),
                              ),
                              child: Text(
                                leave.status.label,
                                style: const TextStyle(
                                  color: AppColors.muted,
                                  fontSize: 11,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  if (paged.hasNextPage)
                    TextButton(
                      onPressed: () => ref.read(leaveHistoryNotifierProvider.notifier).loadNextPage(),
                      child: const Text('Load more'),
                    ),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}
