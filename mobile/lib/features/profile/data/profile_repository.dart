import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'employee_detail.dart';

class ProfileRepository {
  ProfileRepository(this._client);

  final ApiClient _client;

  Future<EmployeeDetail> fetch(String employeeId) async {
    final data = await _client.get('/employees/$employeeId');
    return EmployeeDetail.fromJson(data ?? const {});
  }

  Future<void> updateSelf(Map<String, dynamic> fields) async {
    await _client.put('/employees/me', data: fields);
  }

  Future<void> updateProfileImage(String filePath) async {
    final uploadResult = await _client.postMultipart('/documents/upload/profile-photo', filePath);
    final documentId = uploadResult?['id'] as String?;
    if (documentId == null) return;
    await _client.put('/employees/me/profile-image', data: {
      'profileImageUrl': '/documents/$documentId/content',
    });
  }
}

final profileRepositoryProvider = Provider<ProfileRepository>((ref) {
  return ProfileRepository(ref.watch(apiClientProvider));
});
