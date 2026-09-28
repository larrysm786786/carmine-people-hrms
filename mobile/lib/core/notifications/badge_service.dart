import 'package:app_badge_plus/app_badge_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Maintains the Android/iOS home-screen launcher badge count (the small
/// numbered dot on the app icon, like WhatsApp) for unread notifications —
/// separate from the in-app unread list, since this needs to be visible even
/// while the app is fully closed. Persisted via SharedPreferences so the
/// count survives app restarts until the user actually reads notifications.
///
/// Static methods (not tied to a Riverpod container) so the background FCM
/// isolate (main.dart's top-level handler, which has no ProviderScope) can
/// call them directly, exactly like before this class also gained a
/// Riverpod-facing notifier for the in-app "Alerts" tab badge.
class BadgeService {
  static const _prefsKey = 'unread_notification_badge_count';

  static Future<int> readCount() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getInt(_prefsKey) ?? 0;
  }

  static Future<void> _writeCount(int count) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_prefsKey, count);
    try {
      await AppBadgePlus.updateBadge(count <= 0 ? 0 : count);
    } catch (_) {
      // Some launchers/OEM skins don't support badges at all — badge display
      // is a best-effort convenience, never something that should crash the
      // notification pipeline.
    }
  }

  static Future<int> increment() async {
    final next = await readCount() + 1;
    await _writeCount(next);
    return next;
  }

  static Future<void> clear() async {
    await _writeCount(0);
  }
}

/// Riverpod-facing view of the same persisted count, so the "Alerts" bottom
/// nav tab can show a live number badge that updates the moment a
/// notification arrives while the app is open, and clears the moment the
/// user opens the notifications list — same source of truth as the launcher
/// badge (BadgeService), just observable by widgets.
class BadgeCountNotifier extends Notifier<int> {
  @override
  int build() {
    _load();
    return 0;
  }

  Future<void> _load() async {
    state = await BadgeService.readCount();
  }

  Future<void> increment() async {
    state = await BadgeService.increment();
  }

  Future<void> clear() async {
    await BadgeService.clear();
    state = 0;
  }
}

final badgeCountProvider = NotifierProvider<BadgeCountNotifier, int>(BadgeCountNotifier.new);

/// Separate unread-count badge for the Home screen's "Documents" quick-action tile — counts
/// only document-type push notifications (e.g. the monthly attendance PDF landing in
/// Documents), independent of the general Alerts/launcher badge above, so opening Alerts
/// doesn't silently clear the "you have a new document" indicator and vice versa.
class DocumentsBadgeService {
  static const _prefsKey = 'unread_documents_badge_count';

  static Future<int> readCount() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getInt(_prefsKey) ?? 0;
  }

  static Future<void> _writeCount(int count) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_prefsKey, count < 0 ? 0 : count);
  }

  static Future<int> increment() async {
    final next = await readCount() + 1;
    await _writeCount(next);
    return next;
  }

  static Future<void> clear() async {
    await _writeCount(0);
  }
}

class DocumentsBadgeCountNotifier extends Notifier<int> {
  @override
  int build() {
    _load();
    return 0;
  }

  Future<void> _load() async {
    state = await DocumentsBadgeService.readCount();
  }

  Future<void> increment() async {
    state = await DocumentsBadgeService.increment();
  }

  Future<void> clear() async {
    await DocumentsBadgeService.clear();
    state = 0;
  }
}

final documentsBadgeCountProvider =
    NotifierProvider<DocumentsBadgeCountNotifier, int>(DocumentsBadgeCountNotifier.new);
