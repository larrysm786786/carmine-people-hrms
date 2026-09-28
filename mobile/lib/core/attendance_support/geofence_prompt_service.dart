import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

import '../network/api_client.dart';
import 'office_location.dart';

/// Checks whether the device is currently within the employee's office
/// geofence — used to prompt "check in now?" on app resume, never to
/// silently auto-check-in (a wrong/stale GPS fix producing a false
/// check-in would be worse than occasionally missing the prompt).
class GeofencePromptService {
  GeofencePromptService(this._client);

  final ApiClient _client;

  Future<OfficeLocation?> fetchLocation(String locationId) async {
    final data = await _client.getList('/locations');
    for (final raw in data) {
      final map = raw as Map<String, dynamic>;
      if (map['id'] == locationId) return OfficeLocation.fromJson(map);
    }
    return null;
  }

  Future<bool> isWithinGeofence(OfficeLocation office, Position position) async {
    final distance = Geolocator.distanceBetween(
      position.latitude,
      position.longitude,
      office.latitude,
      office.longitude,
    );
    return distance <= office.geofenceRadiusMeters;
  }
}

final geofencePromptServiceProvider = Provider<GeofencePromptService>((ref) {
  return GeofencePromptService(ref.watch(apiClientProvider));
});
