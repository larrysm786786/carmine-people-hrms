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
import '../application/expense_notifiers.dart';
import '../data/expense_models.dart';
import 'submit_expense_screen.dart';

class ExpensesScreen extends ConsumerWidget {
  const ExpensesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(expenseHistoryNotifierProvider);
    final canApprove =
        ref.watch(authNotifierProvider).valueOrNull?.profile?.hasPermission('leave.approve') ?? false;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Expenses'),
        actions: [
          if (canApprove)
            IconButton(
              icon: const Icon(Icons.fact_check_outlined),
              onPressed: () => context.push(RoutePaths.expenseApprovals),
            ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.carmine,
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const SubmitExpenseScreen()),
        ),
        child: const Icon(Icons.add, color: AppColors.white),
      ),
      body: AsyncValueView<PagedResult<Expense>>(
        value: history,
        onRetry: () => ref.invalidate(expenseHistoryNotifierProvider),
        data: (paged) {
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              const SectionHeader(title: 'My Expense Claims'),
              const SizedBox(height: 10),
              if (paged.items.isEmpty)
                const PremiumCard(child: EmptyStateView(message: 'No expense claims yet.'))
              else
                Column(
                  children: [
                    for (final expense in paged.items)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: PremiumCard(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            children: [
                              Container(
                                width: 40,
                                height: 40,
                                decoration: const BoxDecoration(color: AppColors.gray, shape: BoxShape.circle),
                                child: const Icon(Icons.receipt_long_outlined, color: AppColors.carmine, size: 18),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      expense.category,
                                      style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                                    ),
                                    const SizedBox(height: 2),
                                    Text(
                                      '₹${expense.amount.toStringAsFixed(2)} · ${expense.expenseDate}',
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
                                  expense.status.label,
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
                        onPressed: () => ref.read(expenseHistoryNotifierProvider.notifier).loadNextPage(),
                        child: const Text('Load more'),
                      ),
                  ],
                ),
            ],
          );
        },
      ),
    );
  }
}
