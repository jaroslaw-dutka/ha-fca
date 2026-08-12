using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;

namespace FcaAssistant.Tests.Fakes;

public class FakeHaEntityPublisher : IHaEntityPublisher
{
    private readonly Dictionary<string, IHaEntity> _entities = new();

    public List<IHaEntity> Registered { get; } = [];
    public List<IHaEntity> Published { get; } = [];

    public Task RegisterAsync(IHaSetEntity entity)
    {
        Registered.Add(entity);
        _entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task PublishAsync(IHaEntity entity)
    {
        Published.Add(entity);
        _entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public T? Get<T>(string id) where T : class, IHaEntity => _entities.GetValueOrDefault(id) as T;
}
