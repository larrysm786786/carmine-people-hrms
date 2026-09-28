import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/payroll_repository.dart';
import '../data/payslip.dart';

class PayrollNotifier extends AsyncNotifier<List<Payslip>> {
  @override
  Future<List<Payslip>> build() {
    return ref.watch(payrollRepositoryProvider).fetchMine();
  }
}

final payrollNotifierProvider =
    AsyncNotifierProvider<PayrollNotifier, List<Payslip>>(PayrollNotifier.new);
