import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/utils/server_time.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../../../core/widgets/section_header.dart';
import '../application/attendance_history_notifier.dart';
import '../data/attendance_record.dart';
import 'regularize_dialog.dart';

class AttendanceScreen extends ConsumerWidget {
  const AttendanceScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(attendanceHistoryNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Attendance')),
      body: RefreshIndicator(
        color: AppColors.carmine,
        onRefresh: () async => ref.invalidate(attendanceHistoryNotifierProvider),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
          children: [
            const SectionHeader(title: 'History'),
            const SizedBox(height: 10),
            AsyncValueView<PagedResult<AttendanceRecord>>(
              value: history,
              onRetry: () => ref.invalidate(attendanceHistoryNotifierProvider),
              data: (paged) {
                if (paged.items.isEmpty) {
                  return const PremiumCard(
                    child: EmptyStateView(message: 'No attendance records yet.'),
                  );
                }
                return Column(
                  children: [
                    for (final record in paged.items)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: PremiumCard(
                          padding: const EdgeInsets.all(16),
                          onTap: () => showRegularizeDialog(context, ref, record.id),
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
                                  Icons.calendar_today_outlined,
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
                                      record.date,
                                      style: const TextStyle(
                                        color: AppColors.muted,
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                    const SizedBox(height: 2),
                                    Text(
                                      '${formatServerTime(record.checkInAt)} — ${formatServerTime(record.checkOutAt)}',
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
                                  record.status.label,
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
                        onPressed: () =>
                            ref.read(attendanceHistoryNotifierProvider.notifier).loadNextPage(),
                        child: const Text('Load more'),
                      ),
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
