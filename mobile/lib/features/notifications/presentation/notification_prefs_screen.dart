import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../application/notifications_notifiers.dart';

const _channels = ['Push', 'InApp', 'Email'];

class NotificationPrefsScreen extends ConsumerStatefulWidget {
  const NotificationPrefsScreen({super.key});

  @override
  ConsumerState<NotificationPrefsScreen> createState() => _NotificationPrefsScreenState();
}

class _NotificationPrefsScreenState extends ConsumerState<NotificationPrefsScreen> {
  final _categoryController = TextEditingController();
  String _channel = _channels.first;

  @override
  void dispose() {
    _categoryController.dispose();
    super.dispose();
  }

  Future<void> _add() async {
    final category = _categoryController.text.trim();
    if (category.isEmpty) return;
    await ref.read(notificationPrefsNotifierProvider.notifier).updatePrefs(
          channel: _channel,
          category: category,
          isEnabled: true,
        );
    _categoryController.clear();
  }

  Future<void> _toggle(Map<String, dynamic> pref, bool value) async {
    await ref.read(notificationPrefsNotifierProvider.notifier).updatePrefs(
          channel: pref['channel']?.toString() ?? _channels.first,
          category: pref['category']?.toString() ?? '',
          isEnabled: value,
        );
  }

  @override
  Widget build(BuildContext context) {
    final prefs = ref.watch(notificationPrefsNotifierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Notification Preferences')),
      body: Column(
        children: [
          Expanded(
            child: AsyncValueView<List<dynamic>>(
              value: prefs,
              onRetry: () => ref.invalidate(notificationPrefsNotifierProvider),
              data: (list) {
                if (list.isEmpty) {
                  return const EmptyStateView(
                    message: 'No preferences set. Add one below.',
                    icon: Icons.notifications_outlined,
                  );
                }
                return ListView(
                  children: [
                    for (final item in list.cast<Map<String, dynamic>>())
                      SwitchListTile(
                        activeTrackColor: AppColors.carmine,
                        title: Text(item['category']?.toString() ?? ''),
                        subtitle: Text(item['channel']?.toString() ?? ''),
                        value: item['isEnabled'] as bool? ?? false,
                        onChanged: (value) => _toggle(item, value),
                      ),
                  ],
                );
              },
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  DropdownButton<String>(
                    value: _channel,
                    items: [
                      for (final c in _channels) DropdownMenuItem(value: c, child: Text(c)),
                    ],
                    onChanged: (value) => setState(() => _channel = value ?? _channels.first),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextField(
                      controller: _categoryController,
                      decoration: const InputDecoration(hintText: 'Category (e.g. Leave, Payroll)'),
                    ),
                  ),
                  IconButton(icon: const Icon(Icons.add), onPressed: _add),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
