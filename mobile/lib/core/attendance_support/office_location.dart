class OfficeLocation {
  const OfficeLocation({
    required this.id,
    required this.latitude,
    required this.longitude,
    required this.geofenceRadiusMeters,
    this.officeWifiSsid,
  });

  factory OfficeLocation.fromJson(Map<String, dynamic> json) {
    return OfficeLocation(
      id: json['id'] as String,
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      geofenceRadiusMeters: json['geofenceRadiusMeters'] as int? ?? 100,
      officeWifiSsid: json['officeWifiSsid'] as String?,
    );
  }

  final String id;
  final double latitude;
  final double longitude;
  final int geofenceRadiusMeters;
  final String? officeWifiSsid;
}
