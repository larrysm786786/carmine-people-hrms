import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:geolocator/geolocator.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:workmanager/workmanager.dart';

import '../notifications/local_notifications_service.dart';
import '../storage/profile_cache_store.dart';

const _officeProximityTaskName = 'officeProximityCheck';
const _officeProximityUniqueName = 'office-proximity-check';
const _lastNotifiedDateKey = 'office_proximity_last_notified_date';
const _officeProximityNotificationId = 90001;

// Baked into the release APK by the same --dart-define the app itself uses
// (see mobile-ci.yml); duplicated here because this isolate never runs the
// app's own ApiConfig/DI graph, only this one top-level function.
const _apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'https://carmine-people-api.onrender.com/api/v1',
);

// Runs in a separate background isolate that Android (re-)spawns on its own
// schedule, independent of whether the app process is alive — no Riverpod,
// no widget tree, no access to anything the foreground isolate set up. Must
// rebuild its own minimal network/storage/notification stack from scratch.
// Complements GeofenceRadiusMonitor (same "you're at the office" nudge),
// which only runs while the app process is alive; this is what covers the
// case that monitor's own doc comment calls out — the app fully killed.
@pragma('vm:entry-point')
void backgroundCheckinCallbackDispatcher() {
  Workmanager().executeTask((task, inputData) async {
    try {
      await _checkOfficeProximityAndNotify();
    } catch (_) {
      // Best-effort periodic check; any failure (no network, no background
      // location permission, expired token, GPS off) must never crash the
      // task — it simply tries again at the next scheduled run.
    }
    return true;
  });
}

Future<void> _checkOfficeProximityAndNotify() async {
  const secureStorage = FlutterSecureStorage();
  final accessToken = await secureStorage.read(key: 'access_token');
  if (accessToken == null) return;

  final permission = await Geolocator.checkPermission();
  if (permission != LocationPermission.always) return;

  final dio = Dio(BaseOptions(
    baseUrl: _apiBaseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 15),
    headers: {
      'Authorization': 'Bearer $accessToken',
      'ngrok-skip-browser-warning': 'true',
    },
  ));

  final todayResponse = await dio.get('/attendance/today');
  final todayData = todayResponse.data?['data'] as Map<String, dynamic>?;
  final isCheckedIn = todayData?['isCheckedIn'] as bool? ?? false;

  final prefs = await SharedPreferences.getInstance();
  final today = DateTime.now().toIso8601String().split('T').first;

  if (isCheckedIn) {
    // Clears the "already nudged today" flag so a genuinely new day (next
    // check-in cycle) can nudge again if needed.
    await prefs.remove(_lastNotifiedDateKey);
    return;
  }
  if (prefs.getString(_lastNotifiedDateKey) == today) {
    return; // Already nudged once today — avoid repeating every 15 minutes.
  }

  final profile = await ProfileCacheStore().read();
  final locationId = profile?.locationId;
  if (locationId == null) return;

  final locationsResponse = await dio.get('/locations');
  final locations = (locationsResponse.data?['data'] as List<dynamic>?) ?? [];
  Map<String, dynamic>? office;
  for (final raw in locations) {
    final candidate = raw as Map<String, dynamic>;
    if (candidate['id'] == locationId) {
      office = candidate;
      break;
    }
  }
  if (office == null) return;

  final lat = (office['latitude'] as num?)?.toDouble();
  final lng = (office['longitude'] as num?)?.toDouble();
  final radius = (office['geofenceRadiusMeters'] as num?)?.toDouble();
  if (lat == null || lng == null || radius == null) return;

  final position = await Geolocator.getCurrentPosition();
  final distance = Geolocator.distanceBetween(position.latitude, position.longitude, lat, lng);
  if (distance > radius) return;

  await LocalNotificationsService.init();
  await LocalNotificationsService.showLocal(
    id: _officeProximityNotificationId,
    title: "You're at the office",
    body: "Don't forget to check in for today's attendance.",
  );
  await prefs.setString(_lastNotifiedDateKey, today);
}

/// Registers the periodic background check — call once from app startup,
/// regardless of whether background location permission is granted yet (the
/// task itself checks and silently no-ops if it isn't).
class BackgroundCheckinService {
  Future<void> initialize() async {
    await Workmanager().initialize(backgroundCheckinCallbackDispatcher);
    await Workmanager().registerPeriodicTask(
      _officeProximityUniqueName,
      _officeProximityTaskName,
      frequency: const Duration(minutes: 15),
      existingWorkPolicy: ExistingPeriodicWorkPolicy.keep,
    );
  }

  Future<LocationPermission> currentPermission() => Geolocator.checkPermission();

  /// Android only offers "Allow all the time" once foreground ("while using
  /// the app") location is already granted — call this only after that has
  /// already succeeded, never as the very first location prompt.
  Future<bool> requestBackgroundPermission() async {
    final current = await Geolocator.checkPermission();
    if (current == LocationPermission.always) return true;
    if (current != LocationPermission.whileInUse) return false;
    final requested = await Geolocator.requestPermission();
    return requested == LocationPermission.always;
  }
}

final backgroundCheckinServiceProvider = Provider<BackgroundCheckinService>((ref) {
  return BackgroundCheckinService();
});
