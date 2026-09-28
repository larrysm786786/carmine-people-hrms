import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/attendance_support/geofence_prompt_service.dart';
import '../../../core/attendance_support/wifi_checkin_service.dart';
import '../../../core/auth_session/auth_notifier.dart';
import '../../../core/notifications/badge_service.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/server_time.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/empty_state_view.dart';
import '../../../core/widgets/friendly_error.dart';
import '../../../core/widgets/premium_card.dart';
import '../../../core/widgets/quick_action_tile.dart';
import '../../../core/widgets/section_header.dart';
import '../../../core/widgets/stat_pill.dart';
import '../../announcements/application/announcements_notifier.dart';
import '../../announcements/data/announcement.dart';
import '../../attendance/application/today_attendance_notifier.dart';
import '../../employees/application/celebrations_notifier.dart';
import '../../profile/application/profile_notifier.dart';
import '../application/home_summary_notifier.dart';
import '../data/home_summary.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  bool _geofenceCheckDone = false;
  String? _cachedPhotoPath;
  String? _cachedForUrl;

  Future<void> _ensurePhotoDownloaded(String remotePath) async {
    if (_cachedForUrl == remotePath) return;
    final localPath = await ref.read(profileNotifierProvider.notifier).downloadPhoto(remotePath);
    if (!mounted || localPath == null) return;
    setState(() {
      _cachedForUrl = remotePath;
      _cachedPhotoPath = localPath;
    });
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _maybePromptCheckIn());
  }

  Future<void> _maybePromptCheckIn() async {
    if (_geofenceCheckDone) return;
    _geofenceCheckDone = true;

    final locationServiceEnabled =
        await ref.read(wifiCheckinServiceProvider).isLocationServiceEnabled();
    if (!locationServiceEnabled) {
      if (!mounted) return;
      final shouldOpenSettings = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Location is turned off'),
          content: const Text(
            'Please enable location services to use attendance check-in and the office proximity reminder.',
          ),
          actions: [
            TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Not now')),
            FilledButton(
              onPressed: () => Navigator.of(context).pop(true),
              child: const Text('Open Settings'),
            ),
          ],
        ),
      );
      if (shouldOpenSettings ?? false) {
        await ref.read(wifiCheckinServiceProvider).openLocationSettings();
      }
      return;
    }

    // isCheckedIn already covers "don't ask again once checked in" — this
    // guard is what stops the "you're at the office" prompt from firing on
    // every single Home visit throughout the day once the employee has
    // already checked in, not just re-checking the same value redundantly.
    final today = ref.read(todayAttendanceNotifierProvider).valueOrNull;
    if (today?.isCheckedIn ?? false) return;

    final locationId = ref.read(authNotifierProvider).valueOrNull?.profile?.locationId;
    if (locationId == null) return;

    try {
      final geofenceService = ref.read(geofencePromptServiceProvider);
      final office = await geofenceService.fetchLocation(locationId);
      if (office == null) return;

      final position = await ref.read(wifiCheckinServiceProvider).currentPosition();
      final isNear = await geofenceService.isWithinGeofence(office, position);
      if (!isNear || !mounted) return;

      final shouldCheckIn = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('You\'re at the office'),
          content: const Text('Would you like to check in now?'),
          actions: [
            TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Not now')),
            FilledButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Check in')),
          ],
        ),
      );
      if (shouldCheckIn ?? false) {
        await ref.read(todayAttendanceNotifierProvider.notifier).checkIn();
      }
    } catch (_) {
      // Best-effort convenience prompt; any failure (permission denied, no
      // signal, location service off) must silently fall back to manual
      // check-in rather than surface an error on Home.
    }
  }

  @override
  Widget build(BuildContext context) {
    final summary = ref.watch(homeSummaryNotifierProvider);
    final profile = ref.watch(authNotifierProvider).valueOrNull?.profile;
    final today = ref.watch(todayAttendanceNotifierProvider);
    final isCheckedIn = today.valueOrNull?.isCheckedIn ?? false;

    final profileDetail = ref.watch(profileNotifierProvider).valueOrNull;
    if (profileDetail?.profileImageUrl != null) {
      _ensurePhotoDownloaded(profileDetail!.profileImageUrl!);
    }
    final hasPhoto = _cachedPhotoPath != null && _cachedForUrl == profileDetail?.profileImageUrl;

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _HomeHeader(
              name: profile?.firstName ?? 'there',
              photoPath: hasPhoto ? _cachedPhotoPath : null,
            ),
            Expanded(
              child: AsyncValueView<HomeSummary>(
                value: summary,
                onRetry: () => ref.invalidate(homeSummaryNotifierProvider),
                data: (data) => ListView(
                  padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
                  children: [
                    Builder(builder: (context) {
                      final hasShift = data.schedule.shiftName != null;
                      final fgColor = hasShift ? AppColors.white : AppColors.muted;
                      return PremiumCard(
                        color: hasShift ? AppColors.carmine : AppColors.white,
                        border: hasShift ? null : Border.all(color: AppColors.carmine, width: 1),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(
                                  data.schedule.shiftName ?? 'No shift assigned',
                                  style: TextStyle(
                                    color: fgColor,
                                    fontSize: 18,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                                  decoration: BoxDecoration(
                                    color: hasShift ? AppColors.white : AppColors.carmine,
                                    borderRadius: BorderRadius.circular(999),
                                  ),
                                  child: Text(
                                    data.schedule.isOnBreak ? 'ON BREAK' : 'ACTIVE',
                                    style: TextStyle(
                                      color: hasShift ? AppColors.carmine : AppColors.white,
                                      fontSize: 11,
                                      fontWeight: FontWeight.w800,
                                      letterSpacing: 0.4,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            if (data.schedule.startTime != null && data.schedule.endTime != null) ...[
                              const SizedBox(height: 6),
                              Text(
                                '${data.schedule.startTime} — ${data.schedule.endTime}',
                                style: TextStyle(color: fgColor, fontSize: 13),
                              ),
                            ],
                            const SizedBox(height: 20),
                            SizedBox(
                              width: double.infinity,
                              child: FilledButton(
                                style: FilledButton.styleFrom(
                                  backgroundColor: isCheckedIn
                                      ? const Color(0xFF1E8E3E)
                                      : (hasShift ? AppColors.white : AppColors.carmine),
                                  foregroundColor: isCheckedIn
                                      ? AppColors.white
                                      : (hasShift ? AppColors.carmine : AppColors.white),
                                  padding: const EdgeInsets.symmetric(vertical: 14),
                                ),
                                onPressed: today.isLoading
                                    ? null
                                    : () => isCheckedIn
                                        ? ref.read(todayAttendanceNotifierProvider.notifier).checkOut()
                                        : ref.read(todayAttendanceNotifierProvider.notifier).checkIn(),
                                child: today.isLoading
                                    ? const SizedBox(
                                        height: 18,
                                        width: 18,
                                        child: CircularProgressIndicator(strokeWidth: 2),
                                      )
                                    : Text(isCheckedIn ? 'Check out' : 'Check in'),
                              ),
                            ),
                            if (today.hasError) ...[
                              const SizedBox(height: 12),
                              Text(
                                friendlyErrorMessage(today.error!),
                                style: TextStyle(color: fgColor, fontSize: 13),
                              ),
                            ],
                            const SizedBox(height: 12),
                            Align(
                              alignment: Alignment.centerLeft,
                              child: TextButton(
                                style: TextButton.styleFrom(
                                  foregroundColor: fgColor,
                                  padding: EdgeInsets.zero,
                                  minimumSize: const Size(0, 0),
                                ),
                                onPressed: () => context.push(RoutePaths.attendance),
                                child: const Text('View attendance history'),
                              ),
                            ),
                          ],
                        ),
                      );
                    }),
                    const SizedBox(height: 20),
                    const SectionHeader(title: 'This Month'),
                    const SizedBox(height: 10),
                    PremiumCard(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        children: [
                          Expanded(child: StatPill(value: '${data.monthlySummary.present}', label: 'Present')),
                          const SizedBox(width: 10),
                          Expanded(child: StatPill(value: '${data.monthlySummary.absent}', label: 'Absent')),
                          const SizedBox(width: 10),
                          Expanded(child: StatPill(value: '${data.monthlySummary.leave}', label: 'Leave')),
                          const SizedBox(width: 10),
                          Expanded(child: StatPill(value: '${data.monthlySummary.late}', label: 'Late')),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                    const SectionHeader(title: 'Quick Actions'),
                    const SizedBox(height: 10),
                    GridView.count(
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      crossAxisCount: 4,
                      mainAxisSpacing: 12,
                      crossAxisSpacing: 12,
                      children: [
                        QuickActionTile(
                          icon: Icons.calendar_today_outlined,
                          label: 'Attendance',
                          onTap: () => context.push(RoutePaths.attendance),
                        ),
                        QuickActionTile(
                          icon: Icons.insights_outlined,
                          label: 'My Analytics',
                          onTap: () => context.push(RoutePaths.attendanceAnalytics),
                        ),
                        QuickActionTile(
                          icon: Icons.schedule_outlined,
                          label: 'Overtime',
                          onTap: () => context.push(RoutePaths.overtime),
                        ),
                        QuickActionTile(
                          icon: Icons.swap_horiz_outlined,
                          label: 'Shift Swap',
                          onTap: () => context.push(RoutePaths.shiftSwap),
                        ),
                        QuickActionTile(
                          icon: Icons.beach_access_outlined,
                          label: 'Leave',
                          onTap: () => context.push(RoutePaths.leave),
                        ),
                        QuickActionTile(
                          icon: Icons.campaign_outlined,
                          label: 'Announcements',
                          onTap: () => context.push(RoutePaths.announcements),
                        ),
                        QuickActionTile(
                          icon: Icons.event_outlined,
                          label: 'Holidays',
                          onTap: () => context.push(RoutePaths.holidays),
                        ),
                        QuickActionTile(
                          icon: Icons.folder_outlined,
                          label: 'Documents',
                          badgeCount: ref.watch(documentsBadgeCountProvider),
                          onTap: () {
                            ref.read(documentsBadgeCountProvider.notifier).clear();
                            context.push(RoutePaths.documents);
                          },
                        ),
                        QuickActionTile(
                          icon: Icons.smart_toy_outlined,
                          label: 'AI Assistant',
                          onTap: () => context.push(RoutePaths.aiAssistant),
                        ),
                        QuickActionTile(
                          icon: Icons.account_balance_wallet_outlined,
                          label: 'Payslips',
                          onTap: () => context.push(RoutePaths.payroll),
                        ),
                        QuickActionTile(
                          icon: Icons.account_tree_outlined,
                          label: 'Org Chart',
                          onTap: () => context.push(RoutePaths.orgChart),
                        ),
                        QuickActionTile(
                          icon: Icons.settings_outlined,
                          label: 'Settings',
                          onTap: () => context.push(RoutePaths.settings),
                        ),
                        QuickActionTile(
                          icon: Icons.person_outline,
                          label: 'Profile',
                          onTap: () => context.push(RoutePaths.profile),
                        ),
                      ],
                    ),
                    const SizedBox(height: 24),
                    Consumer(
                      builder: (context, ref, _) {
                        final celebrations = ref.watch(celebrationsNotifierProvider);
                        return celebrations.maybeWhen(
                          data: (list) {
                            if (list.isEmpty) return const SizedBox.shrink();
                            return Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const SectionHeader(title: 'Celebrations'),
                                const SizedBox(height: 10),
                                for (final c in list.take(5))
                                  Padding(
                                    padding: const EdgeInsets.only(bottom: 10),
                                    child: PremiumCard(
                                      padding: const EdgeInsets.all(14),
                                      child: Row(
                                        children: [
                                          Icon(
                                            c.isBirthday ? Icons.cake_outlined : Icons.emoji_events_outlined,
                                            color: AppColors.carmine,
                                          ),
                                          const SizedBox(width: 12),
                                          Expanded(
                                            child: Text(
                                              c.isBirthday
                                                  ? '${c.employeeName}\'s birthday'
                                                  : '${c.employeeName} — ${c.yearsCount} year work anniversary',
                                              style: const TextStyle(
                                                color: AppColors.muted,
                                                fontWeight: FontWeight.w600,
                                              ),
                                            ),
                                          ),
                                          Text(
                                            c.date.split('T').first,
                                            style: const TextStyle(color: AppColors.muted, fontSize: 11),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                const SizedBox(height: 14),
                              ],
                            );
                          },
                          orElse: () => const SizedBox.shrink(),
                        );
                      },
                    ),
                    const SectionHeader(title: 'Announcements'),
                    const SizedBox(height: 10),
                    Consumer(
                      builder: (context, ref, _) {
                        final announcements = ref.watch(announcementsNotifierProvider);
                        return AsyncValueView<List<Announcement>>(
                          value: announcements,
                          onRetry: () => ref.invalidate(announcementsNotifierProvider),
                          data: (list) {
                            if (list.isEmpty) {
                              return const PremiumCard(
                                child: EmptyStateView(
                                  message: 'No announcements yet.',
                                  icon: Icons.campaign_outlined,
                                ),
                              );
                            }
                            return Column(
                              children: [
                                for (final a in list.take(3))
                                  Padding(
                                    padding: const EdgeInsets.only(bottom: 10),
                                    child: PremiumCard(
                                      padding: const EdgeInsets.all(16),
                                      child: Row(
                                        children: [
                                          Container(
                                            width: 40,
                                            height: 40,
                                            decoration: const BoxDecoration(
                                              color: AppColors.gray,
                                              shape: BoxShape.circle,
                                            ),
                                            child: const Icon(
                                              Icons.campaign_outlined,
                                              color: AppColors.carmine,
                                              size: 20,
                                            ),
                                          ),
                                          const SizedBox(width: 12),
                                          Expanded(
                                            child: Column(
                                              crossAxisAlignment: CrossAxisAlignment.start,
                                              children: [
                                                Text(
                                                  a.title,
                                                  style: const TextStyle(
                                                    color: AppColors.muted,
                                                    fontWeight: FontWeight.w700,
                                                  ),
                                                ),
                                                const SizedBox(height: 2),
                                                Text(
                                                  formatServerDateTime(a.publishAt),
                                                  style: const TextStyle(color: AppColors.muted, fontSize: 12),
                                                ),
                                              ],
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                              ],
                            );
                          },
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HomeHeader extends StatelessWidget {
  const _HomeHeader({required this.name, this.photoPath});

  final String name;
  final String? photoPath;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: AppColors.carmine,
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Welcome back',
                style: TextStyle(color: AppColors.white, fontSize: 13),
              ),
              Text(
                name,
                style: const TextStyle(
                  color: AppColors.white,
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
          CircleAvatar(
            radius: 22,
            backgroundColor: AppColors.white,
            backgroundImage: photoPath != null ? FileImage(File(photoPath!)) : null,
            child: photoPath == null ? const Icon(Icons.person_outline, color: AppColors.carmine) : null,
          ),
        ],
      ),
    );
  }
}
