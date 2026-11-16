import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'onboarding_checklist_models.dart';

class OnboardingChecklistRepository {
  OnboardingChecklistRepository(this._client);

  final ApiClient _client;

  Future<List<OnboardingChecklistItem>> fetchMine() async {
    final items = await _client.getList('/onboarding/tasks/mine');
    return items.map((e) => OnboardingChecklistItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> complete(String taskId) {
    return _client.put('/onboarding/tasks/$taskId/complete');
  }

  Future<void> reopen(String taskId) {
    return _client.put('/onboarding/tasks/$taskId/reopen');
  }
}

final onboardingChecklistRepositoryProvider = Provider<OnboardingChecklistRepository>((ref) {
  return OnboardingChecklistRepository(ref.watch(apiClientProvider));
});
