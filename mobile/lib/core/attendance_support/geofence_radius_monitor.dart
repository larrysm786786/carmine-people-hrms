import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../../features/attendance/application/today_attendance_notifier.dart';
import '../auth_session/auth_notifier.dart';
import '../notifications/local_notifications_service.dart';
import 'geofence_prompt_service.dart';
import 'office_location.dart';

/// Watches the device's GPS position while the app is open or backgrounded
/// (not fully killed) and fires an immediate system-tray notification the
/// moment the employee's live position crosses into their office's geofence
/// — "you're at the office, don't forget to check in" — rather than waiting
/// for them to open the Home screen (the separate, in-app-dialog geofence
/// prompt in home_screen.dart). Mirrors WifiOfficeConnectionMonitor's
/// design/limits: this is a Dart-level stream, so it only runs as long as
/// Android keeps the app process alive in the background, not after the OS
/// or the user kills it — a true kill-state geofence trigger needs a native
/// Android foreground service, which this intentionally avoids to not force
/// a permanent "tracking your location" notification on every employee.
class GeofenceRadiusMonitor {
  GeofenceRadiusMonitor(this._ref);

  final Ref _ref;
  StreamSubscription<Position>? _subscription;
  OfficeLocation? _office;
  bool _wasInside = false;
  bool _started = false;

  void start() {
    if (_started) return;
    _started = true;

    const settings = LocationSettings(accuracy: LocationAccuracy.high, distanceFilter: 50);
    _subscription = Geolocator.getPositionStream(locationSettings: settings).listen(
      _onPosition,
      onError: (_) {}, // best-effort — permission denial/GPS off must never crash the app
    );
  }

  void stop() {
    _subscription?.cancel();
    _subscription = null;
    _office = null;
    _wasInside = false;
    _started = false;
  }

  Future<void> _onPosition(Position position) async {
    try {
      final isCheckedIn = _ref.read(todayAttendanceNotifierProvider).valueOrNull?.isCheckedIn ?? false;
      if (isCheckedIn) return; // already checked in — no need to nudge

      final locationId = _ref.read(authNotifierProvider).valueOrNull?.profile?.locationId;
      if (locationId == null) return;

      final geofenceService = _ref.read(geofencePromptServiceProvider);
      _office ??= await geofenceService.fetchLocation(locationId);
      final office = _office;
      if (office == null) return;

      final isInside = await geofenceService.isWithinGeofence(office, position);
      if (isInside && !_wasInside) {
        await LocalNotificationsService.showLocal(
          id: office.id.hashCode,
          title: "You're at the office",
          body: "Don't forget to check in for today's attendance.",
        );
      }
      _wasInside = isInside;
    } catch (_) {
      // Best-effort convenience nudge — a transient API failure must never crash the app.
    }
  }
}

final geofenceRadiusMonitorProvider = Provider<GeofenceRadiusMonitor>((ref) {
  final monitor = GeofenceRadiusMonitor(ref);
  ref.onDispose(monitor.stop);
  return monitor;
});
