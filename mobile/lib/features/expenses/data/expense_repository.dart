import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'expense_models.dart';

class ExpenseRepository {
  ExpenseRepository(this._client);

  final ApiClient _client;

  Future<PagedResult<Expense>> fetchMine({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/expenses/mine', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, Expense.fromJson);
  }

  Future<void> submit({
    required String category,
    required double amount,
    required String expenseDate,
    String? description,
  }) async {
    await _client.post('/expenses', data: {
      'category': category,
      'amount': amount,
      'expenseDate': expenseDate,
      'description': ?description,
    });
  }

  Future<PagedResult<Expense>> fetchPendingApprovals({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/expenses/team-pending', queryParameters: {'page': page, 'pageSize': pageSize});
    return PagedResult.fromRawPage(raw, Expense.fromJson);
  }

  Future<void> approve(String expenseId, {String? comment}) {
    return _client.put('/expenses/$expenseId/approve', data: {'comment': ?comment});
  }

  Future<void> reject(String expenseId, {String? comment}) {
    return _client.put('/expenses/$expenseId/reject', data: {'comment': ?comment});
  }
}

final expenseRepositoryProvider = Provider<ExpenseRepository>((ref) {
  return ExpenseRepository(ref.watch(apiClientProvider));
});
