import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../application/attendance_history_notifier.dart';
import '../data/attendance_repository.dart';

Future<void> showRegularizeDialog(BuildContext context, WidgetRef ref, String attendanceId) {
  final controller = TextEditingController();
  return showDialog<void>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Regularize attendance'),
      content: TextField(
        controller: controller,
        decoration: const InputDecoration(labelText: 'Reason'),
        maxLines: 3,
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cancel')),
        FilledButton(
          onPressed: () async {
            if (controller.text.trim().isEmpty) return;
            await ref
                .read(attendanceRepositoryProvider)
                .regularize(attendanceId, controller.text.trim());
            if (context.mounted) {
              Navigator.of(context).pop();
              ref.invalidate(attendanceHistoryNotifierProvider);
            }
          },
          child: const Text('Submit'),
        ),
      ],
    ),
  );
}
