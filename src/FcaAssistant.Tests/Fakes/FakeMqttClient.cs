using MQTTnet;
using MQTTnet.Diagnostics.PacketInspection;

namespace FcaAssistant.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IMqttClient"/> for exercising <see cref="FcaAssistant.Infrastructure.Mqtt.MqttClientBase"/>
/// without a broker. Records published messages and subscriptions, and can simulate connect failures.
/// </summary>
public class FakeMqttClient : IMqttClient
{
    public int ConnectAttempts { get; private set; }
    public int FailConnectsBefore { get; set; }
    public List<MqttApplicationMessage> Published { get; } = [];
    public List<string> Subscriptions { get; } = [];

    public bool IsConnected { get; private set; }
    public MqttClientOptions Options { get; private set; } = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();

    public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;
    public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync;
    public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;
    public event Func<InspectMqttPacketEventArgs, Task>? InspectPacketAsync;

    public Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
    {
        ConnectAttempts++;
        Options = options;
        if (ConnectAttempts <= FailConnectsBefore)
            throw new InvalidOperationException("Simulated connect failure");

        IsConnected = true;
        return Task.FromResult(new MqttClientConnectResult());
    }

    public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
    {
        Published.Add(applicationMessage);
        // The base client ignores the result; a real one isn't worth constructing.
        return Task.FromResult<MqttClientPublishResult>(null!);
    }

    public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default)
    {
        Subscriptions.AddRange(options.TopicFilters.Select(f => f.Topic));
        return Task.FromResult(new MqttClientSubscribeResult(0, [], string.Empty, []));
    }

    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MqttClientUnsubscribeResult(0, [], string.Empty, []));

    public Task PingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendEnhancedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void Dispose() { }
}
