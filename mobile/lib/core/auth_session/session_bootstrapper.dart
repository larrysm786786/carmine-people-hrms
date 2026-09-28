import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/profile_cache_store.dart';
import 'auth_repository.dart';
import 'auth_state.dart';
import 'employee_profile.dart';

/// The single function every session-producing path funnels through. Both
/// login (with credentials) and restore (no credentials) end up calling
/// _buildAuthenticatedState with the resolved profile — there is no second,
/// divergent construction site for "authenticated" AuthData anywhere in the
/// app. This is the direct structural fix for the original bug where login()
/// and _restoreSession() each built AuthState independently and one of them
/// forgot to populate profile.
class SessionBootstrapper {
  SessionBootstrapper(this._authRepository, this._profileCacheStore);

  final AuthRepository _authRepository;
  final ProfileCacheStore _profileCacheStore;

  Future<AuthData> resolveSession({String? email, String? password}) async {
    if (email != null && password != null) {
      final result = await _authRepository.login(email, password);
      return _buildAuthenticatedState(result.profile);
    }

    final result = await _authRepository.restore();
    if (result == null) return const AuthData.unauthenticated();
    return _buildAuthenticatedState(result.profile);
  }

  Future<AuthData> _buildAuthenticatedState(EmployeeProfile profile) async {
    await _profileCacheStore.save(profile);
    return AuthData.authenticated(profile);
  }

  Future<void> logout() async {
    await _authRepository.logout();
    await _profileCacheStore.clear();
  }
}

final sessionBootstrapperProvider = Provider<SessionBootstrapper>((ref) {
  return SessionBootstrapper(
    ref.watch(authRepositoryProvider),
    ref.watch(profileCacheStoreProvider),
  );
});
