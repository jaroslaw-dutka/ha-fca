using FcaAssistant.App.Handlers;
using FcaAssistant.Fca;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FcaAssistant.Tests.App.Handlers;

public class CommandEntitiesHandlerTests
{
    private readonly FakeHaEntityPublisher _publisher = new();
    private readonly FakeFcaClient _fcaClient = new();
    private readonly FakeRefreshTrigger _refreshTrigger = new();
    private readonly CommandEntitiesHandler _handler;

    public CommandEntitiesHandlerTests() =>
        _handler = new CommandEntitiesHandler(
            _publisher, _fcaClient, _refreshTrigger,
            NullLogger<CommandEntitiesHandler>.Instance);

    private async Task HandleAsync() =>
        await _handler.HandleAsync(TestData.Context(), CancellationToken.None);

    private T Registered<T>(string name) where T : HaEntity =>
        _publisher.Registered.OfType<T>().Single(e => e.Name == name);

    [Fact]
    public async Task RegistersAllButtonsAndSwitches()
    {
        await HandleAsync();

        var buttons = _publisher.Registered.OfType<HaButton>().Select(b => b.Name);
        var switches = _publisher.Registered.OfType<HaSwitch>().Select(s => s.Name);

        Assert.Equal(["Blink", "Charge", "UpdateAll", "UpdateLocation", "UpdateBattery"], buttons);
        Assert.Equal(["Doors", "Climate", "Trunk"], switches);
    }

    [Fact]
    public async Task ButtonPress_SendsCommandAndTriggersRefresh()
    {
        await HandleAsync();

        await Registered<HaButton>("Blink").OnSetAsync("PRESS");

        var sent = Assert.Single(_fcaClient.Sent);
        Assert.Equal("VIN123", sent.Vin);
        Assert.Same(FcaCommands.Blink, sent.Command);
        Assert.Equal(1, _refreshTrigger.TriggerCount);
    }

    [Fact]
    public async Task ButtonPress_WhenSendFails_DoesNotTriggerRefresh()
    {
        _fcaClient.SendResult = false;
        await HandleAsync();

        await Registered<HaButton>("Charge").OnSetAsync("PRESS");

        Assert.Single(_fcaClient.Sent);
        Assert.Equal(0, _refreshTrigger.TriggerCount);
    }

    [Fact]
    public async Task SwitchOn_SendsOnCommand()
    {
        await HandleAsync();

        await Registered<HaSwitch>("Climate").OnSetAsync("ON");

        Assert.Same(FcaCommands.ClimateOn, Assert.Single(_fcaClient.Sent).Command);
        Assert.Equal(1, _refreshTrigger.TriggerCount);
    }

    [Fact]
    public async Task SwitchOff_SendsOffCommand()
    {
        await HandleAsync();

        await Registered<HaSwitch>("Climate").OnSetAsync("OFF");

        Assert.Same(FcaCommands.ClimateOff, Assert.Single(_fcaClient.Sent).Command);
    }
}
