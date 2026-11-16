import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/signature_document_models.dart';
import '../data/signature_document_repository.dart';

class SignatureDocumentPendingNotifier extends AsyncNotifier<List<SignatureDocument>> {
  @override
  Future<List<SignatureDocument>> build() {
    return ref.watch(signatureDocumentRepositoryProvider).fetchPending();
  }

  Future<void> sign(String documentId, String signatureImageBase64) async {
    await ref.read(signatureDocumentRepositoryProvider).sign(documentId, signatureImageBase64);
    ref.invalidateSelf();
  }
}

final signatureDocumentPendingNotifierProvider =
    AsyncNotifierProvider<SignatureDocumentPendingNotifier, List<SignatureDocument>>(
  SignatureDocumentPendingNotifier.new,
);
