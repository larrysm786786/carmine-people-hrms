import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/ai_assistant/presentation/ai_chat_screen.dart';
import '../../features/announcements/presentation/announcements_screen.dart';
import '../../features/attendance/presentation/attendance_analytics_screen.dart';
import '../../features/attendance/presentation/attendance_screen.dart';
import '../../features/auth/biometric_lock_screen.dart';
import '../../features/auth/forgot_password_screen.dart';
import '../../features/auth/login_screen.dart';
import '../../features/documents/presentation/documents_screen.dart';
import '../../features/employees/presentation/employees_list_screen.dart';
import '../../features/holidays/presentation/holidays_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../../features/leave/presentation/leave_approvals_screen.dart';
import '../../features/leave/presentation/leave_calendar_screen.dart';
import '../../features/leave/presentation/leave_screen.dart';
import '../../features/employees/presentation/org_chart_screen.dart';
import '../../features/overtime/presentation/overtime_approvals_screen.dart';
import '../../features/overtime/presentation/overtime_screen.dart';
import '../../features/payroll/presentation/payroll_screen.dart';
import '../../features/shift_swap/presentation/shift_swap_approvals_screen.dart';
import '../../features/shift_swap/presentation/shift_swap_screen.dart';
import '../../features/notifications/presentation/notification_prefs_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/onboarding/presentation/onboarding_screen.dart';
import '../../features/profile/presentation/profile_screen.dart';
import '../../features/settings/presentation/settings_screen.dart';
import '../../features/splash/presentation/splash_screen.dart';
import '../attendance_support/wifi_office_connection_monitor.dart';
import '../auth_session/auth_notifier.dart';
import '../notifications/fcm_service.dart';
import 'app_shell.dart';
import 'route_paths.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final refreshListenable = _AuthRefreshListenable(ref);
  ref.listen(authNotifierProvider, (previous, next) {
    if (next.valueOrNull?.isAuthenticated ?? false) {
      ref.read(fcmServiceProvider).initOnce();
      ref.read(wifiOfficeConnectionMonitorProvider).start();
    }
  });

  return GoRouter(
    initialLocation: RoutePaths.splash,
    refreshListenable: refreshListenable,
    redirect: (context, state) {
      final authState = ref.read(authNotifierProvider);
      final path = state.matchedLocation;

      // Only the very first session resolution (no prior AuthData at all —
      // splash/cold-start) should redirect to splash while loading. A
      // subsequent AsyncLoading from login()/logout() must NOT bounce the
      // user off whatever screen they're on (e.g. away from the login form
      // mid-submit, which would silently swallow the error message once the
      // request settles and LoginScreen has already been disposed).
      if (authState.isLoading && !authState.hasValue && !authState.hasError) {
        return path == RoutePaths.splash ? null : RoutePaths.splash;
      }
      if (authState.isLoading) return null;

      final data = authState.valueOrNull;
      final isAuthenticated = data?.isAuthenticated ?? false;
      final isLocked = data?.isLocked ?? false;
      final isPublicAuthRoute = path == RoutePaths.login ||
          path == RoutePaths.forgotPassword ||
          path == RoutePaths.onboarding;

      if (isLocked) return path == RoutePaths.biometricLock ? null : RoutePaths.biometricLock;
      if (!isAuthenticated && !isPublicAuthRoute) return RoutePaths.login;
      if (isAuthenticated && (isPublicAuthRoute || path == RoutePaths.splash)) {
        return RoutePaths.home;
      }
      return null;
    },
    routes: [
      GoRoute(path: RoutePaths.splash, builder: (context, state) => const SplashScreen()),
      GoRoute(path: RoutePaths.login, builder: (context, state) => const LoginScreen()),
      GoRoute(
        path: RoutePaths.forgotPassword,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: RoutePaths.biometricLock,
        builder: (context, state) => const BiometricLockScreen(),
      ),
      GoRoute(
        path: RoutePaths.onboarding,
        builder: (context, state) => const OnboardingScreen(),
      ),
      GoRoute(path: RoutePaths.documents, builder: (context, state) => const DocumentsScreen()),
      GoRoute(
        path: RoutePaths.announcements,
        builder: (context, state) => const AnnouncementsScreen(),
      ),
      GoRoute(path: RoutePaths.holidays, builder: (context, state) => const HolidaysScreen()),
      GoRoute(path: RoutePaths.aiAssistant, builder: (context, state) => const AiChatScreen()),
      GoRoute(path: RoutePaths.settings, builder: (context, state) => const SettingsScreen()),
      GoRoute(path: RoutePaths.employees, builder: (context, state) => const EmployeesListScreen()),
      GoRoute(
        path: RoutePaths.notificationPrefs,
        builder: (context, state) => const NotificationPrefsScreen(),
      ),
      GoRoute(
        path: RoutePaths.leaveApprovals,
        builder: (context, state) => const LeaveApprovalsScreen(),
      ),
      GoRoute(
        path: RoutePaths.leaveCalendar,
        builder: (context, state) => const LeaveCalendarScreen(),
      ),
      GoRoute(
        path: RoutePaths.attendanceAnalytics,
        builder: (context, state) => const AttendanceAnalyticsScreen(),
      ),
      GoRoute(path: RoutePaths.overtime, builder: (context, state) => const OvertimeScreen()),
      GoRoute(
        path: RoutePaths.overtimeApprovals,
        builder: (context, state) => const OvertimeApprovalsScreen(),
      ),
      GoRoute(path: RoutePaths.shiftSwap, builder: (context, state) => const ShiftSwapScreen()),
      GoRoute(
        path: RoutePaths.shiftSwapApprovals,
        builder: (context, state) => const ShiftSwapApprovalsScreen(),
      ),
      GoRoute(path: RoutePaths.payroll, builder: (context, state) => const PayrollScreen()),
      GoRoute(path: RoutePaths.orgChart, builder: (context, state) => const OrgChartScreen()),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) => AppShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.home, builder: (context, state) => const HomeScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.attendance, builder: (context, state) => const AttendanceScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.leave, builder: (context, state) => const LeaveScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: RoutePaths.notifications,
              builder: (context, state) => const NotificationsScreen(),
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: RoutePaths.profile, builder: (context, state) => const ProfileScreen()),
          ]),
        ],
      ),
    ],
  );
});

/// Bridges Riverpod's AsyncValue-of-AuthData stream of changes into the
/// Listenable that GoRouter's redirect re-evaluation expects.
class _AuthRefreshListenable extends ChangeNotifier {
  _AuthRefreshListenable(Ref ref) {
    ref.listen(authNotifierProvider, (previous, next) => notifyListeners());
  }
}
