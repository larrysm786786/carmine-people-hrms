import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'signature_document_models.dart';

class SignatureDocumentRepository {
  SignatureDocumentRepository(this._client);

  final ApiClient _client;

  Future<List<SignatureDocument>> fetchPending() async {
    final items = await _client.getList('/signature-documents/pending');
    return items.map((e) => SignatureDocument.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> sign(String documentId, String signatureImageBase64) {
    return _client.post('/signature-documents/$documentId/sign', data: {
      'signatureImageBase64': signatureImageBase64,
    });
  }
}

final signatureDocumentRepositoryProvider = Provider<SignatureDocumentRepository>((ref) {
  return SignatureDocumentRepository(ref.watch(apiClientProvider));
});
