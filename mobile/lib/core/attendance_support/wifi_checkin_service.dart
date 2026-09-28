import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:network_info_plus/network_info_plus.dart';

class LocationUnavailableException implements Exception {
  const LocationUnavailableException(this.message);
  final String message;
}

class WifiCheckinService {
  final _networkInfo = NetworkInfo();

  Future<Position> currentPosition() async {
    final permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      final requested = await Geolocator.requestPermission();
      if (requested == LocationPermission.denied) {
        throw const LocationUnavailableException('Location permission denied.');
      }
    }
    if (permission == LocationPermission.deniedForever) {
      throw const LocationUnavailableException(
        'Location permission permanently denied. Enable it in settings.',
      );
    }
    final serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      throw const LocationUnavailableException('Location services are disabled.');
    }
    return Geolocator.getCurrentPosition();
  }

  /// Android sometimes wraps the SSID in double quotes (a quirk of the
  /// underlying WifiInfo.getSSID() API) — strip them so backend comparison
  /// against the configured office SSID is exact, not accidentally failing.
  Future<String?> currentWifiName() async {
    final raw = await _networkInfo.getWifiName();
    if (raw == null) return null;
    return raw.replaceAll('"', '');
  }

  Future<bool> isLocationServiceEnabled() => Geolocator.isLocationServiceEnabled();

  Future<void> openLocationSettings() => Geolocator.openLocationSettings();
}

final wifiCheckinServiceProvider = Provider<WifiCheckinService>((ref) {
  return WifiCheckinService();
});
