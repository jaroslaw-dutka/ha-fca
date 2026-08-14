using FcaAssistant.Infrastructure.Mqtt;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;

namespace FcaAssistant.Tests.Infrastructure.Mqtt;

public class MqttClientBaseTests
{
    private readonly FakeMqttClient _client = new();

    private TestMqttClient CreateSut() => new(_client);

    [Fact]
    public async Task PublishesMadeWhileOffline_AreQueuedAndFlushedOnConnect()
    {
        var sut = CreateSut();

        await sut.Publish("topic/a", "one");
        await sut.Publish("topic/b", "two");
        Assert.Empty(_client.Published);

        await sut.Start(CancellationToken.None);

        Assert.Equal(["topic/a", "topic/b"], _client.Published.Select(m => m.Topic));
        Assert.Equal(["one", "two"], _client.Published.Select(m => m.ConvertPayloadToString()));
    }

    [Fact]
    public async Task TrackedSubscriptions_AreReSubscribedOnConnect()
    {
        var sut = CreateSut();
        sut.Subscribe("a/set");
        sut.Subscribe("b/set");

        await sut.Start(CancellationToken.None);

        Assert.Equal(["a/set", "b/set"], _client.Subscriptions);
    }

    [Fact]
    public async Task Connect_RetriesRebuildingOptionsUntilItSucceeds()
    {
        _client.FailConnectsBefore = 2;
        var sut = CreateSut();

        await sut.Start(CancellationToken.None);

        Assert.Equal(3, _client.ConnectAttempts);
        Assert.Equal(3, sut.BuildOptionsCalls); // options rebuilt for every attempt
        Assert.True(_client.IsConnected);
    }

    [Fact]
    public async Task PublishAfterConnect_GoesStraightToTheClient()
    {
        var sut = CreateSut();
        await sut.Start(CancellationToken.None);

        await sut.Publish("topic/c", "three");

        Assert.Equal("topic/c", Assert.Single(_client.Published).Topic);
    }

    /// <summary>Exposes the protected surface of <see cref="MqttClientBase"/> for testing.</summary>
    private sealed class TestMqttClient(FakeMqttClient client)
        : MqttClientBase(NullLogger.Instance, new FakeMqttClientFactory(client), "Test")
    {
        public int BuildOptionsCalls { get; private set; }

        protected override MqttClientOptions BuildOptions()
        {
            BuildOptionsCalls++;
            return new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();
        }

        public Task Start(CancellationToken cancellationToken) => StartMqtt(cancellationToken);
        public void Subscribe(string topic) => AddSubscription(topic);
        public Task Publish(string topic, string payload) => PublishAsync(topic, payload);
    }
}
