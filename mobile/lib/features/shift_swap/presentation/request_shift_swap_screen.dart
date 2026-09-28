import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../employees/application/employees_list_notifier.dart';
import '../../profile/data/employee_detail.dart';
import '../application/shift_swap_notifiers.dart';

class RequestShiftSwapScreen extends ConsumerStatefulWidget {
  const RequestShiftSwapScreen({super.key});

  @override
  ConsumerState<RequestShiftSwapScreen> createState() => _RequestShiftSwapScreenState();
}

class _RequestShiftSwapScreenState extends ConsumerState<RequestShiftSwapScreen> {
  EmployeeDetail? _counterpart;
  DateTime? _requesterDate;
  DateTime? _counterpartDate;
  final _reasonController = TextEditingController();

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  Future<void> _pickDate(bool isRequester) async {
    final picked = await showDatePicker(
      context: context,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 60)),
      initialDate: DateTime.now(),
    );
    if (picked == null) return;
    setState(() => isRequester ? _requesterDate = picked : _counterpartDate = picked);
  }

  Future<void> _submit() async {
    if (_counterpart == null || _requesterDate == null || _counterpartDate == null) return;
    final success = await ref.read(shiftSwapRequestNotifierProvider.notifier).submit(
          counterpartId: _counterpart!.id,
          requesterDate: _requesterDate!.toIso8601String().split('T').first,
          counterpartDate: _counterpartDate!.toIso8601String().split('T').first,
          reason: _reasonController.text.trim(),
        );
    if (success && mounted) {
      ref.invalidate(shiftSwapHistoryNotifierProvider);
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final employees = ref.watch(employeesListNotifierProvider);
    final state = ref.watch(shiftSwapRequestNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Request Shift Swap')),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text('Swap with', style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            AsyncValueView(
              value: employees,
              data: (paged) => DropdownButtonFormField<EmployeeDetail>(
                initialValue: _counterpart,
                hint: const Text('Select employee'),
                items: [
                  for (final e in paged.items)
                    DropdownMenuItem(value: e, child: Text(e.fullName)),
                ],
                onChanged: (value) => setState(() => _counterpart = value),
              ),
            ),
            const SizedBox(height: 16),
            ListTile(
              title: Text(_requesterDate == null
                  ? 'Your shift date (giving up)'
                  : _requesterDate!.toLocal().toString().split(' ').first),
              trailing: const Icon(Icons.calendar_today_outlined),
              onTap: () => _pickDate(true),
            ),
            ListTile(
              title: Text(_counterpartDate == null
                  ? 'Their shift date (taking over)'
                  : _counterpartDate!.toLocal().toString().split(' ').first),
              trailing: const Icon(Icons.calendar_today_outlined),
              onTap: () => _pickDate(false),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _reasonController,
              decoration: const InputDecoration(labelText: 'Reason'),
              maxLines: 3,
            ),
            if (state.hasError)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text('Failed to submit. Please try again.', style: TextStyle(color: AppColors.carmine)),
              ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: state.isLoading ? null : _submit,
              child: state.isLoading
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.white),
                    )
                  : const Text('Send Request'),
            ),
          ],
        ),
      ),
    );
  }
}
