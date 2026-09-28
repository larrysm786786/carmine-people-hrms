using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class Location : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int GeofenceRadiusMeters { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    // Optional — powers WiFi-based auto check-in prompts on mobile (connecting to this SSID
    // suggests the employee just arrived on-site). Never trusted as sole proof of presence;
    // CheckInAsync still enforces the GPS geofence above regardless of how the check-in was
    // triggered, since SSIDs are trivially spoofable/duplicated across locations.
    public string? OfficeWifiSsid { get; set; }
}
