import 'employee_profile.dart';

enum AuthStatus { authenticated, unauthenticated, locked }

/// The one shape that represents "where the app is with respect to auth."
/// There is exactly one place that ever constructs the authenticated variant
/// of this: session_bootstrapper.dart's _buildAuthenticatedState.
class AuthData {
  const AuthData._({required this.status, this.profile});

  const AuthData.unauthenticated() : this._(status: AuthStatus.unauthenticated);

  const AuthData.authenticated(EmployeeProfile profile)
      : this._(status: AuthStatus.authenticated, profile: profile);

  const AuthData.locked(EmployeeProfile profile)
      : this._(status: AuthStatus.locked, profile: profile);

  final AuthStatus status;
  final EmployeeProfile? profile;

  bool get isAuthenticated => status == AuthStatus.authenticated;
  bool get isLocked => status == AuthStatus.locked;

  AuthData copyWithLocked(bool locked) {
    if (profile == null) return this;
    return locked ? AuthData.locked(profile!) : AuthData.authenticated(profile!);
  }
}
