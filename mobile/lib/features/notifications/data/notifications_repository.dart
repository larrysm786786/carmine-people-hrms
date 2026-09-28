import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/utils/paged_result.dart';
import 'notification_item.dart';

class NotificationsRepository {
  NotificationsRepository(this._client);

  final ApiClient _client;

  Future<PagedResult<NotificationItem>> fetchAll({int page = 1, int pageSize = 20}) async {
    final raw = await _client.getPage('/notifications', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromRawPage(raw, NotificationItem.fromJson);
  }

  Future<void> markRead(String id) {
    return _client.put('/notifications/$id/read');
  }

  Future<List<dynamic>> fetchPreferences() {
    return _client.getList('/notifications/preferences');
  }

  Future<void> setPreference({
    required String channel,
    required String category,
    required bool isEnabled,
  }) {
    return _client.put('/notifications/preferences', data: {
      'channel': channel,
      'category': category,
      'isEnabled': isEnabled,
    });
  }

  Future<void> registerDevice(String fcmToken) {
    return _client.post('/notifications/devices', data: {
      'token': fcmToken,
      // Backend's RegisterDeviceRequest.Platform is a required field — omitting it
      // fails JSON model binding (400) and the request never reaches the DB, which
      // is exactly why devices were silently never getting registered before this.
      'platform': defaultTargetPlatform == TargetPlatform.iOS ? 'ios' : 'android',
    });
  }
}

final notificationsRepositoryProvider = Provider<NotificationsRepository>((ref) {
  return NotificationsRepository(ref.watch(apiClientProvider));
});
