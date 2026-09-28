using CarminePeople.Application.Common.Geo;
using Xunit;

namespace CarminePeople.UnitTests.Geo;

public class GeoDistanceCalculatorTests
{
    [Fact]
    public void DistanceMeters_SamePoint_ReturnsZero()
    {
        var distance = GeoDistanceCalculator.DistanceMeters(12.9716, 77.5946, 12.9716, 77.5946);
        Assert.Equal(0, distance, precision: 3);
    }

    [Fact]
    public void DistanceMeters_KnownShortDistance_IsWithinExpectedRange()
    {
        // ~0.001 degree latitude difference is roughly 111 meters
        var distance = GeoDistanceCalculator.DistanceMeters(12.9716, 77.5946, 12.9726, 77.5946);
        Assert.InRange(distance, 100, 120);
    }

    [Fact]
    public void DistanceMeters_FarApart_ExceedsTypicalGeofenceRadius()
    {
        // Roughly 1km away
        var distance = GeoDistanceCalculator.DistanceMeters(12.9716, 77.5946, 12.9806, 77.5946);
        Assert.True(distance > 500);
    }

    [Theory]
    [InlineData(12.9716, 77.5946, 12.97161, 77.59461, 100)] // ~1.5m apart, well within 100m geofence
    public void DistanceMeters_WithinGeofenceRadius_IsBelowRadius(double lat1, double lng1, double lat2, double lng2, int radiusMeters)
    {
        var distance = GeoDistanceCalculator.DistanceMeters(lat1, lng1, lat2, lng2);
        Assert.True(distance <= radiusMeters);
    }
}
