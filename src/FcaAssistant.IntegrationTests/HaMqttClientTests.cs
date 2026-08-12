using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;

namespace FcaAssistant.IntegrationTests;

/// <summary>
/// Exercises the real MQTT plumbing (MqttClientBase connect + offline queue/flush, HaMqttClient topic
/// building and retained announce/publish) against an in-process MQTTnet broker.
/// </summary>
public class HaMqttClientTests
{
    [Fact]
    public async Task AnnounceAndPublish_DeliverRetainedDiscoveryAndStateToBroker()
    {
        var port = FreePort();
        var received = new ConcurrentBag<MqttApplicationMessage>();

        var server = new MqttServerFactory().CreateMqttServer(
            new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());
        server.InterceptingPublishAsync += args =>
        {
            received.Add(args.ApplicationMessage);
            return Task.CompletedTask;
        };
        await server.StartAsync();

        try
        {
            await using var client = new HaMqttClient(
                NullLogger<HaMqttClient>.Instance,
                Options.Create(new HaMqttSettings { ClientId = "it-test", Server = "localhost", Port = port }));

            await client.ConnectAsync(CancellationToken.None);

            var sensor = new HaSensor(Device(), "odometer") { State = "12345" };

            await client.AnnounceAsync(sensor);
            await client.PublishAsync(sensor);

            await WaitUntilAsync(
                () => received.Any(m => m.Topic == "homeassistant/sensor/VIN_odometer/state"),
                TimeSpan.FromSeconds(15));

            var config = received.Single(m => m.Topic == "homeassistant/sensor/VIN_odometer/config");
            Assert.True(config.Retain);
            Assert.Contains("homeassistant/sensor/VIN_odometer/state", config.ConvertPayloadToString());

            var state = received.Single(m => m.Topic == "homeassistant/sensor/VIN_odometer/state");
            Assert.True(state.Retain);
            Assert.Equal("12345", state.ConvertPayloadToString());
        }
        finally
        {
            await server.StopAsync();
            server.Dispose();
        }
    }

    private static HaDevice Device() => new()
    {
        Name = "Car",
        Identifiers = ["VIN"],
        Manufacturer = "Fiat",
        Model = "500e",
        Version = "1.0"
    };

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Condition was not met within the timeout.");
            await Task.Delay(25);
        }
    }
}
