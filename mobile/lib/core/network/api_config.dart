import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_config_override_store.dart';

/// Debug-only convenience default so a fresh dev checkout can run without any
/// setup. Never rely on this for a real device — use --dart-define=API_BASE_URL
/// or the in-app Settings override instead.
const _debugDefaultBaseUrl = 'http://10.0.2.2:5083/api/v1';

const _compiledBaseUrl = String.fromEnvironment('API_BASE_URL');

class ApiConfig {
  ApiConfig(this._overrideStore);

  final ApiConfigOverrideStore _overrideStore;

  Future<String> resolveBaseUrl() async {
    final override = await _overrideStore.read();
    if (override != null) return override;
    if (_compiledBaseUrl.isNotEmpty) return _compiledBaseUrl;
    return _debugDefaultBaseUrl;
  }

  Future<void> setOverride(String? baseUrl) => _overrideStore.write(baseUrl);
}

final apiConfigOverrideStoreProvider = Provider<ApiConfigOverrideStore>((ref) {
  return ApiConfigOverrideStore();
});

final apiConfigProvider = Provider<ApiConfig>((ref) {
  return ApiConfig(ref.watch(apiConfigOverrideStoreProvider));
});
