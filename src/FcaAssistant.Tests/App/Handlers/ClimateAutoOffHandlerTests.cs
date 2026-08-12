using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.App.Handlers;

public class ClimateAutoOffHandlerTests
{
    private readonly FakeHaEntityPublisher _publisher = new();
    private readonly ClimateAutoOffHandler _handler;

    public ClimateAutoOffHandlerTests() =>
        _handler = new ClimateAutoOffHandler(
            _publisher,
            Options.Create(new AppSettings { RefreshInterval = 15 }),
            NullLogger<ClimateAutoOffHandler>.Instance);

    // Registers a Climate switch (ON) with the publisher and returns it, mirroring what CommandEntitiesHandler does.
    private async Task<HaSwitch> RegisterClimateSwitchAsync(CarContext context)
    {
        var @switch = new HaSwitch(context.Device, "Climate", (_, _) => Task.CompletedTask);
        @switch.SetState(true);
        await _publisher.RegisterAsync(@switch);
        return @switch;
    }

    [Fact]
    public async Task SwitchMissingFromStates_DoesNothing()
    {
        var context = TestData.Context(states: []);
        await RegisterClimateSwitchAsync(context);

        await _handler.HandleAsync(context, CancellationToken.None);

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task SwitchOff_DoesNothing()
    {
        var states = new[] { TestData.State("switch.car_climate", "off", DateTimeOffset.UtcNow.AddHours(-1)) };
        var context = TestData.Context(states: states);
        await RegisterClimateSwitchAsync(context);

        await _handler.HandleAsync(context, CancellationToken.None);

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task OnButWithinInterval_DoesNothing()
    {
        var states = new[] { TestData.State("switch.car_climate", "on", DateTimeOffset.UtcNow.AddMinutes(-5)) };
        var context = TestData.Context(states: states);
        await RegisterClimateSwitchAsync(context);

        await _handler.HandleAsync(context, CancellationToken.None);

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task OnAndOld_ButSwitchNotRegistered_DoesNothing()
    {
        var states = new[] { TestData.State("switch.car_climate", "on", DateTimeOffset.UtcNow.AddMinutes(-20)) };
        var context = TestData.Context(states: states);
        // No switch registered with the publisher.

        await _handler.HandleAsync(context, CancellationToken.None);

        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task OnAndOld_PublishesSwitchTurnedOff()
    {
        var states = new[] { TestData.State("switch.car_climate", "on", DateTimeOffset.UtcNow.AddMinutes(-20)) };
        var context = TestData.Context(states: states);
        var @switch = await RegisterClimateSwitchAsync(context);

        await _handler.HandleAsync(context, CancellationToken.None);

        var published = Assert.Single(_publisher.Published);
        Assert.Same(@switch, published);
        Assert.False(@switch.IsOn);
    }

    [Fact]
    public async Task MatchesByFriendlyName_WhenEntityIdRenamed()
    {
        var states = new[]
        {
            TestData.State("switch.renamed_by_user", "on", DateTimeOffset.UtcNow.AddMinutes(-20), friendlyName: "Car Climate")
        };
        var context = TestData.Context(states: states);
        var @switch = await RegisterClimateSwitchAsync(context);

        await _handler.HandleAsync(context, CancellationToken.None);

        Assert.Same(@switch, Assert.Single(_publisher.Published));
    }
}
