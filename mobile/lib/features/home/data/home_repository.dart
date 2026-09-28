import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'home_summary.dart';

class HomeRepository {
  HomeRepository(this._client);

  final ApiClient _client;

  Future<HomeSummary> fetchSummary() async {
    final data = await _client.get('/attendance/home-summary');
    return HomeSummary.fromJson(data ?? const {});
  }
}

final homeRepositoryProvider = Provider<HomeRepository>((ref) {
  return HomeRepository(ref.watch(apiClientProvider));
});
