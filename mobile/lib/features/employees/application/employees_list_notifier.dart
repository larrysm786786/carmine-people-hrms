import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../../profile/data/employee_detail.dart';
import '../data/employees_repository.dart';

class EmployeesListNotifier extends AsyncNotifier<PagedResult<EmployeeDetail>> {
  @override
  Future<PagedResult<EmployeeDetail>> build() {
    return ref.watch(employeesRepositoryProvider).fetchAll(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage = await ref.read(employeesRepositoryProvider).fetchAll(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }
}

final employeesListNotifierProvider =
    AsyncNotifierProvider<EmployeesListNotifier, PagedResult<EmployeeDetail>>(
  EmployeesListNotifier.new,
);
