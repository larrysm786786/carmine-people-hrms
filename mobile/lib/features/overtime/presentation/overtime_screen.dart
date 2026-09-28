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
import '../application/overtime_notifiers.dart';
import '../data/overtime_models.dart';
import 'request_overtime_screen.dart';

class OvertimeScreen extends ConsumerWidget {
  const OvertimeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(overtimeHistoryNotifierProvider);
    final canApprove =
        ref.watch(authNotifierProvider).valueOrNull?.profile?.hasPermission('overtime.approve') ?? false;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Overtime'),
        actions: [
          if (canApprove)
            IconButton(
              icon: const Icon(Icons.fact_check_outlined),
              onPressed: () => context.push(RoutePaths.overtimeApprovals),
            ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.carmine,
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const RequestOvertimeScreen()),
        ),
        child: const Icon(Icons.add, color: AppColors.white),
      ),
      body: AsyncValueView<PagedResult<OvertimeRequestItem>>(
        value: history,
        onRetry: () => ref.invalidate(overtimeHistoryNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No overtime requests yet.')),
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
                    onTap: item.isPending
                        ? () => ref.read(overtimeHistoryNotifierProvider.notifier).cancel(item.id)
                        : null,
                    child: Row(
                      children: [
                        Container(
                          width: 40,
                          height: 40,
                          decoration: const BoxDecoration(color: AppColors.gray, shape: BoxShape.circle),
                          child: const Icon(Icons.schedule_outlined, color: AppColors.carmine, size: 18),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                '${item.hours}h on ${item.date}',
                                style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                              ),
                              if (item.reason != null)
                                Text(item.reason!, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
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
                            item.status.label,
                            style: const TextStyle(color: AppColors.muted, fontSize: 11, fontWeight: FontWeight.w700),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              if (paged.hasNextPage)
                Center(
                  child: TextButton(
                    onPressed: () => ref.read(overtimeHistoryNotifierProvider.notifier).loadNextPage(),
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
