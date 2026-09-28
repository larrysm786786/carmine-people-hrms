import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import '../../profile/data/employee_detail.dart';
import 'celebration.dart';
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
}

final employeesRepositoryProvider = Provider<EmployeesRepository>((ref) {
  return EmployeesRepository(ref.watch(apiClientProvider));
});
