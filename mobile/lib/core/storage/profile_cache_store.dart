import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../auth_session/employee_profile.dart';

/// Caches the last-known profile so a cold app-restore can render instantly
/// while resolveSession() re-validates in the background — never a substitute
/// for a fresh call, only a first-paint hint.
class ProfileCacheStore {
  static const _key = 'cached_employee_profile';

  Future<EmployeeProfile?> read() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_key);
    if (raw == null) return null;
    try {
      return EmployeeProfile.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return null;
    }
  }

  Future<void> save(EmployeeProfile profile) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, jsonEncode(profile.toJson()));
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_key);
  }
}

final profileCacheStoreProvider = Provider<ProfileCacheStore>((ref) => ProfileCacheStore());
