import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth_session/auth_notifier.dart';
import '../../core/auth_session/biometric_service.dart';

class BiometricLockScreen extends ConsumerStatefulWidget {
  const BiometricLockScreen({super.key});

  @override
  ConsumerState<BiometricLockScreen> createState() => _BiometricLockScreenState();
}

class _BiometricLockScreenState extends ConsumerState<BiometricLockScreen> {
  bool _authenticating = false;

  Future<void> _unlock() async {
    setState(() => _authenticating = true);
    final success = await ref.read(biometricServiceProvider).authenticate();
    if (!mounted) return;
    setState(() => _authenticating = false);
    if (success) {
      ref.read(authNotifierProvider.notifier).unlock();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.lock_outline, size: 48),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: _authenticating ? null : _unlock,
              icon: const Icon(Icons.fingerprint),
              label: const Text('Unlock'),
            ),
          ],
        ),
      ),
    );
  }
}
