import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/auth_session/auth_notifier.dart';
import '../../../core/router/route_paths.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/async_value_view.dart';
import '../../../core/widgets/premium_card.dart';
import '../application/profile_notifier.dart';
import '../data/employee_detail.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  bool _uploadingPhoto = false;
  String? _cachedPhotoPath;
  String? _cachedForUrl;

  Future<void> _pickAndUploadPhoto() async {
    final file = await FilePicker.pickFile(type: FileType.image);
    if (file?.path == null) return;
    setState(() => _uploadingPhoto = true);
    final success = await ref.read(profileNotifierProvider.notifier).updatePhoto(file!.path!);
    if (!mounted) return;
    setState(() {
      _uploadingPhoto = false;
      _cachedPhotoPath = null;
      _cachedForUrl = null;
    });
    if (!success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Photo upload failed. Please try again.')),
      );
    }
  }

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
  Widget build(BuildContext context) {
    final profile = ref.watch(profileNotifierProvider);
    final canViewEmployees =
        ref.watch(authNotifierProvider).valueOrNull?.profile?.hasPermission('employee.view') ?? false;

    return Scaffold(
      body: SafeArea(
        child: AsyncValueView<EmployeeDetail>(
          value: profile,
          onRetry: () => ref.invalidate(profileNotifierProvider),
          data: (detail) {
            if (detail.profileImageUrl != null) {
              _ensurePhotoDownloaded(detail.profileImageUrl!);
            }
            final hasPhoto = _cachedPhotoPath != null && _cachedForUrl == detail.profileImageUrl;
            return ListView(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
            children: [
              PremiumCard(
                color: AppColors.carmine,
                child: Column(
                  children: [
                    Stack(
                      clipBehavior: Clip.none,
                      children: [
                        CircleAvatar(
                          radius: 38,
                          backgroundColor: AppColors.white,
                          backgroundImage: hasPhoto ? FileImage(File(_cachedPhotoPath!)) : null,
                          child: !hasPhoto
                              ? const Icon(Icons.person, size: 38, color: AppColors.carmine)
                              : null,
                        ),
                        Positioned(
                          right: -4,
                          bottom: -4,
                          child: Material(
                            color: AppColors.muted,
                            shape: const CircleBorder(),
                            child: InkWell(
                              customBorder: const CircleBorder(),
                              onTap: _uploadingPhoto ? null : _pickAndUploadPhoto,
                              child: Padding(
                                padding: const EdgeInsets.all(6),
                                child: _uploadingPhoto
                                    ? const SizedBox(
                                        width: 16,
                                        height: 16,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: AppColors.white,
                                        ),
                                      )
                                    : const Icon(Icons.edit, size: 16, color: AppColors.white),
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Text(
                      detail.fullName,
                      style: const TextStyle(
                        color: AppColors.white,
                        fontSize: 20,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      detail.designationTitle,
                      style: const TextStyle(color: AppColors.white, fontSize: 13),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),
              PremiumCard(
                padding: EdgeInsets.zero,
                child: Column(
                  children: [
                    _ProfileRow(icon: Icons.badge_outlined, label: detail.employeeCode),
                    const _RowDivider(),
                    _ProfileRow(icon: Icons.email_outlined, label: detail.email),
                    if (detail.phoneNumber != null) ...[
                      const _RowDivider(),
                      _ProfileRow(icon: Icons.phone_outlined, label: detail.phoneNumber!),
                    ],
                    const _RowDivider(),
                    _ProfileRow(icon: Icons.apartment_outlined, label: detail.departmentName),
                    const _RowDivider(),
                    _ProfileRow(icon: Icons.place_outlined, label: detail.locationName),
                    if (detail.managerName != null) ...[
                      const _RowDivider(),
                      _ProfileRow(
                        icon: Icons.supervisor_account_outlined,
                        label: detail.managerName!,
                      ),
                    ],
                  ],
                ),
              ),
              const SizedBox(height: 20),
              PremiumCard(
                padding: EdgeInsets.zero,
                child: Column(
                  children: [
                    _ActionRow(
                      icon: Icons.folder_outlined,
                      label: 'My Documents',
                      onTap: () => context.push(RoutePaths.documents),
                    ),
                    const _RowDivider(),
                    _ActionRow(
                      icon: Icons.notifications_outlined,
                      label: 'Notification Preferences',
                      onTap: () => context.push(RoutePaths.notificationPrefs),
                    ),
                    if (canViewEmployees) ...[
                      const _RowDivider(),
                      _ActionRow(
                        icon: Icons.groups_outlined,
                        label: 'Employee Directory',
                        onTap: () => context.push(RoutePaths.employees),
                      ),
                    ],
                  ],
                ),
              ),
              const SizedBox(height: 20),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: () => ref.read(authNotifierProvider.notifier).logout(),
                  icon: const Icon(Icons.logout),
                  label: const Text('Log out'),
                ),
              ),
            ],
            );
          },
        ),
      ),
    );
  }
}

class _ProfileRow extends StatelessWidget {
  const _ProfileRow({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(
        children: [
          Icon(icon, color: AppColors.carmine, size: 20),
          const SizedBox(width: 14),
          Expanded(child: Text(label, style: const TextStyle(color: AppColors.muted))),
        ],
      ),
    );
  }
}

class _ActionRow extends StatelessWidget {
  const _ActionRow({required this.icon, required this.label, required this.onTap});

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Row(
          children: [
            Icon(icon, color: AppColors.carmine, size: 20),
            const SizedBox(width: 14),
            Expanded(
              child: Text(
                label,
                style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
              ),
            ),
            const Icon(Icons.chevron_right, color: AppColors.muted, size: 20),
          ],
        ),
      ),
    );
  }
}

class _RowDivider extends StatelessWidget {
  const _RowDivider();

  @override
  Widget build(BuildContext context) {
    return const Divider(height: 1, indent: 16, endIndent: 16, color: AppColors.gray);
  }
}
