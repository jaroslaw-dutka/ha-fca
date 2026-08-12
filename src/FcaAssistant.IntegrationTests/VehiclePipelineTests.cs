using FcaAssistant.App;
using FcaAssistant.App.Handlers;
using FcaAssistant.Fca;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FcaAssistant.IntegrationTests;

/// <summary>
/// Drives the whole vehicle-processing pipeline through the real DI graph and the Mock FCA backend
/// (which deserializes the real ./Mocks payloads), capturing everything at the MQTT boundary.
/// </summary>
public class VehiclePipelineTests
{
    private static (ServiceProvider Provider, RecordingHaMqttClient Mqtt) BuildPipeline()
    {
        // Mock FCA backend reads its payloads relative to the current directory.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["fca:brand"] = "Mock" })
            .Build();

        var mqtt = new RecordingHaMqttClient();

        var services = new ServiceCollection()
            .AddLogger(configuration)
            .AddHttp(configuration)
            .AddFca(configuration)
            .AddHa(configuration)
            .AddApp(configuration);
        services.RemoveAll<IHaMqttClient>();
        services.AddSingleton<IHaMqttClient>(mqtt);

        return (services.BuildServiceProvider(), mqtt);
    }

    private static async Task<RecordingHaMqttClient> RunAsync()
    {
        var (provider, mqtt) = BuildPipeline();
        await using (provider)
        {
            var vehicle = Assert.Single(await provider.GetRequiredService<IFcaClient>().GetVehiclesAsync());
            var context = new CarContext(vehicle, []);

            foreach (var handler in provider.GetServices<IVehicleHandler>())
                await handler.HandleAsync(context, CancellationToken.None);
        }

        return mqtt;
    }

    [Fact]
    public async Task Processing_RegistersAllControlEntities()
    {
        var mqtt = await RunAsync();

        var switches = mqtt.Subscribed.OfType<HaSwitch>().Select(s => s.Name);
        var buttons = mqtt.Subscribed.OfType<HaButton>().Select(b => b.Name);

        Assert.Equal(["Doors", "Climate", "Trunk"], switches);
        Assert.Equal(5, buttons.Count());
    }

    [Fact]
    public async Task Processing_PublishesSensorsFromMockPayloads()
    {
        var mqtt = await RunAsync();

        // The Mock details/remote payloads produce a bunch of sensors, plus the timestamp.
        Assert.Contains(mqtt.Published.OfType<HaSensor>(), s => s.Name == "LAST_UPDATE");
        Assert.Contains(mqtt.Published.OfType<HaSensor>(), s => s.Name.StartsWith("car_"));
    }

    [Fact]
    public async Task Processing_AnnouncesEveryPublishedAndRegisteredEntityOnce()
    {
        var mqtt = await RunAsync();

        var announcedIds = mqtt.Announced.Select(e => e.Id).ToList();

        Assert.Equal(announcedIds.Count, announcedIds.Distinct().Count());
        Assert.All(mqtt.Published, e => Assert.Contains(e.Id, announcedIds));
        Assert.All(mqtt.Subscribed, e => Assert.Contains(e.Id, announcedIds));
    }
}
