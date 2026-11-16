import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../application/expense_notifiers.dart';

const _expenseCategories = ['Travel', 'Food', 'Accommodation', 'Office Supplies', 'Other'];

class SubmitExpenseScreen extends ConsumerStatefulWidget {
  const SubmitExpenseScreen({super.key});

  @override
  ConsumerState<SubmitExpenseScreen> createState() => _SubmitExpenseScreenState();
}

class _SubmitExpenseScreenState extends ConsumerState<SubmitExpenseScreen> {
  String _category = _expenseCategories.first;
  DateTime _expenseDate = DateTime.now();
  final _amountController = TextEditingController();
  final _descriptionController = TextEditingController();

  @override
  void dispose() {
    _amountController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  String _formatDate(DateTime date) =>
      '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _expenseDate,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now(),
    );
    if (picked == null) return;
    setState(() => _expenseDate = picked);
  }

  Future<void> _submit() async {
    final amount = double.tryParse(_amountController.text.trim());
    if (amount == null || amount <= 0) return;
    final success = await ref.read(expenseSubmitNotifierProvider.notifier).submit(
          category: _category,
          amount: amount,
          expenseDate: _formatDate(_expenseDate),
          description: _descriptionController.text.trim(),
        );
    if (success && mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final submitState = ref.watch(expenseSubmitNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Submit expense')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            DropdownButtonFormField<String>(
              initialValue: _category,
              decoration: const InputDecoration(labelText: 'Category'),
              items: [
                for (final category in _expenseCategories)
                  DropdownMenuItem(value: category, child: Text(category)),
              ],
              onChanged: (value) => setState(() => _category = value ?? _category),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _amountController,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Amount', prefixText: '₹ '),
            ),
            const SizedBox(height: 16),
            TextFormField(
              readOnly: true,
              onTap: _pickDate,
              controller: TextEditingController(text: _formatDate(_expenseDate)),
              decoration: const InputDecoration(
                labelText: 'Expense date',
                suffixIcon: Icon(Icons.calendar_today_outlined),
              ),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _descriptionController,
              decoration: const InputDecoration(labelText: 'Description (optional)'),
              maxLines: 3,
            ),
            if (submitState.hasError)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  submitState.error.toString(),
                  style: const TextStyle(color: AppColors.carmine),
                ),
              ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: submitState.isLoading ? null : _submit,
              child: submitState.isLoading
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Submit'),
            ),
          ],
        ),
      ),
    );
  }
}
