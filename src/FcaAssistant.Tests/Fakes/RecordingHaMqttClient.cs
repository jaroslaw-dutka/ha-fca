using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Tests.Fakes;

public class RecordingHaMqttClient : IHaMqttClient
{
    public List<IHaEntity> Announced { get; } = [];
    public List<IHaEntity> Published { get; } = [];
    public List<IHaSetEntity> Subscribed { get; } = [];

    public Task ConnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AnnounceAsync(IHaEntity entity)
    {
        Announced.Add(entity);
        return Task.CompletedTask;
    }

    public Task PublishAsync(IHaEntity entity)
    {
        Published.Add(entity);
        return Task.CompletedTask;
    }

    public void Subscribe(IHaSetEntity entity) => Subscribed.Add(entity);
}
