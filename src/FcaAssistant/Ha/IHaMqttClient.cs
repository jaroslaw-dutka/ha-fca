using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Ha;

public interface IHaMqttClient
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task AnnounceAsync(IHaEntity entity);
    Task PublishAsync(IHaEntity entity);
    void Subscribe(IHaSetEntity entity);
}