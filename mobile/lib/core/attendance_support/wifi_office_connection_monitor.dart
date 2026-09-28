import 'dart:async';

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/attendance/application/today_attendance_notifier.dart';
import '../auth_session/auth_notifier.dart';
import '../notifications/local_notifications_service.dart';
import 'geofence_prompt_service.dart';
import 'wifi_checkin_service.dart';

/// Watches for the device joining the office WiFi network (any of the
/// comma-separated SSIDs configured on the employee's Location) while the
/// app is open or backgrounded, and fires an immediate system-tray
/// notification with sound — "you're on the office network, don't forget to
/// check in" — the moment that happens, rather than waiting for the
/// employee to open the Home screen (which is the separate, in-app-dialog
/// geofence prompt in home_screen.dart).
class WifiOfficeConnectionMonitor {
  WifiOfficeConnectionMonitor(this._ref);

  final Ref _ref;
  StreamSubscription<List<ConnectivityResult>>? _subscription;
  String? _lastNotifiedSsid;
  bool _started = false;

  void start() {
    if (_started) return;
    _started = true;

    _subscription = Connectivity().onConnectivityChanged.listen((results) async {
      if (!results.contains(ConnectivityResult.wifi)) {
        // Left WiFi (or connected to something else) — allow the next
        // office-WiFi join to notify again rather than staying silent
        // forever after the first notification of the day.
        _lastNotifiedSsid = null;
        return;
      }
      await _checkOfficeWifi();
    });
  }

  void stop() {
    _subscription?.cancel();
    _subscription = null;
    _started = false;
  }

  Future<void> _checkOfficeWifi() async {
    try {
      final isCheckedIn = _ref.read(todayAttendanceNotifierProvider).valueOrNull?.isCheckedIn ?? false;
      if (isCheckedIn) return; // already checked in — no need to nudge

      final locationId = _ref.read(authNotifierProvider).valueOrNull?.profile?.locationId;
      if (locationId == null) return;

      final wifiService = _ref.read(wifiCheckinServiceProvider);
      final currentSsid = await wifiService.currentWifiName();
      if (currentSsid == null || currentSsid == _lastNotifiedSsid) return;

      final office = await _ref.read(geofencePromptServiceProvider).fetchLocation(locationId);
      if (office == null || !_isOfficeSsid(office.officeWifiSsid, currentSsid)) return;

      _lastNotifiedSsid = currentSsid;
      await LocalNotificationsService.showLocal(
        id: currentSsid.hashCode,
        title: "You're on the office network",
        body: "Don't forget to check in for today's attendance.",
      );
    } catch (_) {
      // Best-effort convenience nudge — permission issues, no location
      // configured, or a transient API failure must never crash the app.
    }
  }

  bool _isOfficeSsid(String? configuredSsids, String connectedSsid) {
    if (configuredSsids == null || configuredSsids.trim().isEmpty) return false;
    return configuredSsids
        .split(',')
        .map((s) => s.trim())
        .any((allowed) => allowed.toLowerCase() == connectedSsid.toLowerCase());
  }
}

final wifiOfficeConnectionMonitorProvider = Provider<WifiOfficeConnectionMonitor>((ref) {
  final monitor = WifiOfficeConnectionMonitor(ref);
  ref.onDispose(monitor.stop);
  return monitor;
});
