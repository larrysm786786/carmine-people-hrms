import 'package:shared_preferences/shared_preferences.dart';

class ApiConfigOverrideStore {
  static const _key = 'api_base_url_override';

  Future<String?> read() async {
    final prefs = await SharedPreferences.getInstance();
    final value = prefs.getString(_key);
    return (value == null || value.isEmpty) ? null : value;
  }

  Future<void> write(String? baseUrl) async {
    final prefs = await SharedPreferences.getInstance();
    if (baseUrl == null || baseUrl.isEmpty) {
      await prefs.remove(_key);
    } else {
      await prefs.setString(_key, baseUrl);
    }
  }
}
