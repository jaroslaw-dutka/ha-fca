using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.Fca;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.App.Handlers;

public class AutoRefreshHandlerTests
{
    private readonly FakeFcaClient _fcaClient = new();

    private AutoRefreshHandler Handler(bool battery = false, bool location = false) =>
        new(_fcaClient, Options.Create(new AppSettings { AutoRefreshBattery = battery, AutoRefreshLocation = location }));

    [Fact]
    public async Task BatteryEnabled_SendsDeepRefresh()
    {
        await Handler(battery: true).HandleAsync(TestData.Context(), CancellationToken.None);

        Assert.Same(FcaCommands.DeepRefresh, Assert.Single(_fcaClient.Sent).Command);
    }

    [Fact]
    public async Task LocationEnabled_SendsVehicleFinder()
    {
        await Handler(location: true).HandleAsync(TestData.Context(), CancellationToken.None);

        Assert.Same(FcaCommands.VehicleFinder, Assert.Single(_fcaClient.Sent).Command);
    }

    [Fact]
    public async Task NothingEnabled_SendsNothing()
    {
        await Handler().HandleAsync(TestData.Context(), CancellationToken.None);

        Assert.Empty(_fcaClient.Sent);
    }

    [Fact]
    public async Task BothEnabled_SendsBoth()
    {
        await Handler(battery: true, location: true).HandleAsync(TestData.Context(), CancellationToken.None);

        Assert.Equal(2, _fcaClient.Sent.Count);
    }
}
