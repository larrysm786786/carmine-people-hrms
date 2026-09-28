import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth_session/biometric_service.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_config.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/storage/token_storage.dart';
import '../../../core/theme/app_colors.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  final _controller = TextEditingController();
  bool _testing = false;
  String? _testResult;
  bool _testFailed = false;
  bool _biometricEnabled = false;
  bool _biometricAvailable = false;

  @override
  void initState() {
    super.initState();
    _loadCurrent();
    _loadBiometricState();
  }

  Future<void> _loadCurrent() async {
    final current = await ref.read(apiConfigProvider).resolveBaseUrl();
    if (mounted) _controller.text = current;
  }

  Future<void> _loadBiometricState() async {
    final available = await ref.read(biometricServiceProvider).isAvailable();
    final enabled = await ref.read(tokenStorageProvider).readBiometricEnabled();
    if (!mounted) return;
    setState(() {
      _biometricAvailable = available;
      _biometricEnabled = enabled;
    });
  }

  Future<void> _toggleBiometric(bool value) async {
    if (value) {
      final success = await ref.read(biometricServiceProvider).authenticate();
      if (!success) return;
    }
    await ref.read(tokenStorageProvider).setBiometricEnabled(value);
    if (mounted) setState(() => _biometricEnabled = value);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _testConnection() async {
    setState(() {
      _testing = true;
      _testResult = null;
    });
    try {
      await ref.read(apiConfigProvider).setOverride(_controller.text.trim());
      await ref.read(apiClientProvider).get('/branding');
      setState(() {
        _testFailed = false;
        _testResult = 'Connection successful.';
      });
    } catch (e) {
      setState(() {
        _testFailed = true;
        _testResult = e is ApiException ? e.message : 'Connection failed.';
      });
    } finally {
      if (mounted) setState(() => _testing = false);
    }
  }

  Future<void> _saveAndRestart() async {
    await ref.read(apiConfigProvider).setOverride(_controller.text.trim());
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Saved. Please restart the app for changes to take effect.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_biometricAvailable) ...[
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                activeTrackColor: AppColors.carmine,
                title: const Text('Biometric App Lock'),
                subtitle: const Text('Require fingerprint/face unlock when reopening the app'),
                value: _biometricEnabled,
                onChanged: _toggleBiometric,
              ),
              const Divider(height: 32),
            ],
            Text('API Base URL', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            TextField(
              controller: _controller,
              decoration: const InputDecoration(
                hintText: 'http://192.168.1.10:5083/api/v1',
              ),
            ),
            if (_testResult != null) ...[
              const SizedBox(height: 8),
              Text(
                _testResult!,
                style: TextStyle(
                  color: _testFailed ? AppColors.carmine : AppColors.muted,
                  fontWeight: _testFailed ? FontWeight.normal : FontWeight.bold,
                ),
              ),
            ],
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: _testing ? null : _testConnection,
                    child: _testing
                        ? const SizedBox(
                            height: 16,
                            width: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Test Connection'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton(
                    onPressed: _saveAndRestart,
                    child: const Text('Save'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
