import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';

class AiRepository {
  AiRepository(this._client);

  final ApiClient _client;

  Future<bool> fetchStatus() async {
    final data = await _client.get('/ai/status');
    return data?['configured'] as bool? ?? false;
  }

  // history: prior turns as {"role": "user" | "assistant", "content": "..."} — required by
  // the backend (AiChatRequest.History), which is not nullable/defaulted there.
  Future<String> sendMessage(String message, List<Map<String, String>> history) async {
    final data = await _client.post('/ai/chat', data: {'message': message, 'history': history});
    return data?['reply'] as String? ?? '';
  }
}

final aiRepositoryProvider = Provider<AiRepository>((ref) {
  return AiRepository(ref.watch(apiClientProvider));
});
