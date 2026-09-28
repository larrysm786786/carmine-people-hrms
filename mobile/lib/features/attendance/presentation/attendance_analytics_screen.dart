import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../../home/application/home_summary_notifier.dart';
import '../../home/data/home_summary.dart';
import '../application/attendance_history_notifier.dart';
import '../data/attendance_record.dart';

class AttendanceAnalyticsScreen extends ConsumerWidget {
  const AttendanceAnalyticsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summary = ref.watch(homeSummaryNotifierProvider);
    final history = ref.watch(attendanceHistoryNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('My Attendance Analytics')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
        children: [
          AsyncValueView<HomeSummary>(
            value: summary,
            onRetry: () => ref.invalidate(homeSummaryNotifierProvider),
            data: (data) => PremiumCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'This month at a glance',
                    style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700, fontSize: 15),
                  ),
                  const SizedBox(height: 16),
                  _BarRow(
                    label: 'Present',
                    value: data.monthlySummary.present,
                    max: data.monthlySummary.present + data.monthlySummary.absent + 1,
                  ),
                  _BarRow(
                    label: 'Absent',
                    value: data.monthlySummary.absent,
                    max: data.monthlySummary.present + data.monthlySummary.absent + 1,
                  ),
                  _BarRow(
                    label: 'Leave',
                    value: data.monthlySummary.leave,
                    max: data.monthlySummary.present + 1,
                  ),
                  _BarRow(
                    label: 'Late',
                    value: data.monthlySummary.late,
                    max: data.monthlySummary.present + 1,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Total working hours: ${data.monthlySummary.workingHours}h',
                    style: const TextStyle(color: AppColors.muted, fontSize: 13),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),
          const Text(
            'Punctuality trend (recent records)',
            style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700, fontSize: 15),
          ),
          const SizedBox(height: 10),
          AsyncValueView<PagedResult<AttendanceRecord>>(
            value: history,
            onRetry: () => ref.invalidate(attendanceHistoryNotifierProvider),
            data: (paged) {
              if (paged.items.isEmpty) {
                return const PremiumCard(child: Text('No records yet to analyze.'));
              }
              final late = paged.items.where((r) => r.status == AttendanceStatus.late).length;
              final onTime = paged.items.length - late;
              return PremiumCard(
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        children: [
                          Text(
                            '$onTime',
                            style: const TextStyle(
                              color: AppColors.carmine,
                              fontSize: 24,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const Text('On time', style: TextStyle(color: AppColors.muted, fontSize: 12)),
                        ],
                      ),
                    ),
                    Expanded(
                      child: Column(
                        children: [
                          Text(
                            '$late',
                            style: const TextStyle(
                              color: AppColors.muted,
                              fontSize: 24,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const Text('Late', style: TextStyle(color: AppColors.muted, fontSize: 12)),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _BarRow extends StatelessWidget {
  const _BarRow({required this.label, required this.value, required this.max});

  final String label;
  final int value;
  final int max;

  @override
  Widget build(BuildContext context) {
    final fraction = max == 0 ? 0.0 : (value / max).clamp(0.0, 1.0);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          SizedBox(width: 60, child: Text(label, style: const TextStyle(color: AppColors.muted, fontSize: 12))),
          Expanded(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: fraction,
                minHeight: 10,
                backgroundColor: AppColors.gray,
                valueColor: const AlwaysStoppedAnimation(AppColors.carmine),
              ),
            ),
          ),
          const SizedBox(width: 10),
          SizedBox(
            width: 24,
            child: Text(
              '$value',
              textAlign: TextAlign.right,
              style: const TextStyle(color: AppColors.muted, fontSize: 12, fontWeight: FontWeight.w700),
            ),
          ),
        ],
      ),
    );
  }
}
