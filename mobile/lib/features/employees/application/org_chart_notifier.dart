import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/employees_repository.dart';
import '../data/org_chart_node.dart';

class OrgChartNotifier extends AsyncNotifier<List<OrgChartNode>> {
  @override
  Future<List<OrgChartNode>> build() {
    return ref.watch(employeesRepositoryProvider).fetchOrgChart();
  }
}

final orgChartNotifierProvider =
    AsyncNotifierProvider<OrgChartNotifier, List<OrgChartNode>>(OrgChartNotifier.new);
