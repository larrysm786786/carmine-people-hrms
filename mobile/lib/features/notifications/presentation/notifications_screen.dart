import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/notifications/badge_service.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/paged_result.dart';
import '../../../core/utils/server_time.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/notifications_notifiers.dart';
import '../data/notification_item.dart';

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});

  @override
  ConsumerState<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends ConsumerState<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    // Visiting the list is the user "reading" their notifications, same as
    // opening WhatsApp's chat list clears its launcher badge.
    ref.read(badgeCountProvider.notifier).clear();
  }

  @override
  Widget build(BuildContext context) {
    final notifications = ref.watch(notificationsListNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Notifications')),
      body: AsyncValueView<PagedResult<NotificationItem>>(
        value: notifications,
        onRetry: () => ref.invalidate(notificationsListNotifierProvider),
        data: (paged) {
          if (paged.items.isEmpty) {
            return const Padding(
              padding: EdgeInsets.all(20),
              child: PremiumCard(child: EmptyStateView(message: 'No notifications yet.')),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            children: [
              for (final n in paged.items)
                Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: PremiumCard(
                    padding: const EdgeInsets.all(16),
                    onTap: n.isRead
                        ? null
                        : () => ref.read(notificationsListNotifierProvider.notifier).markRead(n.id),
                    child: Row(
                      children: [
                        Container(
                          width: 40,
                          height: 40,
                          decoration: BoxDecoration(
                            color: n.isRead ? AppColors.gray : AppColors.carmine,
                            shape: BoxShape.circle,
                          ),
                          child: Icon(
                            n.isRead ? Icons.notifications_none : Icons.notifications_active,
                            color: n.isRead ? AppColors.carmine : AppColors.white,
                            size: 18,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                n.title,
                                style: TextStyle(
                                  color: AppColors.muted,
                                  fontWeight: n.isRead ? FontWeight.w500 : FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                n.body,
                                style: const TextStyle(color: AppColors.muted, fontSize: 12),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                formatServerDateTime(n.sentAt),
                                style: const TextStyle(color: AppColors.muted, fontSize: 10),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              if (paged.hasNextPage)
                Center(
                  child: TextButton(
                    onPressed: () =>
                        ref.read(notificationsListNotifierProvider.notifier).loadNextPage(),
                    child: const Text('Load more'),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
