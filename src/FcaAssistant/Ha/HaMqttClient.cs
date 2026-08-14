using System.Text.Json;
using System.Text.Json.Serialization;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;
using FcaAssistant.Infrastructure.Mqtt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace FcaAssistant.Ha;

public class HaMqttClient(ILogger<HaMqttClient> logger, IOptions<HaMqttSettings> options, IMqttClientFactory clientFactory)
    : MqttClientBase(logger, clientFactory, "HomeAssistant"), IHaMqttClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private readonly Dictionary<string, IHaSetEntity> _setEntities = new();
    private readonly HaMqttSettings _settings = options.Value;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.User) || string.IsNullOrWhiteSpace(_settings.Password))
            Logger.LogWarning("Mqtt User/Password is EMPTY.");

        AddSubscription(GetTopic("+", "+", HaMqttTopic.Set));
        StartMqtt(cancellationToken);
        return Task.CompletedTask;
    }

    protected override MqttClientOptions BuildOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithCleanSession()
            .WithClientId(_settings.ClientId)
            .WithTcpServer(_settings.Server, _settings.Port);

        if (!string.IsNullOrWhiteSpace(_settings.User) && !string.IsNullOrWhiteSpace(_settings.Password))
            builder.WithCredentials(_settings.User, _settings.Password);

        if (_settings.UseTls)
            builder.WithTlsOptions(_ => { });

        return builder.Build();
    }

    protected override async Task OnMessageReceivedAsync(MqttApplicationMessage message)
    {
        try
        {
            if (_setEntities.TryGetValue(message.Topic, out var command))
            {
                await command.OnSetAsync(message.ConvertPayloadToString());
                await PublishAsync(command);
            }
            else
            {
                Logger.LogWarning("Unhandled MQTT message with topic: {Topic}", message.Topic);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error processing MQTT message with topic: {Topic}", message.Topic);
        }
    }

    public async Task AnnounceAsync(IHaEntity entity)
    {
        var interfaces = entity.GetType().GetInterfaces();
        var announcement = new HaAnnouncement
        {
            Device = entity.Device,
            Name = entity.Name,
            UniqueId = entity.Id,
            Platform = "mqtt",
            UnitOfMeasurement = entity.UnitOfMeasurement,
            DeviceClass = entity.DeviceClass,
            Icon = entity.Icon,
            StateTopic = interfaces.Contains(typeof(IHaStateEntity)) ? GetTopic(entity, HaMqttTopic.State) : null,
            AttributesTopic = interfaces.Contains(typeof(IHaAttributesEntity)) ? GetTopic(entity, HaMqttTopic.Attributes) : null,
            CommandTopic = interfaces.Contains(typeof(IHaSetEntity)) ? GetTopic(entity, HaMqttTopic.Set) : null,
        };
        var json = JsonSerializer.Serialize(announcement, SerializerOptions);
        await PublishAsync(GetTopic(entity, HaMqttTopic.Config), json, retain: true);
    }

    public async Task PublishAsync(IHaEntity entity)
    {
        if (entity is IHaStateEntity stateEntity)
            await PublishAsync(GetTopic(entity, HaMqttTopic.State), stateEntity.State, retain: true);
        if (entity is IHaAttributesEntity attributesEntity)
            await PublishAsync(GetTopic(entity, HaMqttTopic.Attributes), attributesEntity.SerializedAttributes, retain: true);
    }

    public void Subscribe(IHaSetEntity entity)
    {
        var topic = GetTopic(entity, HaMqttTopic.Set);
        Logger.LogDebug("Subscribing to topic: {Topic}", topic);
        _setEntities.Add(topic, entity);
    }

    private string GetTopic(IHaEntity entity, HaMqttTopic topic) =>
        GetTopic(entity.Type, entity.Id, topic);

    private string GetTopic(string type, string id, HaMqttTopic topic) =>
        $"homeassistant/{type}/{id}/{topic.ToString().ToLower()}";
}
