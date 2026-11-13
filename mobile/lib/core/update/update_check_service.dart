import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:package_info_plus/package_info_plus.dart';

/// Result of comparing the running app's build number against the latest GitHub Release.
class AppUpdateInfo {
  const AppUpdateInfo({required this.updateAvailable, required this.currentVersion, this.latestVersion, this.downloadUrl});

  final bool updateAvailable;
  final String currentVersion;
  final String? latestVersion;
  final String? downloadUrl;
}

// There's no Play Store for this app (sideloaded APK), so "check for update" means asking
// GitHub directly: .github/workflows/mobile-ci.yml publishes a public Release (tag
// "app-v<pubspec version>") with the built APK attached on every push to main. The releases/
// latest endpoint needs no auth for a public repo, so this works with a plain, unauthenticated
// request — no backend involvement at all.
class UpdateCheckService {
  static const _repo = 'larrysm786786/carmine-people-hrms';
  final _dio = Dio();

  Future<AppUpdateInfo> checkForUpdate() async {
    final packageInfo = await PackageInfo.fromPlatform();
    final currentBuild = int.tryParse(packageInfo.buildNumber) ?? 0;
    final currentVersion = '${packageInfo.version}+${packageInfo.buildNumber}';

    final response = await _dio.get<Map<String, dynamic>>(
      'https://api.github.com/repos/$_repo/releases/latest',
      options: Options(headers: {'Accept': 'application/vnd.github+json'}),
    );
    final data = response.data ?? {};
    final tagName = data['tag_name'] as String? ?? '';

    final buildMatch = RegExp(r'\+(\d+)$').firstMatch(tagName);
    final latestBuild = buildMatch != null ? int.tryParse(buildMatch.group(1)!) ?? 0 : 0;

    final assets = (data['assets'] as List<dynamic>?)?.cast<Map<String, dynamic>>() ?? [];
    Map<String, dynamic>? apkAsset;
    for (final asset in assets) {
      if ((asset['name'] as String? ?? '').endsWith('.apk')) {
        apkAsset = asset;
        break;
      }
    }
    final downloadUrl = apkAsset?['browser_download_url'] as String?;

    return AppUpdateInfo(
      updateAvailable: latestBuild > currentBuild && downloadUrl != null,
      currentVersion: currentVersion,
      latestVersion: tagName.isEmpty ? null : tagName.replaceFirst('app-v', ''),
      downloadUrl: downloadUrl,
    );
  }
}

final updateCheckServiceProvider = Provider<UpdateCheckService>((ref) => UpdateCheckService());
