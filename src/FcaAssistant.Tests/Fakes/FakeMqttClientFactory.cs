using FcaAssistant.Infrastructure.Mqtt;
using MQTTnet;

namespace FcaAssistant.Tests.Fakes;

/// <summary>Hands <see cref="MqttClientBase"/> a <see cref="FakeMqttClient"/> instead of a real one.</summary>
public class FakeMqttClientFactory(FakeMqttClient client) : IMqttClientFactory
{
    public IMqttClient CreateClient() => client;
}
