using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Ha;

public class HaEntityPublisher(IHaMqttClient haMqttClient) : IHaEntityPublisher
{
    private readonly Dictionary<string, IHaEntity> _entities = new();

    public async Task RegisterAsync(IHaSetEntity entity)
    {
        if (!_entities.TryAdd(entity.Id, entity))
            return;

        haMqttClient.Subscribe(entity);
        await haMqttClient.AnnounceAsync(entity);
    }

    public async Task PublishAsync(IHaEntity entity)
    {
        if (_entities.TryAdd(entity.Id, entity))
            await haMqttClient.AnnounceAsync(entity);

        await haMqttClient.PublishAsync(entity);
    }

    public T? Get<T>(string id) where T : class, IHaEntity =>
        _entities.GetValueOrDefault(id) as T;
}
