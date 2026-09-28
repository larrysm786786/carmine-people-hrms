import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';

class AiRepository {
  AiRepository(this._client);

  final ApiClient _client;

  Future<bool> fetchStatus() async {
    final data = await _client.get('/ai/status');
    return data?['available'] as bool? ?? false;
  }

  Future<String> sendMessage(String message) async {
    final data = await _client.post('/ai/chat', data: {'message': message});
    return data?['reply'] as String? ?? '';
  }
}

final aiRepositoryProvider = Provider<AiRepository>((ref) {
  return AiRepository(ref.watch(apiClientProvider));
});
