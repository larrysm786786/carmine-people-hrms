import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/server_time.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/attendance_calendar_notifier.dart';
import '../data/attendance_record.dart';
import 'regularize_dialog.dart';

class AttendanceCalendarScreen extends ConsumerStatefulWidget {
  const AttendanceCalendarScreen({super.key});

  @override
  ConsumerState<AttendanceCalendarScreen> createState() => _AttendanceCalendarScreenState();
}

class _AttendanceCalendarScreenState extends ConsumerState<AttendanceCalendarScreen> {
  late DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);

  void _shiftMonth(int delta) {
    setState(() => _month = DateTime(_month.year, _month.month + delta));
  }

  AttendanceRecord? _recordForDay(List<AttendanceRecord> records, int day) {
    final target = DateTime(_month.year, _month.month, day);
    for (final record in records) {
      final recordDate = DateTime.parse(record.date);
      if (DateUtils.isSameDay(recordDate, target)) return record;
    }
    return null;
  }

  // Only the statuses that need the employee's attention get a dot — a plain
  // present/WFH day stays visually quiet so the eye goes straight to what
  // matters, consistent with the app's flat 4-color palette (no new hues).
  bool _needsAttention(AttendanceStatus status) =>
      status == AttendanceStatus.absent ||
      status == AttendanceStatus.late ||
      status == AttendanceStatus.pendingRegularization;

  @override
  Widget build(BuildContext context) {
    final records = ref.watch(attendanceCalendarNotifierProvider(_month));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Attendance Calendar'),
        actions: [
          IconButton(icon: const Icon(Icons.chevron_left), onPressed: () => _shiftMonth(-1)),
          IconButton(icon: const Icon(Icons.chevron_right), onPressed: () => _shiftMonth(1)),
        ],
      ),
      body: AsyncValueView<List<AttendanceRecord>>(
        value: records,
        onRetry: () => ref.invalidate(attendanceCalendarNotifierProvider(_month)),
        data: (list) {
          final firstDayOfMonth = DateTime(_month.year, _month.month, 1);
          final daysInMonth = DateTime(_month.year, _month.month + 1, 0).day;
          final leadingBlanks = firstDayOfMonth.weekday % 7;

          return RefreshIndicator(
            color: AppColors.carmine,
            onRefresh: () async => ref.invalidate(attendanceCalendarNotifierProvider(_month)),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              children: [
                Text(
                  '${_monthName(_month.month)} ${_month.year}',
                  style: const TextStyle(
                    color: AppColors.muted,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 12),
                GridView.count(
                  crossAxisCount: 7,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  children: [
                    for (final label in const ['S', 'M', 'T', 'W', 'T', 'F', 'S'])
                      Center(
                        child: Text(
                          label,
                          style: const TextStyle(
                            color: AppColors.muted,
                            fontWeight: FontWeight.w700,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    for (int i = 0; i < leadingBlanks; i++) const SizedBox.shrink(),
                    for (int day = 1; day <= daysInMonth; day++) _buildDayCell(day, list),
                  ],
                ),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 16,
                  runSpacing: 6,
                  children: const [
                    _LegendDot(label: 'Today'),
                    _LegendDot(label: 'Needs attention', outlined: true),
                  ],
                ),
                const SizedBox(height: 24),
                Text(
                  'Records this month',
                  style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 8),
                if (list.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 16),
                    child: Text('No attendance records for this month yet.'),
                  )
                else
                  for (final record in list.reversed)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: PremiumCard(
                        padding: const EdgeInsets.all(16),
                        onTap: () async {
                          await showRegularizeDialog(context, ref, record.id);
                          ref.invalidate(attendanceCalendarNotifierProvider(_month));
                        },
                        child: Row(
                          children: [
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
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildDayCell(int day, List<AttendanceRecord> records) {
    final date = DateTime(_month.year, _month.month, day);
    final record = _recordForDay(records, day);
    final isToday = DateUtils.isSameDay(date, DateTime.now());
    final flagged = record != null && _needsAttention(record.status);

    return Container(
      margin: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        color: isToday ? AppColors.carmine : AppColors.gray,
        borderRadius: BorderRadius.circular(8),
        border: flagged && !isToday ? Border.all(color: AppColors.carmine, width: 1.5) : null,
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(
            '$day',
            style: TextStyle(
              color: isToday ? AppColors.white : AppColors.muted,
              fontWeight: FontWeight.w600,
              fontSize: 12,
            ),
          ),
          if (flagged)
            Container(
              margin: const EdgeInsets.only(top: 2),
              width: 6,
              height: 6,
              decoration: BoxDecoration(
                color: isToday ? AppColors.white : AppColors.carmine,
                shape: BoxShape.circle,
              ),
            ),
        ],
      ),
    );
  }

  String _monthName(int month) {
    const names = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December',
    ];
    return names[month - 1];
  }
}

class _LegendDot extends StatelessWidget {
  const _LegendDot({required this.label, this.outlined = false});

  final String label;
  final bool outlined;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 12,
          height: 12,
          decoration: BoxDecoration(
            color: outlined ? AppColors.white : AppColors.carmine,
            shape: BoxShape.circle,
            border: outlined ? Border.all(color: AppColors.carmine, width: 1.5) : null,
          ),
        ),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
      ],
    );
  }
}
