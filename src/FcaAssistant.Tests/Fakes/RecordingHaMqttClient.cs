using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Tests.Fakes;

public class RecordingHaMqttClient : IHaMqttClient
{
    public List<IHaEntity> Announced { get; } = [];
    public List<IHaEntity> Published { get; } = [];
    public List<IHaSetEntity> Subscribed { get; } = [];
    public bool Connected { get; private set; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        Connected = true;
        return Task.CompletedTask;
    }

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
