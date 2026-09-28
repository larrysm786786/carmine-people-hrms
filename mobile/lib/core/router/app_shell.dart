import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../notifications/badge_service.dart';

class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _alertsTabIndex = 3;

  static const _tabs = [
    (icon: Icons.home_outlined, selectedIcon: Icons.home, label: 'Home'),
    (icon: Icons.calendar_today_outlined, selectedIcon: Icons.calendar_today, label: 'Attendance'),
    (icon: Icons.beach_access_outlined, selectedIcon: Icons.beach_access, label: 'Leave'),
    (icon: Icons.notifications_outlined, selectedIcon: Icons.notifications, label: 'Alerts'),
    (icon: Icons.person_outline, selectedIcon: Icons.person, label: 'Profile'),
  ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final unreadCount = ref.watch(badgeCountProvider);

    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: navigationShell.currentIndex,
        onDestinationSelected: (index) => navigationShell.goBranch(
          index,
          initialLocation: index == navigationShell.currentIndex,
        ),
        destinations: [
          for (var i = 0; i < _tabs.length; i++)
            NavigationDestination(
              icon: i == _alertsTabIndex
                  ? Badge.count(count: unreadCount, isLabelVisible: unreadCount > 0, child: Icon(_tabs[i].icon))
                  : Icon(_tabs[i].icon),
              selectedIcon: i == _alertsTabIndex
                  ? Badge.count(count: unreadCount, isLabelVisible: unreadCount > 0, child: Icon(_tabs[i].selectedIcon))
                  : Icon(_tabs[i].selectedIcon),
              label: _tabs[i].label,
            ),
        ],
      ),
    );
  }
}
