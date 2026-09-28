import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';

import '../../../core/auth_session/auth_notifier.dart';
import '../../../core/network/api_client.dart';
import '../data/employee_detail.dart';
import '../data/profile_repository.dart';

class ProfileNotifier extends AsyncNotifier<EmployeeDetail> {
  @override
  Future<EmployeeDetail> build() async {
    final authState = ref.watch(authNotifierProvider);
    final employeeId = authState.valueOrNull?.profile?.id;
    if (employeeId == null) {
      throw StateError('Cannot load profile: no authenticated employee.');
    }
    return ref.watch(profileRepositoryProvider).fetch(employeeId);
  }

  Future<bool> updateFields(Map<String, dynamic> fields) async {
    state = const AsyncLoading();
    final result = await AsyncValue.guard(() async {
      await ref.read(profileRepositoryProvider).updateSelf(fields);
      final employeeId = ref.read(authNotifierProvider).valueOrNull!.profile!.id;
      return ref.read(profileRepositoryProvider).fetch(employeeId);
    });
    state = result;
    if (!result.hasError) {
      // Keep AuthNotifier's session-defining profile in sync so the shell
      // header never diverges from what this screen just saved — this is
      // exactly the kind of two-source divergence the rewrite exists to
      // prevent, just in a subtler spot than the original login/restore bug.
      ref.invalidate(authNotifierProvider);
    }
    return !result.hasError;
  }

  Future<String?> downloadPhoto(String remotePath) async {
    try {
      final dir = await getTemporaryDirectory();
      final localPath = '${dir.path}/profile_photo_${remotePath.hashCode}.jpg';
      await ref.read(apiClientProvider).downloadToFile(remotePath, localPath);
      return localPath;
    } catch (_) {
      return null;
    }
  }

  Future<bool> updatePhoto(String filePath) async {
    state = const AsyncLoading();
    final result = await AsyncValue.guard(() async {
      await ref.read(profileRepositoryProvider).updateProfileImage(filePath);
      final employeeId = ref.read(authNotifierProvider).valueOrNull!.profile!.id;
      return ref.read(profileRepositoryProvider).fetch(employeeId);
    });
    state = result;
    if (!result.hasError) {
      ref.invalidate(authNotifierProvider);
    }
    return !result.hasError;
  }
}

final profileNotifierProvider =
    AsyncNotifierProvider<ProfileNotifier, EmployeeDetail>(ProfileNotifier.new);
