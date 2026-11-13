import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/auth_session/biometric_service.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_config.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/storage/token_storage.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/update/update_check_service.dart';

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
  bool _checkingUpdate = false;
  AppUpdateInfo? _updateInfo;
  String? _updateCheckError;

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

  Future<void> _checkForUpdate() async {
    setState(() {
      _checkingUpdate = true;
      _updateCheckError = null;
    });
    try {
      final info = await ref.read(updateCheckServiceProvider).checkForUpdate();
      if (mounted) setState(() => _updateInfo = info);
    } catch (_) {
      if (mounted) _updateCheckError = 'Unable to check for updates right now.';
    } finally {
      if (mounted) setState(() => _checkingUpdate = false);
    }
  }

  Future<void> _downloadUpdate(String url) async {
    final uri = Uri.parse(url);
    await launchUrl(uri, mode: LaunchMode.externalApplication);
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
            const Divider(height: 32),
            Text('App Update', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            if (_updateInfo != null) ...[
              Text('Current version: ${_updateInfo!.currentVersion}', style: TextStyle(color: AppColors.muted)),
              const SizedBox(height: 4),
              Text(
                _updateInfo!.updateAvailable ? 'New version available: ${_updateInfo!.latestVersion}' : "You're up to date.",
                style: TextStyle(
                  color: _updateInfo!.updateAvailable ? AppColors.carmine : AppColors.muted,
                  fontWeight: _updateInfo!.updateAvailable ? FontWeight.bold : FontWeight.normal,
                ),
              ),
              const SizedBox(height: 12),
            ],
            if (_updateCheckError != null) ...[
              Text(_updateCheckError!, style: TextStyle(color: AppColors.carmine)),
              const SizedBox(height: 12),
            ],
            if (_updateInfo?.updateAvailable == true)
              FilledButton.icon(
                onPressed: () => _downloadUpdate(_updateInfo!.downloadUrl!),
                icon: const Icon(Icons.download),
                label: const Text('Download Update'),
              )
            else
              OutlinedButton(
                onPressed: _checkingUpdate ? null : _checkForUpdate,
                child: _checkingUpdate
                    ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Check for Update'),
              ),
          ],
        ),
      ),
    );
  }
}
