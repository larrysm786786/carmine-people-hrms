import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:carmine_people/core/auth_session/auth_repository.dart';
import 'package:carmine_people/core/auth_session/employee_profile.dart';
import 'package:carmine_people/core/auth_session/session_bootstrapper.dart';
import 'package:carmine_people/core/storage/profile_cache_store.dart';

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository({this.restoreResult});

  LoginResult? restoreResult;
  bool loginCalled = false;
  bool restoreCalled = false;

  static final _profile = EmployeeProfile(
    id: '1',
    employeeCode: 'E1',
    firstName: 'Test',
    lastName: 'User',
    email: 't@example.com',
    roleName: 'Employee',
    departmentId: 'd1',
    designationId: 'des1',
    locationId: 'l1',
    permissions: const [],
  );

  @override
  Future<LoginResult> login(String email, String password) async {
    loginCalled = true;
    return LoginResult(_profile);
  }

  @override
  Future<LoginResult?> restore() async {
    restoreCalled = true;
    return restoreResult;
  }

  @override
  Future<void> logout() async {}

  @override
  Future<void> forgotPassword(String email) async {}
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  setUp(() => SharedPreferences.setMockInitialValues({}));

  group('SessionBootstrapper.resolveSession', () {
    test('login path produces authenticated state with populated profile', () async {
      final repo = _FakeAuthRepository();
      final bootstrapper = SessionBootstrapper(repo, ProfileCacheStore());

      final result = await bootstrapper.resolveSession(email: 'a@b.com', password: 'pw');

      expect(repo.loginCalled, isTrue);
      expect(result.isAuthenticated, isTrue);
      expect(result.profile, isNotNull);
      expect(result.profile!.email, 't@example.com');
    });

    test('restore path (no credentials) produces the same authenticated shape', () async {
      final repo = _FakeAuthRepository(
        restoreResult: LoginResult(_FakeAuthRepository._profile),
      );
      final bootstrapper = SessionBootstrapper(repo, ProfileCacheStore());

      final result = await bootstrapper.resolveSession();

      expect(repo.restoreCalled, isTrue);
      expect(result.isAuthenticated, isTrue);
      expect(result.profile, isNotNull);
      expect(result.profile!.email, 't@example.com');
    });

    test('restore path with no session returns unauthenticated, not an error', () async {
      final repo = _FakeAuthRepository(restoreResult: null);
      final bootstrapper = SessionBootstrapper(repo, ProfileCacheStore());

      final result = await bootstrapper.resolveSession();

      expect(result.isAuthenticated, isFalse);
      expect(result.profile, isNull);
    });
  });
}
