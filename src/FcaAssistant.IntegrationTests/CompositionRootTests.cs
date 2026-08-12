using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.Fca;
using FcaAssistant.Ha;
using FcaAssistant.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FcaAssistant.IntegrationTests;

/// <summary>
/// Wires up the real composition root (the AddXxx extensions) and verifies the graph resolves.
/// Uses the Mock FCA backend so nothing touches the network.
/// </summary>
public class CompositionRootTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["fca:brand"] = "Mock"
            })
            .Build();

        return new ServiceCollection()
            .AddLogger(configuration)
            .AddHttp(configuration)
            .AddFca(configuration)
            .AddHa(configuration)
            .AddApp(configuration)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task AppService_ResolvesWithFullDependencyGraph()
    {
        await using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IAppService>());
    }

    [Fact]
    public async Task VehicleHandlers_ResolveInExecutionOrder()
    {
        await using var provider = BuildProvider();

        var handlers = provider.GetServices<IVehicleHandler>().Select(h => h.GetType());

        Assert.Equal(
        [
            typeof(AutoRefreshHandler),
            typeof(LocationHandler),
            typeof(SensorsHandler),
            typeof(CommandEntitiesHandler),
            typeof(ClimateAutoOffHandler),
            typeof(TimestampHandler)
        ], handlers);
    }
}
