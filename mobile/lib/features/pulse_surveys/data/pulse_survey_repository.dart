import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'pulse_survey_models.dart';

class PulseSurveyRepository {
  PulseSurveyRepository(this._client);

  final ApiClient _client;

  Future<List<PulseSurvey>> fetchPending() async {
    final items = await _client.getList('/pulse-surveys/pending');
    return items.map((e) => PulseSurvey.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> respond(String surveyId, {required int rating, String? comment, required bool isAnonymous}) {
    return _client.post('/pulse-surveys/$surveyId/responses', data: {
      'rating': rating,
      'comment': ?comment,
      'isAnonymous': isAnonymous,
    });
  }
}

final pulseSurveyRepositoryProvider = Provider<PulseSurveyRepository>((ref) {
  return PulseSurveyRepository(ref.watch(apiClientProvider));
});
