using MQTTnet;

namespace FcaAssistant.Infrastructure.Mqtt;

/// <summary>Default <see cref="IMqttClientFactory"/> backed by MQTTnet's own factory.</summary>
public sealed class MqttnetClientFactory : IMqttClientFactory
{
    public IMqttClient CreateClient() => new MqttClientFactory().CreateMqttClient();
}
