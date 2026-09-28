import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/notifications/badge_service.dart';
import 'core/notifications/local_notifications_service.dart';

/// Top-level (non-class) function required by firebase_messaging: the OS
/// invokes this in a separate isolate when a data message arrives while the
/// app is backgrounded or fully killed, so it cannot depend on any state
/// from the running app widget tree.
@pragma('vm:entry-point')
Future<void> _firebaseBackgroundMessageHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  await LocalNotificationsService.showFromRemoteMessage(message);
  // App is backgrounded/killed here — this is exactly the "carmine app
  // closed" case from the badge requirement, so bump the launcher badge
  // (WhatsApp-style 1, 2, 3...) alongside the system-tray notification.
  await BadgeService.increment();
  if (message.data['type'] == 'document') {
    await DocumentsBadgeService.increment();
  }
}

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await Firebase.initializeApp();
    FirebaseMessaging.onBackgroundMessage(_firebaseBackgroundMessageHandler);
    await LocalNotificationsService.init();
  } catch (_) {
    // Firebase not configured for this platform yet — app still works,
    // just without push notifications.
  }
  runApp(const ProviderScope(child: CarminePeopleApp()));
}
