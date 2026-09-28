import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_state.dart';
import 'session_bootstrapper.dart';

/// build() is the ONLY method that ever produces state from scratch — there
/// is no separate "restore" code path that constructs AuthData differently
/// from login. Both call the same resolveSession(), which itself funnels
/// through the same private builder in SessionBootstrapper.
class AuthNotifier extends AsyncNotifier<AuthData> {
  @override
  Future<AuthData> build() {
    return ref.watch(sessionBootstrapperProvider).resolveSession();
  }

  Future<void> login(String email, String password) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(sessionBootstrapperProvider).resolveSession(email: email, password: password),
    );
  }

  Future<void> logout() async {
    await ref.read(sessionBootstrapperProvider).logout();
    state = const AsyncData(AuthData.unauthenticated());
  }

  void lock() {
    final current = state.valueOrNull;
    if (current == null || !current.isAuthenticated) return;
    state = AsyncData(current.copyWithLocked(true));
  }

  void unlock() {
    final current = state.valueOrNull;
    if (current == null || !current.isLocked) return;
    state = AsyncData(current.copyWithLocked(false));
  }
}

final authNotifierProvider = AsyncNotifierProvider<AuthNotifier, AuthData>(AuthNotifier.new);
