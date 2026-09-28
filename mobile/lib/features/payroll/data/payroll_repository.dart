import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'payslip.dart';

class PayrollRepository {
  PayrollRepository(this._client);

  final ApiClient _client;

  Future<List<Payslip>> fetchMine({int? year}) async {
    final items = await _client.getList('/payroll/payslips', queryParameters: {'year': ?year});
    return items.map((e) => Payslip.fromJson(e as Map<String, dynamic>)).toList();
  }
}

final payrollRepositoryProvider = Provider<PayrollRepository>((ref) {
  return PayrollRepository(ref.watch(apiClientProvider));
});
