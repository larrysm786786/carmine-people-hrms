namespace CarminePeople.Application.Attendance.Services;

// A Location's OfficeWifiSsid field holds one or more allowed network names, comma-separated
// (e.g. "Airtel_Carmine,Airtel_Carmine 5G,Hatwy Carmine 5G") — most offices have a 2.4GHz and
// 5GHz band advertised as distinct SSIDs, or multiple routers, and any of them should count as
// "on the office network." Pure/static so it's trivially unit-testable without a DB.
public static class OfficeWifiMatcher
{
    public static bool IsAllowed(string? configuredSsids, string? connectedSsid)
    {
        if (string.IsNullOrWhiteSpace(configuredSsids)) return true; // no restriction configured
        if (string.IsNullOrWhiteSpace(connectedSsid)) return false;

        return configuredSsids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(allowed => string.Equals(allowed, connectedSsid, StringComparison.OrdinalIgnoreCase));
    }
}
