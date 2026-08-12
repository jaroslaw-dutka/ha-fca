using System.Text.Json.Nodes;
using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.App.Mapping;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.App.Handlers;

public class SensorsHandlerTests
{
    private readonly FakeHaEntityPublisher _publisher = new();

    private SensorsHandler Handler(DistanceUnit unit = DistanceUnit.Kilometers) =>
        new(_publisher, new VehicleDetailsMapper(), Options.Create(new AppSettings { DistanceUnit = unit }));

    private HaSensor Published(string name) => _publisher.Published.OfType<HaSensor>().Single(s => s.Name == name);

    private async Task HandleAsync(JsonNode details, JsonNode? remote = null)
    {
        var context = TestData.Context(TestData.Vehicle(details: details, remote: remote ?? JsonNode.Parse("{}")!));
        await Handler().HandleAsync(context, CancellationToken.None);
    }

    [Fact]
    public async Task MapsBatterySensorFromDetails()
    {
        await HandleAsync(JsonNode.Parse("""{ "evInfo": { "battery": { "stateOfCharge": 80 } } }""")!);

        var sensor = Published("car_evInfo_battery_stateOfCharge");
        Assert.Equal("80", sensor.State);
        Assert.Equal("battery", sensor.DeviceClass);
        Assert.Equal("%", sensor.UnitOfMeasurement);
    }

    [Fact]
    public async Task MapsDistanceValue_AndSkipsCompanionUnitKey()
    {
        await HandleAsync(JsonNode.Parse("""{ "odometer": { "value": 100, "unit": "km" } }""")!);

        var sensor = Published("car_odometer_value");
        Assert.Equal("distance", sensor.DeviceClass);
        Assert.Equal("km", sensor.UnitOfMeasurement);

        Assert.DoesNotContain(_publisher.Published.OfType<HaSensor>(), s => s.Name == "car_odometer_unit");
    }

    [Fact]
    public async Task PublishesRemoteSensorsWithCarRemotePrefix()
    {
        await HandleAsync(JsonNode.Parse("{}")!, JsonNode.Parse("""{ "trunk": { "status": "LOCKED" } }""")!);

        var sensor = Published("car_remote_trunk_status");
        Assert.Equal("LOCKED", sensor.State);
    }
}
