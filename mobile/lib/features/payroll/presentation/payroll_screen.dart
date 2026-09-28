import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/payroll_notifier.dart';
import '../data/payslip.dart';

class PayrollScreen extends ConsumerWidget {
  const PayrollScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final payslips = ref.watch(payrollNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Payslips')),
      body: AsyncValueView<List<Payslip>>(
        value: payslips,
        onRetry: () => ref.invalidate(payrollNotifierProvider),
        data: (list) {
          if (list.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No payslips available yet.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final payslip in list)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          payslip.monthLabel,
                          style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700, fontSize: 16),
                        ),
                        const SizedBox(height: 12),
                        _PayslipRow(label: 'Gross', value: payslip.grossAmount),
                        _PayslipRow(label: 'Deductions', value: -payslip.deductionsAmount),
                        const Divider(height: 20, color: AppColors.gray),
                        _PayslipRow(label: 'Net Pay', value: payslip.netAmount, emphasized: true),
                        if (payslip.notes != null) ...[
                          const SizedBox(height: 8),
                          Text(payslip.notes!, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
                        ],
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

class _PayslipRow extends StatelessWidget {
  const _PayslipRow({required this.label, required this.value, this.emphasized = false});

  final String label;
  final double value;
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              color: AppColors.muted,
              fontWeight: emphasized ? FontWeight.w800 : FontWeight.w500,
            ),
          ),
          Text(
            '₹${value.toStringAsFixed(2)}',
            style: TextStyle(
              color: emphasized ? AppColors.carmine : AppColors.muted,
              fontWeight: emphasized ? FontWeight.w800 : FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
