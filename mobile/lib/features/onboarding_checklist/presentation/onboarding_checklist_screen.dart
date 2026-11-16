import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/onboarding_checklist_notifiers.dart';
import '../data/onboarding_checklist_models.dart';

class OnboardingChecklistScreen extends ConsumerWidget {
  const OnboardingChecklistScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final items = ref.watch(onboardingChecklistNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('My Onboarding Checklist')),
      body: AsyncValueView<List<OnboardingChecklistItem>>(
        value: items,
        onRetry: () => ref.invalidate(onboardingChecklistNotifierProvider),
        data: (list) {
          if (list.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(
                child: EmptyStateView(message: 'No onboarding tasks yet. Check back soon.'),
              ),
            );
          }
          final completedCount = list.where((i) => i.isCompleted).length;
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              PremiumCard(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Expanded(
                      child: LinearProgressIndicator(
                        value: completedCount / list.length,
                        color: AppColors.carmine,
                        backgroundColor: AppColors.gray,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Text('$completedCount / ${list.length}'),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              for (final item in list)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    child: CheckboxListTile(
                      value: item.isCompleted,
                      onChanged: (_) => ref.read(onboardingChecklistNotifierProvider.notifier).toggle(item),
                      activeColor: AppColors.carmine,
                      title: Text(
                        item.title,
                        style: TextStyle(
                          color: AppColors.muted,
                          fontWeight: FontWeight.w700,
                          decoration: item.isCompleted ? TextDecoration.lineThrough : null,
                        ),
                      ),
                      subtitle: item.description != null
                          ? Text(item.description!, style: const TextStyle(color: AppColors.muted, fontSize: 12))
                          : null,
                      controlAffinity: ListTileControlAffinity.leading,
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
