import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/unique_file_namer.dart';
import 'document.dart';

class DocumentsRepository {
  DocumentsRepository(this._client);

  final ApiClient _client;

  Future<List<DocumentItem>> fetchAll(String employeeId) async {
    final items = await _client.getList('/employees/$employeeId/documents');
    return items.map((e) => DocumentItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<String> download(DocumentItem document) async {
    final dir = await getTemporaryDirectory();
    final fileName = UniqueFileNamer.forDocument(document.id, document.extension);
    final filePath = '${dir.path}/$fileName';

    await _client.downloadToFile('/documents/${document.id}/content', filePath);
    return filePath;
  }

  Future<String> uploadIdProof(String filePath) async {
    final data = await _client.postMultipart('/documents/upload/id-proof', filePath);
    return data?['id'] as String? ?? '';
  }

  Future<String> uploadProfilePhoto(String filePath) async {
    final data = await _client.postMultipart('/documents/upload/profile-photo', filePath);
    return data?['id'] as String? ?? '';
  }
}

final documentsRepositoryProvider = Provider<DocumentsRepository>((ref) {
  return DocumentsRepository(ref.watch(apiClientProvider));
});
