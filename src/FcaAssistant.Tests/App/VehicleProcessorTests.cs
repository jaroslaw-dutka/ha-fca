using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.App;

public class VehicleProcessorTests
{
    private readonly FakeFcaClient _fca = new();
    private readonly FakeHaApiClient _ha = new();

    private VehicleProcessor CreateProcessor(params IVehicleHandler[] handlers) => new(
        NullLogger<VehicleProcessor>.Instance,
        Options.Create(new AppSettings()),
        _fca,
        _ha,
        handlers);

    [Fact]
    public async Task RunsEveryHandlerForEachVehicle()
    {
        _fca.Vehicles.AddRange([TestData.Vehicle(vin: "VIN1"), TestData.Vehicle(vin: "VIN2")]);
        var first = new RecordingVehicleHandler();
        var second = new RecordingVehicleHandler();

        await CreateProcessor(first, second).ProcessAsync(CancellationToken.None);

        Assert.Equal(["VIN1", "VIN2"], first.Handled.Select(c => c.Vehicle.Vehicle.Vin));
        Assert.Equal(["VIN1", "VIN2"], second.Handled.Select(c => c.Vehicle.Vehicle.Vin));
    }

    [Fact]
    public async Task PassesFetchedStatesIntoEachContext()
    {
        _fca.Vehicles.Add(TestData.Vehicle());
        _ha.States.Add(TestData.State("sensor.foo", "bar"));
        var handler = new RecordingVehicleHandler();

        await CreateProcessor(handler).ProcessAsync(CancellationToken.None);

        var context = Assert.Single(handler.Handled);
        Assert.Same(_ha.States, context.States);
    }

    [Fact]
    public async Task WithNoVehicles_RunsNoHandlers()
    {
        var handler = new RecordingVehicleHandler();

        await CreateProcessor(handler).ProcessAsync(CancellationToken.None);

        Assert.Empty(handler.Handled);
    }

    [Fact]
    public async Task RunsHandlersInRegistrationOrderPerVehicle()
    {
        _fca.Vehicles.Add(TestData.Vehicle());
        var order = new List<string>();
        var first = new RecordingVehicleHandler(_ => order.Add("first"));
        var second = new RecordingVehicleHandler(_ => order.Add("second"));

        await CreateProcessor(first, second).ProcessAsync(CancellationToken.None);

        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public async Task PropagatesHandlerException()
    {
        _fca.Vehicles.Add(TestData.Vehicle());
        var handler = new RecordingVehicleHandler(_ => throw new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateProcessor(handler).ProcessAsync(CancellationToken.None));
    }
}
