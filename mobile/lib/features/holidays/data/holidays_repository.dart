import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'holiday.dart';

class HolidaysRepository {
  HolidaysRepository(this._client);

  final ApiClient _client;

  Future<List<Holiday>> fetchAll() async {
    final items = await _client.getList('/holidays');
    return items.map((e) => Holiday.fromJson(e as Map<String, dynamic>)).toList();
  }
}

final holidaysRepositoryProvider = Provider<HolidaysRepository>((ref) {
  return HolidaysRepository(ref.watch(apiClientProvider));
});
