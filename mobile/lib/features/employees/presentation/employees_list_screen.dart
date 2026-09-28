import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../profile/data/employee_detail.dart';
import '../application/employees_list_notifier.dart';

class EmployeesListScreen extends ConsumerWidget {
  const EmployeesListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final employees = ref.watch(employeesListNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Employees')),
      body: AsyncValueView<PagedResult<EmployeeDetail>>(
        value: employees,
        onRetry: () => ref.invalidate(employeesListNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const EmptyStateView(message: 'No employees found.');
          }
          return ListView(
            children: [
              for (final employee in paged.items)
                ListTile(
                  leading: const CircleAvatar(
                    backgroundColor: AppColors.gray,
                    child: Icon(Icons.person_outline, color: AppColors.muted),
                  ),
                  title: Text(employee.fullName),
                  subtitle: Text('${employee.designationTitle} — ${employee.departmentName}'),
                  trailing: Text(employee.employeeCode),
                ),
              if (paged.hasNextPage)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Center(
                    child: TextButton(
                      onPressed: () =>
                          ref.read(employeesListNotifierProvider.notifier).loadNextPage(),
                      child: const Text('Load more'),
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
