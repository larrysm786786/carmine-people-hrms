import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/employee_directory_item.dart';
import '../data/employees_repository.dart';

class EmployeeDirectoryNotifier extends AsyncNotifier<List<EmployeeDirectoryItem>> {
  @override
  Future<List<EmployeeDirectoryItem>> build() {
    return ref.watch(employeesRepositoryProvider).fetchDirectory();
  }
}

final employeeDirectoryNotifierProvider =
    AsyncNotifierProvider<EmployeeDirectoryNotifier, List<EmployeeDirectoryItem>>(
  EmployeeDirectoryNotifier.new,
);
