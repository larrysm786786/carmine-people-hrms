import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/signature_document_notifiers.dart';
import '../data/signature_document_models.dart';
import 'sign_document_screen.dart';

class SignatureDocumentsScreen extends ConsumerWidget {
  const SignatureDocumentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pending = ref.watch(signatureDocumentPendingNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Documents to Sign')),
      body: AsyncValueView<List<SignatureDocument>>(
        value: pending,
        onRetry: () => ref.invalidate(signatureDocumentPendingNotifierProvider),
        data: (documents) {
          if (documents.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'Nothing pending your signature right now.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final document in documents)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute(builder: (_) => SignDocumentScreen(document: document)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.draw_outlined, color: AppColors.carmine),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            document.title,
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
