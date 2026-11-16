import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/pulse_survey_models.dart';
import '../data/pulse_survey_repository.dart';

class PulseSurveyPendingNotifier extends AsyncNotifier<List<PulseSurvey>> {
  @override
  Future<List<PulseSurvey>> build() {
    return ref.watch(pulseSurveyRepositoryProvider).fetchPending();
  }

  Future<void> respond(String surveyId, {required int rating, String? comment, required bool isAnonymous}) async {
    await ref.read(pulseSurveyRepositoryProvider).respond(
          surveyId,
          rating: rating,
          comment: comment,
          isAnonymous: isAnonymous,
        );
    ref.invalidateSelf();
  }
}

final pulseSurveyPendingNotifierProvider =
    AsyncNotifierProvider<PulseSurveyPendingNotifier, List<PulseSurvey>>(PulseSurveyPendingNotifier.new);
