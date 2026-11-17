import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import '../../profile/data/employee_detail.dart';
import 'celebration.dart';
import 'employee_directory_item.dart';
import 'org_chart_node.dart';

class EmployeesRepository {
  EmployeesRepository(this._client);

  final ApiClient _client;

  Future<PagedResult<EmployeeDetail>> fetchAll({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/employees', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, EmployeeDetail.fromJson);
  }

  Future<List<OrgChartNode>> fetchOrgChart() async {
    final items = await _client.getList('/employees/org-chart');
    return items.map((e) => OrgChartNode.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<Celebration>> fetchUpcomingCelebrations({int withinDays = 30}) async {
    final items = await _client.getList('/employees/celebrations', queryParameters: {
      'withinDays': withinDays,
    });
    return items.map((e) => Celebration.fromJson(e as Map<String, dynamic>)).toList();
  }

  // Name-only list of every other active employee, not gated behind employee.view — backs
  // pickers (like the shift-swap counterpart) that any employee must be able to use.
  Future<List<EmployeeDirectoryItem>> fetchDirectory() async {
    final items = await _client.getList('/employees/directory');
    return items.map((e) => EmployeeDirectoryItem.fromJson(e as Map<String, dynamic>)).toList();
  }
}

final employeesRepositoryProvider = Provider<EmployeesRepository>((ref) {
  return EmployeesRepository(ref.watch(apiClientProvider));
});
