import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/api_client.dart';
import '../network/api_exception.dart';
import '../storage/token_storage.dart';
import 'employee_profile.dart';

class LoginResult {
  LoginResult(this.profile);
  final EmployeeProfile profile;
}

class AuthRepository {
  AuthRepository(this._client, this._tokenStorage);

  final ApiClient _client;
  final TokenStorage _tokenStorage;

  Future<LoginResult> login(String email, String password) async {
    final data = await _client.post('/auth/login', data: {
      'email': email,
      'password': password,
    });
    if (data == null) throw const UnknownApiException();
    return _saveAndExtractProfile(data);
  }

  /// Restore path: no credentials, uses the stored refresh token. Returns
  /// null (not an error) when there is simply no session to restore.
  Future<LoginResult?> restore() async {
    final refreshToken = await _tokenStorage.readRefreshToken();
    if (refreshToken == null) return null;
    final data = await _client.post('/auth/refresh', data: {
      'refreshToken': refreshToken,
    });
    if (data == null) return null;
    return _saveAndExtractProfile(data);
  }

  Future<LoginResult> _saveAndExtractProfile(Map<String, dynamic> data) async {
    await _tokenStorage.saveTokens(
      accessToken: data['accessToken'] as String,
      refreshToken: data['refreshToken'] as String,
    );
    final profileJson = data['employee'] as Map<String, dynamic>;
    return LoginResult(EmployeeProfile.fromJson(profileJson));
  }

  Future<void> logout() async {
    final refreshToken = await _tokenStorage.readRefreshToken();
    try {
      if (refreshToken != null) {
        await _client.post('/auth/logout', data: {'refreshToken': refreshToken});
      }
    } catch (_) {
      // Logout is best-effort server-side; local token wipe below always happens.
    }
    await _tokenStorage.clear();
  }

  Future<void> forgotPassword(String email) async {
    await _client.post('/auth/forgot-password', data: {'email': email});
  }
}

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(apiClientProvider), ref.watch(tokenStorageProvider));
});
