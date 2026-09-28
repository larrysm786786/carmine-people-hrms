import 'dart:async';

import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class NotificationBus {
  final _controller = StreamController<RemoteMessage>.broadcast();

  Stream<RemoteMessage> get stream => _controller.stream;

  void publish(RemoteMessage message) => _controller.add(message);
}

final notificationBusProvider = Provider<NotificationBus>((ref) => NotificationBus());
