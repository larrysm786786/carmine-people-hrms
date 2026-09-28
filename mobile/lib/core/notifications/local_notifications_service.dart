import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

/// Displays a system-tray notification for a push message. FCM only
/// auto-shows a "notification" payload when the app is fully backgrounded/
/// killed on Android — a foreground app, and any data-only message, must be
/// shown manually via this local-notifications channel.
class LocalNotificationsService {
  static const _channelId = 'carmine_people_default';
  static final _plugin = FlutterLocalNotificationsPlugin();
  static bool _initialized = false;

  static Future<void> init() async {
    if (_initialized) return;
    _initialized = true;

    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings();
    await _plugin.initialize(
      settings: const InitializationSettings(android: androidSettings, iOS: iosSettings),
    );

    const channel = AndroidNotificationChannel(
      _channelId,
      'Announcements & Updates',
      description: 'Notifications from HR/Admin and app activity',
      importance: Importance.high,
    );
    await _plugin
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(channel);
  }

  static Future<void> showFromRemoteMessage(RemoteMessage message) async {
    final notification = message.notification;
    final title = notification?.title ?? message.data['title'] as String? ?? 'Carmine People';
    final body = notification?.body ?? message.data['body'] as String? ?? '';
    await showLocal(id: message.hashCode, title: title, body: body);
  }

  /// General-purpose system-tray notification for anything generated
  /// on-device (not from a push payload) — e.g. the office-WiFi-connected
  /// check-in reminder. Plays the device's default notification sound
  /// (playSound defaults true, sound left null = system default) exactly
  /// like a push notification would.
  static Future<void> showLocal({required int id, required String title, required String body}) async {
    await _plugin.show(
      id: id,
      title: title,
      body: body,
      notificationDetails: const NotificationDetails(
        android: AndroidNotificationDetails(
          _channelId,
          'Announcements & Updates',
          importance: Importance.high,
          priority: Priority.high,
          playSound: true,
        ),
        iOS: DarwinNotificationDetails(presentSound: true),
      ),
    );
  }
}
