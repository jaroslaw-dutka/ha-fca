using MQTTnet;

namespace FcaAssistant.Infrastructure.Mqtt;

/// <summary>Creates the underlying MQTT client. A seam so tests can supply an in-memory client.</summary>
public interface IMqttClientFactory
{
    IMqttClient CreateClient();
}
