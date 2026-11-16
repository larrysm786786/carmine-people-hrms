import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/onboarding_checklist_models.dart';
import '../data/onboarding_checklist_repository.dart';

class OnboardingChecklistNotifier extends AsyncNotifier<List<OnboardingChecklistItem>> {
  @override
  Future<List<OnboardingChecklistItem>> build() {
    return ref.watch(onboardingChecklistRepositoryProvider).fetchMine();
  }

  Future<void> toggle(OnboardingChecklistItem item) async {
    final repo = ref.read(onboardingChecklistRepositoryProvider);
    if (item.isCompleted) {
      await repo.reopen(item.id);
    } else {
      await repo.complete(item.id);
    }
    ref.invalidateSelf();
  }
}

final onboardingChecklistNotifierProvider =
    AsyncNotifierProvider<OnboardingChecklistNotifier, List<OnboardingChecklistItem>>(
  OnboardingChecklistNotifier.new,
);
