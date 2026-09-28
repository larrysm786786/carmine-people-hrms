import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../application/leave_notifiers.dart';
import '../data/leave_models.dart';

class ApplyLeaveScreen extends ConsumerStatefulWidget {
  const ApplyLeaveScreen({super.key});

  @override
  ConsumerState<ApplyLeaveScreen> createState() => _ApplyLeaveScreenState();
}

class _ApplyLeaveScreenState extends ConsumerState<ApplyLeaveScreen> {
  String? _selectedTypeId;
  DateTime? _startDate;
  DateTime? _endDate;
  final _reasonController = TextEditingController();

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  // Single range-picker dialog: the user taps a start day and an end day on
  // the same calendar and confirms once, instead of two separate pickers.
  Future<void> _pickDateRange() async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: now.subtract(const Duration(days: 30)),
      lastDate: now.add(const Duration(days: 365)),
      initialDateRange: _startDate != null && _endDate != null
          ? DateTimeRange(start: _startDate!, end: _endDate!)
          : null,
    );
    if (picked == null) return;
    setState(() {
      _startDate = picked.start;
      _endDate = picked.end;
    });
  }

  String _formatDate(DateTime date) =>
      '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  Future<void> _submit() async {
    if (_selectedTypeId == null || _startDate == null || _endDate == null) return;
    final success = await ref.read(leaveApplyNotifierProvider.notifier).submit(
          leaveTypeId: _selectedTypeId!,
          startDate: _startDate!.toIso8601String().split('T').first,
          endDate: _endDate!.toIso8601String().split('T').first,
          reason: _reasonController.text.trim(),
        );
    if (success && mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final types = ref.watch(leaveTypesNotifierProvider);
    final applyState = ref.watch(leaveApplyNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Apply for leave')),
      body: AsyncValueView<List<LeaveType>>(
        value: types,
        onRetry: () => ref.invalidate(leaveTypesNotifierProvider),
        data: (leaveTypes) {
          if (leaveTypes.isEmpty) {
            return const EmptyStateView(
              message: 'No leave types are configured yet. Contact HR to set one up.',
              icon: Icons.beach_access_outlined,
            );
          }
          return Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                DropdownButtonFormField<String>(
                  initialValue: _selectedTypeId,
                  decoration: const InputDecoration(labelText: 'Leave type'),
                  items: [
                    for (final type in leaveTypes)
                      DropdownMenuItem(value: type.id, child: Text(type.name)),
                  ],
                  onChanged: (value) => setState(() => _selectedTypeId = value),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  readOnly: true,
                  onTap: _pickDateRange,
                  controller: TextEditingController(
                    text: _startDate == null || _endDate == null
                        ? ''
                        : '${_formatDate(_startDate!)}  →  ${_formatDate(_endDate!)}',
                  ),
                  decoration: const InputDecoration(
                    labelText: 'Leave dates',
                    hintText: 'Select start and end date',
                    suffixIcon: Icon(Icons.calendar_today_outlined),
                  ),
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: _reasonController,
                  decoration: const InputDecoration(labelText: 'Reason'),
                  maxLines: 3,
                ),
                if (applyState.hasError)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      applyState.error.toString(),
                      style: const TextStyle(color: AppColors.carmine),
                    ),
                  ),
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: applyState.isLoading ? null : _submit,
                  child: applyState.isLoading
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Submit'),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
