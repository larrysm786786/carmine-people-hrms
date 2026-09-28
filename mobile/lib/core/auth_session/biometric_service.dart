import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:local_auth/local_auth.dart';

class BiometricService {
  BiometricService([LocalAuthentication? auth]) : _auth = auth ?? LocalAuthentication();

  final LocalAuthentication _auth;

  Future<bool> isAvailable() async {
    try {
      final canCheck = await _auth.canCheckBiometrics;
      final isSupported = await _auth.isDeviceSupported();
      return canCheck && isSupported;
    } catch (_) {
      return false;
    }
  }

  Future<bool> authenticate() async {
    try {
      // biometricOnly: false lets the OS fall back to the device's own PIN/
      // pattern/password when biometrics aren't available or fail — this
      // covers every fingerprint sensor type (side-button, in-display, or
      // rear) since that detection is entirely handled by the OS, not this
      // package; biometricOnly would otherwise strand a user whose sensor
      // is temporarily unreadable with no way back into the app.
      return await _auth.authenticate(
        localizedReason: 'Unlock Carmine People',
        options: const AuthenticationOptions(biometricOnly: false),
      );
    } catch (_) {
      return false;
    }
  }
}

final biometricServiceProvider = Provider<BiometricService>((ref) => BiometricService());
