import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth_session/auth_notifier.dart';
import '../data/document.dart';
import '../data/documents_repository.dart';

class DocumentsNotifier extends AsyncNotifier<List<DocumentItem>> {
  @override
  Future<List<DocumentItem>> build() async {
    final employeeId = ref.watch(authNotifierProvider).valueOrNull?.profile?.id;
    if (employeeId == null) {
      throw StateError('Cannot load documents: no authenticated employee.');
    }
    return ref.watch(documentsRepositoryProvider).fetchAll(employeeId);
  }

  Future<String> download(DocumentItem document) {
    return ref.read(documentsRepositoryProvider).download(document);
  }

  Future<bool> uploadIdProof(String filePath) async {
    final result = await AsyncValue.guard(() async {
      await ref.read(documentsRepositoryProvider).uploadIdProof(filePath);
      return build();
    });
    if (!result.hasError) state = result;
    return !result.hasError;
  }

  Future<bool> uploadProfilePhoto(String filePath) async {
    final result = await AsyncValue.guard(() async {
      await ref.read(documentsRepositoryProvider).uploadProfilePhoto(filePath);
      return build();
    });
    if (!result.hasError) state = result;
    return !result.hasError;
  }
}

final documentsNotifierProvider =
    AsyncNotifierProvider<DocumentsNotifier, List<DocumentItem>>(DocumentsNotifier.new);
