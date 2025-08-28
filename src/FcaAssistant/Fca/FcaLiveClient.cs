using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using FcaAssistant.Aws;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace FcaAssistant.Fca;

public class FcaLiveClient : IFcaClient
{
    private readonly ILogger<FcaLiveClient> _logger;
    private readonly IFcaApiClient _apiClient;
    private readonly FcaApiConfig _apiConfig;
    private readonly AmazonCognitoIdentityClient _cognitoClient;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _commands = new();
    private FcaSession? _fcaSession;
    private IManagedMqttClient _client;

    public FcaLiveClient(ILogger<FcaLiveClient> logger, IFcaApiConfigProvider configProvider, IFcaApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
        _apiConfig = configProvider.Get();
        _cognitoClient = new AmazonCognitoIdentityClient(new AnonymousAWSCredentials(), _apiConfig.AwsEndpoint);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_fcaSession is not null)
            return;

        await LoginAsync();
        await ConnectToMqttAsync();

        _ = Task.Run(async () =>
        {
            var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    _logger.LogInformation("REFRESH SESSION");
                    await LoginAsync();
                }
                catch (Exception e)
                {

                    _logger.LogError("ERROR WHILE REFRESH SESSION");
                    _logger.LogDebug("{0}", e);
                }
            }
        }, cancellationToken);
    }

    public async Task<List<VehicleInfo>> GetVehiclesAsync()
    {
        ArgumentNullException.ThrowIfNull(_fcaSession);

        var result = new List<VehicleInfo>();

        var vehicleResponse = await _apiClient.GetVehicles(_fcaSession);
        foreach (var vehicle in vehicleResponse.Vehicles)
        {
            result.Add(new VehicleInfo
            {
                Vehicle = vehicle,
                Details = await _apiClient.GetVehicleDetails(_fcaSession, vehicle.Vin),
                Location = await _apiClient.GetVehicleLocation(_fcaSession, vehicle.Vin),
                Remote = await _apiClient.GetVehicleRemoteStatus(_fcaSession, vehicle.Vin)
            });
        }

        return result;
    }

    public async Task SendCommandAsync(string vin, string command, string pin, string action)
    {
        ArgumentNullException.ThrowIfNull(_fcaSession);

        _logger.LogInformation("SEND COMMAND {command}: ", command);

        if (string.IsNullOrWhiteSpace(pin))
            throw new Exception("PIN NOT SET");

        var pinAuthResponse = await _apiClient.AuthenticatePin(_fcaSession, pin);
        var commandResponse = await _apiClient.SendCommand(_fcaSession, pinAuthResponse.Token, vin, action, command);

        var tcs = new TaskCompletionSource();
        _commands.TryAdd(commandResponse.CorrelationId, tcs);

        var index = Task.WaitAny([tcs.Task], TimeSpan.FromSeconds(30));

        _commands.TryRemove(commandResponse.CorrelationId, out _);

        if (index < 0)
            throw new TimeoutException("Command timed out");
    }

    public async Task<bool> TrySendCommandAsync(string vin, string command, string pin, string action)
    {
        try
        {
            await SendCommandAsync(vin, command, pin, action);
            await Task.Delay(TimeSpan.FromSeconds(5));
            _logger.LogInformation("Command: {command} SUCCESSFUL", command);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Command: {command} ERROR. Maybe wrong pin?", command);
            _logger.LogDebug(e, e.Message);
            return false;
        }
    }

    private async Task LoginAsync()
    {
        var bootstrapResponse = await _apiClient.Bootstrap();
        bootstrapResponse.ThrowOnError("Login failed.");

        var loginResponse = await _apiClient.Login();
        loginResponse.ThrowOnError("Authentication failed.");

        var tokenResponse = await _apiClient.GetToken(loginResponse.SessionInfo.LoginToken);
        tokenResponse.ThrowOnError("Authentication failed.");

        var identityResponse = await _apiClient.GetIdentity(tokenResponse.IdToken);
        identityResponse.ThrowOnError("Identity failed.");

        var credentialsResponse = await _cognitoClient.GetCredentialsForIdentityAsync(identityResponse.IdentityId, new Dictionary<string, string>
        {
            { "cognito-identity.amazonaws.com", identityResponse.Token }
        });

        _fcaSession = new FcaSession
        {
            UserId = loginResponse.UID,
            AwsCredentials = new ImmutableCredentials(credentialsResponse.Credentials.AccessKeyId, credentialsResponse.Credentials.SecretKey, credentialsResponse.Credentials.SessionToken)
        };
    }

    private async Task ConnectToMqttAsync()
    {
        ArgumentNullException.ThrowIfNull(_fcaSession);

        var baseUri = new Uri("wss://ahwxpxjb5ckg1-ats.iot.eu-west-1.amazonaws.com:443/mqtt");
        var signedUri = AwsSigner.SignQuery(_fcaSession.AwsCredentials, "GET", baseUri, DateTime.UtcNow, _apiConfig.AwsEndpoint.SystemName, "iotdata", string.Empty);

        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_apiConfig.ClientId)
            .WithWebSocketServer(builder =>
            {
                builder.WithUri(signedUri.AbsoluteUri);
                builder.WithRequestHeaders(new Dictionary<string, string>
                {
                    { "host", signedUri.Host }
                });
            })
            .WithCleanSession();

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(builder.Build())
            .Build();

        _client = new MqttFactory().CreateManagedMqttClient();
        await _client.StartAsync(options);

        _client.ApplicationMessageReceivedAsync += args =>
        {
            var msg = args.ApplicationMessage;
            var payload = msg.ConvertPayloadToString();

            _logger.LogInformation("MQTT: {topic} - {payload}", msg.Topic, payload);

            var item = JsonSerializer.Deserialize<NotificationItem>(payload);
            if (_commands.TryRemove(item.CorrelationId, out var commandTask))
            {
                if (string.Equals(item.Notification.Data.Status, "success", StringComparison.InvariantCultureIgnoreCase))
                    commandTask.SetResult();
                else
                    commandTask.SetException(new Exception($"Command failed. Status: {item.Notification.Data.Status}."));
            }

            return Task.CompletedTask;
        };

        _client.ConnectedAsync += args =>
        {
            _logger.LogInformation("Connected to FCA MQTT: {reason}", args.ConnectResult.ReasonString);
            return Task.CompletedTask;
        };

        _client.ConnectingFailedAsync += args =>
        {
            _logger.LogInformation("Connection to FCA MQTT failed: {reason}", args.ConnectResult.ReasonString);
            return Task.CompletedTask;
        };

        _client.DisconnectedAsync += args =>
        {
            _logger.LogInformation("Disconnected from FCA MQTT: {reason}", args.ReasonString);
            return Task.CompletedTask;
        };

        await _client.SubscribeAsync("channels/" + _fcaSession.UserId + "/+/notifications/updates");
    }
}