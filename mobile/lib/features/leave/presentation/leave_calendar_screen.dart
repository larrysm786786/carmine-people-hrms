import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../application/leave_notifiers.dart';
import '../data/leave_models.dart';

class LeaveCalendarScreen extends ConsumerStatefulWidget {
  const LeaveCalendarScreen({super.key});

  @override
  ConsumerState<LeaveCalendarScreen> createState() => _LeaveCalendarScreenState();
}

class _LeaveCalendarScreenState extends ConsumerState<LeaveCalendarScreen> {
  late DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);

  void _shiftMonth(int delta) {
    setState(() => _month = DateTime(_month.year, _month.month + delta));
  }

  bool _isOnLeave(LeaveApplication leave, DateTime day) {
    final start = DateTime.parse(leave.startDate);
    final end = DateTime.parse(leave.endDate);
    final d = DateTime(day.year, day.month, day.day);
    return !d.isBefore(start) && !d.isAfter(end);
  }

  @override
  Widget build(BuildContext context) {
    final leaves = ref.watch(leaveCalendarNotifierProvider(_month));
    final firstDayOfMonth = DateTime(_month.year, _month.month, 1);
    final daysInMonth = DateTime(_month.year, _month.month + 1, 0).day;
    final leadingBlanks = firstDayOfMonth.weekday % 7;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Leave Calendar'),
        actions: [
          IconButton(icon: const Icon(Icons.chevron_left), onPressed: () => _shiftMonth(-1)),
          IconButton(icon: const Icon(Icons.chevron_right), onPressed: () => _shiftMonth(1)),
        ],
      ),
      body: AsyncValueView<List<LeaveApplication>>(
        value: leaves,
        onRetry: () => ref.invalidate(leaveCalendarNotifierProvider(_month)),
        data: (list) {
          return ListView(
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
              const SizedBox(height: 24),
              Text(
                'On leave this month',
                style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 8),
              if (list.isEmpty)
                const Padding(
                  padding: EdgeInsets.symmetric(vertical: 16),
                  child: Text('No one is on approved leave this month.'),
                )
              else
                for (final leave in list)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.beach_access_outlined, color: AppColors.carmine),
                    title: Text(leave.employeeName),
                    subtitle: Text('${leave.leaveTypeName} · ${leave.startDate} — ${leave.endDate}'),
                  ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildDayCell(int day, List<LeaveApplication> leaves) {
    final date = DateTime(_month.year, _month.month, day);
    final onLeaveCount = leaves.where((l) => _isOnLeave(l, date)).length;
    final isToday = DateUtils.isSameDay(date, DateTime.now());

    return Container(
      margin: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        color: isToday ? AppColors.carmine : AppColors.gray,
        borderRadius: BorderRadius.circular(8),
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
          if (onLeaveCount > 0)
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
