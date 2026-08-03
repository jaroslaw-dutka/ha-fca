using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace FcaAssistant.Infrastructure.Mqtt;

/// <summary>
/// Shared MQTT client used by every component that talks MQTT. It owns a single
/// <see cref="IMqttClient"/> and takes care of the concerns MQTTnet v5 no longer
/// provides out of the box (the managed client was removed in v5):
/// <list type="bullet">
/// <item>background connect + auto-reconnect with back-off</item>
/// <item>re-signing/rebuilding the options on every attempt (see <see cref="BuildOptions"/>)</item>
/// <item>re-subscribing tracked topics after every (re)connect</item>
/// <item>an outbound queue so publishes made while offline are flushed on connect</item>
/// </list>
/// </summary>
public abstract class MqttClientBase : IAsyncDisposable
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly string _name;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly List<string> _subscriptions = new();
    private readonly ConcurrentQueue<MqttApplicationMessage> _outbound = new();

    private IMqttClient? _client;
    private CancellationToken _cancellationToken;

    protected MqttClientBase(ILogger logger, string name)
    {
        Logger = logger;
        _name = name;
    }

    protected ILogger Logger { get; }

    /// <summary>
    /// Builds the options for a single connect attempt. Invoked on every (re)connect,
    /// so implementations backed by short-lived credentials (e.g. an AWS SigV4 signed
    /// URL) get a fresh, valid connection each time instead of replaying a stale one.
    /// </summary>
    protected abstract MqttClientOptions BuildOptions();

    /// <summary>Handles a received message. Override to route it.</summary>
    protected virtual Task OnMessageReceivedAsync(MqttApplicationMessage message) => Task.CompletedTask;

    /// <summary>
    /// Registers a topic filter that is (re)subscribed on every connect. Safe to call
    /// before the connection is established; if already connected it subscribes immediately.
    /// </summary>
    protected void AddSubscription(string topic)
    {
        _subscriptions.Add(topic);
        if (_client is { IsConnected: true })
            _ = _client.SubscribeAsync(topic, cancellationToken: _cancellationToken);
    }

    /// <summary>
    /// Wires up the client and kicks off the connection loop in the background, mirroring
    /// the old managed client: returns immediately and keeps (re)connecting on its own.
    /// </summary>
    protected void StartMqtt(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        _client = new MqttClientFactory().CreateMqttClient();
        _client.ConnectedAsync += OnConnectedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;

        _ = ConnectCoreAsync();
    }

    protected Task PublishAsync(string topic, string? payload, bool retain = false)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload ?? string.Empty)
            .WithRetainFlag(retain)
            .Build();

        return PublishAsync(message);
    }

    private async Task PublishAsync(MqttApplicationMessage message)
    {
        if (_client is { IsConnected: true })
        {
            try
            {
                await _client.PublishAsync(message, _cancellationToken);
                return;
            }
            catch (Exception e)
            {
                Logger.LogDebug(e, "Publish to {name} MQTT topic {topic} failed, queueing", _name, message.Topic);
            }
        }

        // Offline (or the direct publish failed): queue it and let the next connect flush it.
        _outbound.Enqueue(message);
    }

    private Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        Logger.LogInformation("Connected to {name} MQTT: {reason}", _name, args.ConnectResult.ReasonString);
        return Task.CompletedTask;
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        Logger.LogWarning("Disconnected from {name} MQTT: {reason}", _name, args.ReasonString);

        try
        {
            await Task.Delay(ReconnectDelay, _cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await ConnectCoreAsync();
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var message = args.ApplicationMessage;
        Logger.LogDebug("{name} MQTT: {topic} - {payload}", _name, message.Topic, message.ConvertPayloadToString());
        await OnMessageReceivedAsync(message);
    }

    private async Task ConnectCoreAsync()
    {
        if (_client is null)
            return;

        try
        {
            await _connectLock.WaitAsync(_cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            // Retry until connected (or shutdown). Each attempt rebuilds the options, so an
            // expired signature/session no longer wedges reconnects in a tight loop.
            while (!_cancellationToken.IsCancellationRequested && !_client.IsConnected)
            {
                try
                {
                    await _client.ConnectAsync(BuildOptions(), _cancellationToken);

                    foreach (var topic in _subscriptions)
                        await _client.SubscribeAsync(topic, cancellationToken: _cancellationToken);

                    await FlushOutboundAsync();
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Logger.LogDebug(e, "Failed to connect to {name} MQTT, retrying in {seconds}s", _name, ReconnectDelay.TotalSeconds);
                    try
                    {
                        await Task.Delay(ReconnectDelay, _cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task FlushOutboundAsync()
    {
        while (_outbound.TryDequeue(out var message))
        {
            try
            {
                await _client!.PublishAsync(message, _cancellationToken);
            }
            catch (Exception e)
            {
                // Put it back and stop; the next connect will try again.
                Logger.LogDebug(e, "Flushing queued {name} MQTT message to {topic} failed, re-queueing", _name, message.Topic);
                _outbound.Enqueue(message);
                return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            try
            {
                if (_client.IsConnected)
                    await _client.DisconnectAsync();
            }
            catch
            {
                // Best effort on shutdown.
            }

            _client.Dispose();
        }

        _connectLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
