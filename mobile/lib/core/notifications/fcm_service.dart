import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/notifications/data/notifications_repository.dart';
import 'badge_service.dart';
import 'local_notifications_service.dart';
import 'notification_bus.dart';

/// The message/token-refresh listeners must attach exactly once per
/// app-process lifetime, not once per login/restore call — otherwise
/// repeated login/logout cycles in one session stack up duplicate listeners,
/// each firing on every future message. _listenersAttached guards that.
///
/// Device *registration* is intentionally allowed to retry on every login —
/// unlike the listeners, a failed registration (e.g. the device had no
/// network the first time, or the permission prompt hadn't been answered
/// yet) must not permanently strand that device with no push notifications
/// for the rest of the app's lifetime.
class FcmService {
  FcmService(this._repository, this._bus, this._ref);

  final NotificationsRepository _repository;
  final NotificationBus _bus;
  final Ref _ref;
  bool _listenersAttached = false;

  Future<void> initOnce() async {
    try {
      final messaging = FirebaseMessaging.instance;
      final settings = await messaging.requestPermission();
      if (kDebugMode) {
        debugPrint('FCM permission status: ${settings.authorizationStatus}');
      }

      final token = await messaging.getToken();
      if (kDebugMode) {
        debugPrint('FCM token: ${token ?? "null (getToken failed)"}');
      }
      if (token != null) {
        await _safeRegister(token);
      }

      if (_listenersAttached) return;
      _listenersAttached = true;

      messaging.onTokenRefresh.listen(_safeRegister);

      FirebaseMessaging.onMessage.listen((message) {
        _bus.publish(message);
        LocalNotificationsService.showFromRemoteMessage(message);
        _ref.read(badgeCountProvider.notifier).increment();
        if (message.data['type'] == 'document') {
          _ref.read(documentsBadgeCountProvider.notifier).increment();
        }
      });
    } catch (e) {
      // Firebase isn't configured for every platform this app runs on (e.g.
      // the web build has no Firebase config yet) — push notifications are a
      // convenience layer, never something that should block app startup.
      // Still surfaced in debug builds so a real misconfiguration (missing
      // google-services.json, no Play Services on the test device, etc.) is
      // visible instead of silently producing "no notifications ever arrive."
      if (kDebugMode) debugPrint('FCM initOnce failed: $e');
    }
  }

  Future<void> _safeRegister(String token) async {
    try {
      await _repository.registerDevice(token);
    } catch (e) {
      // Device registration is best-effort; a transient failure here must
      // never crash app startup or block login.
      if (kDebugMode) debugPrint('FCM device registration failed: $e');
    }
  }
}

final fcmServiceProvider = Provider<FcmService>((ref) {
  return FcmService(
    ref.watch(notificationsRepositoryProvider),
    ref.watch(notificationBusProvider),
    ref,
  );
});
