import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/paged_result.dart';
import '../data/notification_item.dart';
import '../data/notifications_repository.dart';

class NotificationsListNotifier extends AsyncNotifier<PagedResult<NotificationItem>> {
  @override
  Future<PagedResult<NotificationItem>> build() {
    return ref.watch(notificationsRepositoryProvider).fetchAll(page: 1);
  }

  Future<void> loadNextPage() async {
    final current = state.valueOrNull;
    if (current == null || !current.hasNextPage) return;
    final nextPage =
        await ref.read(notificationsRepositoryProvider).fetchAll(page: current.page + 1);
    state = AsyncData(
      PagedResult(
        items: [...current.items, ...nextPage.items],
        page: nextPage.page,
        pageSize: nextPage.pageSize,
        total: nextPage.total,
      ),
    );
  }

  Future<void> markRead(String id) async {
    await ref.read(notificationsRepositoryProvider).markRead(id);
    final current = state.valueOrNull;
    if (current == null) return;
    state = AsyncData(
      PagedResult(
        items: [
          for (final item in current.items)
            if (item.id == id)
              NotificationItem(
                id: item.id,
                title: item.title,
                body: item.body,
                isRead: true,
                sentAt: item.sentAt,
              )
            else
              item,
        ],
        page: current.page,
        pageSize: current.pageSize,
        total: current.total,
      ),
    );
  }
}

final notificationsListNotifierProvider =
    AsyncNotifierProvider<NotificationsListNotifier, PagedResult<NotificationItem>>(
  NotificationsListNotifier.new,
);

class NotificationPrefsNotifier extends AsyncNotifier<List<dynamic>> {
  @override
  Future<List<dynamic>> build() {
    return ref.watch(notificationsRepositoryProvider).fetchPreferences();
  }

  Future<void> updatePrefs({
    required String channel,
    required String category,
    required bool isEnabled,
  }) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      await ref.read(notificationsRepositoryProvider).setPreference(
            channel: channel,
            category: category,
            isEnabled: isEnabled,
          );
      return ref.read(notificationsRepositoryProvider).fetchPreferences();
    });
  }
}

final notificationPrefsNotifierProvider =
    AsyncNotifierProvider<NotificationPrefsNotifier, List<dynamic>>(
  NotificationPrefsNotifier.new,
);
