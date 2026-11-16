import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/pulse_survey_notifiers.dart';
import '../data/pulse_survey_models.dart';

class PulseSurveysScreen extends ConsumerWidget {
  const PulseSurveysScreen({super.key});

  Future<void> _answer(BuildContext context, WidgetRef ref, PulseSurvey survey) async {
    int rating = 0;
    bool isAnonymous = false;
    final commentController = TextEditingController();

    final submitted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: Text(survey.question),
          content: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  children: [
                    for (var i = 1; i <= 5; i++)
                      GestureDetector(
                        onTap: () => setState(() => rating = i),
                        child: Icon(
                          i <= rating ? Icons.star : Icons.star_border,
                          color: AppColors.carmine,
                          size: 32,
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: commentController,
                  decoration: const InputDecoration(labelText: 'Comment (optional)'),
                  maxLines: 2,
                ),
                CheckboxListTile(
                  value: isAnonymous,
                  onChanged: (v) => setState(() => isAnonymous = v ?? false),
                  title: const Text('Respond anonymously'),
                  controlAffinity: ListTileControlAffinity.leading,
                  contentPadding: EdgeInsets.zero,
                ),
              ],
            ),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancel')),
            FilledButton(
              onPressed: rating == 0 ? null : () => Navigator.of(context).pop(true),
              child: const Text('Submit'),
            ),
          ],
        ),
      ),
    );

    if (submitted != true || rating == 0) return;
    await ref.read(pulseSurveyPendingNotifierProvider.notifier).respond(
          survey.id,
          rating: rating,
          comment: commentController.text.trim().isEmpty ? null : commentController.text.trim(),
          isAnonymous: isAnonymous,
        );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pending = ref.watch(pulseSurveyPendingNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Pulse Surveys')),
      body: AsyncValueView<List<PulseSurvey>>(
        value: pending,
        onRetry: () => ref.invalidate(pulseSurveyPendingNotifierProvider),
        data: (surveys) {
          if (surveys.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No surveys waiting for your response. Thanks!')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final survey in surveys)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    onTap: () => _answer(context, ref, survey),
                    child: Row(
                      children: [
                        const Icon(Icons.mood_outlined, color: AppColors.carmine),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            survey.question,
                            style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w700),
                          ),
                        ),
                        const Icon(Icons.chevron_right, color: AppColors.muted),
                      ],
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
