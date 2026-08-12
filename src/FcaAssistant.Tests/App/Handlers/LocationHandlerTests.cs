using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.App.Handlers;

public class LocationHandlerTests
{
    private readonly FakeHaEntityPublisher _publisher = new();
    private readonly LocationHandler _handler;

    public LocationHandlerTests() =>
        _handler = new LocationHandler(_publisher, Options.Create(new AppSettings { CarUnknownLocation = "away" }));

    private static VehicleLocation At(double lat, double lon) => new() { Latitude = lat, Longitude = lon };

    private async Task<HaSensor<HaLocation>> PublishAsync(VehicleLocation location, params HaRestApiEntityState[] states)
    {
        var context = TestData.Context(TestData.Vehicle(location: location), states);
        await _handler.HandleAsync(context, CancellationToken.None);
        return Assert.IsType<HaSensor<HaLocation>>(Assert.Single(_publisher.Published));
    }

    [Fact]
    public async Task InsideZone_StateIsZoneFriendlyName()
    {
        var sensor = await PublishAsync(
            At(50.0, 19.0),
            TestData.Zone("zone.home", 50.0, 19.0, radius: 100, "Home"));

        Assert.Equal("Home", sensor.State);
    }

    [Fact]
    public async Task OutsideAllZones_StateIsUnknownLocation()
    {
        var sensor = await PublishAsync(
            At(50.0, 19.0),
            TestData.Zone("zone.home", 0.0, 0.0, radius: 100, "Home"));

        Assert.Equal("away", sensor.State);
    }

    [Fact]
    public async Task MultipleContainingZones_NearestIsChosen()
    {
        var sensor = await PublishAsync(
            At(50.01, 19.0),
            TestData.Zone("zone.home", 50.0, 19.0, radius: 100_000, "Home"),
            TestData.Zone("zone.work", 50.01, 19.0, radius: 100_000, "Work"));

        Assert.Equal("Work", sensor.State);
    }

    [Fact]
    public async Task PublishesGpsAttributesFromVehicleLocation()
    {
        var sensor = await PublishAsync(At(50.5, 19.25));

        Assert.Equal(50.5, sensor.Attributes.Latitude);
        Assert.Equal(19.25, sensor.Attributes.Longitude);
        Assert.Equal("gps", sensor.Attributes.SourceType);
        Assert.Equal(2, sensor.Attributes.GpsAccuracy);
    }
}
